using System.ComponentModel;
using System.Linq;
using Models;
using Models.Scene;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using Utils.Types;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class ProbeInspectorViewModel
    {
        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;

        private string ActiveProbeName =>
            _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE).ActiveProbeName;

        #endregion

        #region Properties

        [ObservableProperty]
        private bool _enabled;

        [ObservableProperty]
        private Vector3 _surfaceCoordinate;

        [ObservableProperty]
        private float _depth;

        [ObservableProperty]
        private Vector3 _angles;

        [ObservableProperty]
        private bool _locked;

        [ObservableProperty]
        private ProbeColor _probeColor;

        #endregion

        public ProbeInspectorViewModel(StoreService storeService)
        {
            // Register services.
            _storeService = storeService;

            // Subscribe to scene state changes and initialize properties.
            _sceneStateSubscription = _storeService.Store.Subscribe(
                state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                OnSceneStateChanged,
                new SubscribeOptions<SceneState> { fireImmediately = true }
            );
            PropertyChanged += OnPropertyChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnSceneStateChanged(SceneState sceneState)
        {
            // Early exit if no active probe.
            if (string.IsNullOrEmpty(sceneState.ActiveProbeName))
            {
                Enabled = true;
                return;
            }

            Enabled = true;

            // Get depth from probe manager.
            var (surfaceCoordinateT, depthT) = ProbeManager
                .Instances.First(manager => manager.name == sceneState.ActiveProbeName)
                .GetSurfaceCoordinateT();

            // If the probe is outside the brain (i.e., no valid surface coordinate), use the APMLDV position.
            if (float.IsNaN(surfaceCoordinateT.x))
            {
                SurfaceCoordinate = sceneState.ActiveProbeState.APMLDV;
            }
            // Otherwise, use the surface coordinate with depth.
            else
            {
                SurfaceCoordinate = surfaceCoordinateT;
                Depth = depthT;
            }

            Angles = sceneState.ActiveProbeState.Angles;

            Locked = sceneState.ActiveProbeState.Locked;

            ProbeColor = sceneState.ActiveProbeState.Color;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        private void OnShuttingDown()
        {
            _sceneStateSubscription.Dispose();
            PropertyChanged -= OnPropertyChanged;
            App.shuttingDown -= OnShuttingDown;
        }

        #region Commands

        [ICommand]
        private void LockProbe()
        {
            _storeService.Store.Dispatch(SceneActions.SET_PROBE_LOCKED, (ActiveProbeName, !Locked));
        }

        [ICommand]
        private void DuplicateProbe()
        {
            _storeService.Store.Dispatch(SceneActions.DUPLICATE_PROBE, ActiveProbeName);
        }

        [ICommand]
        private void MoveProbeToReferenceCoordinate() { }

        [ICommand]
        private void MoveProbeToDura() { }

        [ICommand]
        private void SetProbeColor(ProbeColor color)
        {
            _storeService.Store.Dispatch(SceneActions.SET_PROBE_COLOR, (ActiveProbeName, color));
        }

        #endregion
    }
}
