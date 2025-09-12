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
        #region Constants

        private readonly Vector2 _pitchRange = new(0, 90);

        #endregion

        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;

        private string ActiveProbeName =>
            _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE).ActiveProbeName;

        #endregion

        #region Properties

        // Can be either the tip or surface, depending on settings.
        [ObservableProperty]
        private Vector3 _position;

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
            App.shuttingDown += OnShuttingDown;
        }

        private void OnSceneStateChanged(SceneState sceneState)
        {
            // Early exit if no active probe.
            if (string.IsNullOrEmpty(sceneState.ActiveProbeName))
                return;

            Position = sceneState.ActiveProbeState.APMLDV;
            Angles = sceneState.ActiveProbeState.Angles;
            Locked = sceneState.ActiveProbeState.Locked;
            ProbeColor = sceneState.ActiveProbeState.Color;
        }

        private void OnShuttingDown()
        {
            _sceneStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }

        #region Commands

        [ICommand]
        private void SetPosition(Vector3 position)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_PROBE_POSITION,
                (ActiveProbeName, position)
            );
        }

        [ICommand]
        private void SetAngles(Vector3 angles)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_PROBE_ANGLES,
                (ActiveProbeName, angles, _pitchRange)
            );
        }

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
        private void MoveProbeToReferenceCoordinate()
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_PROBE_POSITION,
                (ActiveProbeName, Vector3.zero)
            );
        }

        [ICommand]
        private void MoveProbeToDura()
        {
            ProbeManager
                .Instances.First(manager => manager.name == ActiveProbeName)
                .DropProbeToBrainSurface();
        }

        [ICommand]
        private void SetProbeColor(ProbeColor color)
        {
            _storeService.Store.Dispatch(SceneActions.SET_PROBE_COLOR, (ActiveProbeName, color));
        }

        #endregion
    }
}
