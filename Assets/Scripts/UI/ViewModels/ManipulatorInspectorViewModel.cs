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

        #endregion

        public ManipulatorInspectorViewModel(StoreService storeService)
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
            // Early exit if no active manipulator.
            if (string.IsNullOrEmpty(ActiveManipulatorId))
                return;

            VisualizationProbeName = sceneState.ActiveManipulatorState.ProbeName;
            Angles = sceneState.ActiveManipulatorState.Angles;
            _axesCount = sceneState.NumberOfAxesOnManipulator;
            Handedness = sceneState.ActiveManipulatorState.Handedness;
            ReferenceCoordinateOffset = sceneState.ActiveManipulatorState.ReferenceCoordinateOffset;
            DuraOffset = sceneState.ActiveManipulatorState.DuraOffset;
            IsManualControlEnabled = sceneState.ActiveManipulatorState.ManualControlEnabled;
        }

        private void OnShuttingDown()
        {
            _sceneStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }

        #region Commands

        [ICommand]
        private void SetAngles(Vector3 angles) { }

        [ICommand]
        private void SetHandedness(ManipulatorHandedness handedness) { }

        [ICommand]
        private void SetReferenceCoordinateOffset(Vector4 referenceCoordinateOffset) { }

        [ICommand]
        private void UseCurrentPositionForReferenceCoordinateOffset() { }

        [ICommand]
        private void SetDuraOffset(float duraOffset) { }

        [ICommand]
        private void RecalculateDuraOffset() { }

        [ICommand]
        private void SetManualControlEnabled(bool isEnabled) { }

        #endregion
    }
}
