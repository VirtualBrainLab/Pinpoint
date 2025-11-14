using System;
using System.Linq;
using BrainAtlas;
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
    public partial class ManipulatorInspectorViewModel
    {
        #region Services

        private readonly StoreService _storeService;
        private readonly EphysLinkService _ephysLinkService;
        private readonly IDisposableSubscription _sceneStateSubscription;

        private string ActiveManipulatorId =>
            _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE).ActiveManipulatorId;

        #endregion

        #region Properties

        [ObservableProperty]
        private string _visualizationProbeName;

        [ObservableProperty]
        private Vector3 _angles;

        [ObservableProperty]
        private int _axesCount;

        [ObservableProperty]
        private ManipulatorHandedness _handedness;

        [ObservableProperty]
        private Vector4 _referenceCoordinateOffset;

        [ObservableProperty]
        private float _duraOffset;

        [ObservableProperty]
        private bool _isManualControlEnabled;

        [ObservableProperty]
        private Vector4 _demoHomeCoordinate;

        [ObservableProperty]
        private Vector4 _demoTargetCoordinate;

        [ObservableProperty]
        private bool _isDemoRunning;

        #endregion

        public ManipulatorInspectorViewModel(
            StoreService storeService,
            EphysLinkService ephysLinkService
        )
        {
            // Register services.
            _storeService = storeService;
            _ephysLinkService = ephysLinkService;

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
            // Early exit if no active manipulator.
            if (string.IsNullOrEmpty(ActiveManipulatorId))
                return;

            VisualizationProbeName = sceneState.ActiveManipulatorState.VisualizationProbeName;
            Angles = sceneState.ActiveManipulatorState.Angles;
            AxesCount = sceneState.NumberOfAxesOnManipulator;
            Handedness = sceneState.ActiveManipulatorState.Handedness;
            ReferenceCoordinateOffset = sceneState.ActiveManipulatorState.ReferenceCoordinateOffset;
            DuraOffset = sceneState.ActiveManipulatorState.DuraOffset;
            IsManualControlEnabled = sceneState.ActiveManipulatorState.ManualControlEnabled;
            DemoHomeCoordinate = sceneState.ActiveManipulatorState.DemoHomeCoordinate;
            DemoTargetCoordinate = sceneState.ActiveManipulatorState.DemoTargetCoordinate;
            IsDemoRunning = sceneState.ActiveManipulatorState.IsDemoRunning;
        }

        private void OnShuttingDown()
        {
            _sceneStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }

        #region Commands

        [ICommand]
        private void AddVisualizationProbe(ProbeType probeType)
        {
            _storeService.Store.Dispatch(
                SceneActions.ADD_VISUALIZATION_PROBE,
                (ActiveManipulatorId, Guid.NewGuid().ToString(), probeType)
            );
        }

        [ICommand]
        private void InspectVisualizationProbe()
        {
            _storeService.Store.Dispatch(SceneActions.SET_ACTIVE_PROBE, VisualizationProbeName);
        }

        [ICommand]
        private void SetAngles(Vector3 angles)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_ANGLES,
                (ActiveManipulatorId, angles)
            );
        }

        [ICommand]
        private void SetHandedness(ManipulatorHandedness handedness)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_HANDEDNESS,
                (ActiveManipulatorId, handedness)
            );
        }

        [ICommand]
        private void SetReferenceCoordinateOffset(Vector4 referenceCoordinateOffset)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_REFERENCE_COORDINATE_OFFSET,
                (ActiveManipulatorId, referenceCoordinateOffset)
            );
        }

        [ICommand]
        private async void UseCurrentPositionForReferenceCoordinateOffset()
        {
            await _ephysLinkService.SetManipulatorReferenceCoordinateToCurrentPosition(
                ActiveManipulatorId
            );
        }

        [ICommand]
        private void ResetDuraOffset()
        {
            _storeService.Store.Dispatch(SceneActions.RESET_DURA_OFFSET, ActiveManipulatorId);
        }

        [ICommand]
        private async void RecalculateDuraOffset()
        {
            await _ephysLinkService.SetManipulatorDuraOffsetToCurrentDepth(ActiveManipulatorId);
        }

        [ICommand]
        private void SetManualControlEnabled(bool isEnabled)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_MANUAL_CONTROL_ENABLED,
                (ActiveManipulatorId, isEnabled)
            );
        }

        [ICommand]
        private void SetDemoHomeCoordinate(Vector4 coordinate)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_DEMO_HOME_COORDINATE,
                (ActiveManipulatorId, coordinate)
            );
        }

        [ICommand]
        private async void SetDemoHomeToCurrentPosition()
        {
            await _ephysLinkService.SetManipulatorDemoHomeCoordinateToCurrentPosition(
                ActiveManipulatorId
            );
        }

        [ICommand]
        private void SetDemoTargetCoordinate(Vector4 coordinate)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_DEMO_TARGET_COORDINATE,
                (ActiveManipulatorId, coordinate)
            );
        }

        [ICommand]
        private async void SetDemoTargetToCurrentPosition()
        {
            await _ephysLinkService.SetManipulatorDemoTargetCoordinateToCurrentPosition(
                ActiveManipulatorId
            );
        }

        [ICommand]
        private void SetIsDemoRunning(bool isRunning)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_DEMO_RUNNING,
                (ActiveManipulatorId, isRunning)
            );
        }

        #endregion
    }
}
