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
        private Vector4 _position;

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
        }

        private void OnSceneStateChanged(SceneState sceneState)
        {
            // Early exit if no active probe.
            if (string.IsNullOrEmpty(sceneState.ActiveProbeName))
            {
                Enabled = false;
                return;
            }

            Enabled = true;

            // Get depth from probe manager.
            var (_, depthT) = ProbeManager
                .Instances.First(manager => manager.name == sceneState.ActiveProbeName)
                .GetSurfaceCoordinateT();
            Position = new Vector4(
                sceneState.ActiveProbeState.APMLDV.x,
                sceneState.ActiveProbeState.APMLDV.y,
                sceneState.ActiveProbeState.APMLDV.z,
                depthT
            );

            Angles = sceneState.ActiveProbeState.Angles;

            Locked = sceneState.ActiveProbeState.Locked;

            ProbeColor = sceneState.ActiveProbeState.Color;
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
