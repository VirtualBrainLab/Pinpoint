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
            var currentPositionResponse = await _ephysLinkService.GetPosition(ActiveManipulatorId);
            if (!string.IsNullOrEmpty(currentPositionResponse.Error))
                return;
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_REFERENCE_COORDINATE_OFFSET,
                (ActiveManipulatorId, currentPositionResponse.Position)
            );
        }

        [ICommand]
        private void SetDuraOffset(float duraOffset)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_DURA_OFFSET,
                (ActiveManipulatorId, duraOffset)
            );
        }

        [ICommand]
        private void RecalculateDuraOffset()
        {
            // Find visualization probe manager.
            var visualizationProbeManager = ProbeManager.Instances.First(manager =>
                manager.name == VisualizationProbeName
            );

            // Get probe state.
            var visualizationProbeState = _storeService
                .Store.GetState<SceneState>(SliceNames.SCENE_SLICE)
                .Probes.First(state => state.Name == VisualizationProbeName);

            // Use distance from tip to the surface of the brain when inside the brain.
            if (visualizationProbeManager.IsProbeInBrain())
            {
                _storeService.Store.Dispatch(
                    SceneActions.CHANGE_MANIPULATOR_DURA_OFFSET_BY,
                    (
                        ActiveManipulatorId,
                        -Vector3.Distance(
                            visualizationProbeState.APMLDV,
                            visualizationProbeManager.GetSurfaceCoordinateT().surfaceCoordinateT
                        )
                    )
                );
            }
            // If outside, find the surface first and then compute the distance.
            else
            {
                // Find the surface coordinate.
                var (brainSurfaceCoordinateIndex, _) =
                    visualizationProbeManager.CalculateEntryCoordinate();

                // Exit if there's no surface.
                if (float.IsNaN(brainSurfaceCoordinateIndex.x))
                {
                    return;
                }

                var brainSurfaceToTransformed = BrainAtlasManager.ActiveAtlasTransform.U2T(
                    BrainAtlasManager.ActiveReferenceAtlas.World2Atlas(
                        BrainAtlasManager.ActiveReferenceAtlas.AtlasIdx2World(
                            brainSurfaceCoordinateIndex
                        )
                    )
                );

                _storeService.Store.Dispatch(
                    SceneActions.CHANGE_MANIPULATOR_DURA_OFFSET_BY,
                    (
                        ActiveManipulatorId,
                        Vector3.Distance(brainSurfaceToTransformed, visualizationProbeState.APMLDV)
                    )
                );
            }
        }

        [ICommand]
        private void SetManualControlEnabled(bool isEnabled)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_MANUAL_CONTROL_ENABLED,
                (ActiveManipulatorId, isEnabled)
            );
        }

        #endregion
    }
}
