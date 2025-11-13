using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class AutomationInspectorViewModel
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
        private AutomationProgressState _automationProgressState;

        [ObservableProperty]
        private Vector4 _referenceCoordinate;

        /// <summary>
        ///     Selected target insertion probe manager dropdown index (including the none option).
        /// </summary>
        [ObservableProperty]
        private int _targetInsertionProbeIndex;

        /// <summary>
        ///     Filtered list of targetable insertion probes for the active manipulator probe.
        ///     For insertions that are co-terminal and have not been selected yet. Does not include the "None" option.
        /// </summary>
        [ObservableProperty]
        private List<ProbeState> _targetInsertionProbeStates;

        [ObservableProperty]
        private float _duraOffset;

        [ObservableProperty]
        private int _selectedInsertionSpeedIndex;

        /// <summary>
        ///     Custom base insertion drive speed (µm/s).
        /// </summary>
        [ObservableProperty]
        private int _customInsertionSpeed;

        /// <summary>
        ///     Distance to drive past the target entry coordinate (µm).
        /// </summary>
        [ObservableProperty]
        private int _drivePastDistance;

        /// <summary>
        ///     Starting ETA of a drive to compute progress (seconds).
        /// </summary>
        private int _originalETA;

        /// <summary>
        ///     ETA to reach the target or to exit (seconds).
        /// </summary>
        [ObservableProperty]
        private int _eta;

        /// <summary>
        ///     Drive progress percentage (0 to 1).
        /// </summary>
        /// <remarks>To be computed from `_eta / _originalETA`.</remarks>
        [ObservableProperty]
        private float _driveProgressPercentage;

        #endregion

        public AutomationInspectorViewModel(
            StoreService storeService,
            EphysLinkService ephysLinkService
        )
        {
            // Register services.
            _storeService = storeService;
            _ephysLinkService = ephysLinkService;

            // Subscribe to state changes and initialize properties.
            _sceneStateSubscription = _storeService.Store.Subscribe(
                state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                OnSceneStateChanged,
                new SubscribeOptions<SceneState> { fireImmediately = true }
            );
            App.shuttingDown += OnShuttingDown;
        }

        private void OnSceneStateChanged(SceneState state)
        {
            // Exit if not enabled.
            if (string.IsNullOrEmpty(state.ActiveManipulatorId))
                return;

            // Set the automation progress state from the active probe state.
            AutomationProgressState = state.ActiveManipulatorState.AutomationProgressState;

            // Get the active manipulator's reference coordinate.
            ReferenceCoordinate = state.ActiveManipulatorState.ReferenceCoordinateOffset;

            // Get the list of targetable insertion probes for the active manipulator probe.
            var targetableInsertionProbeStates = state
                .Probes
                // 1. Not manipulator controlled.
                .Where(probeState =>
                    !state
                        .Manipulators.Select(manipulatorState =>
                            manipulatorState.VisualizationProbeName
                        )
                        .Contains(probeState.Name)
                )
                // 2. Co-terminal with the active probe angles.
                .Where(probeState =>
                    IsCoterminal(probeState.Angles, state.ActiveManipulatorState.Angles)
                )
                // 3. Is in the brain.
                .Where(probeState =>
                {
                    var probeManager = ProbeManager.Instances.FirstOrDefault(manager =>
                        manager.name == probeState.Name
                    );
                    return probeManager != null
                        && probeManager.CalculateEntryCoordinate().probeInBrain;
                })
                // 4. Is not already selected by other manipulator probes (unless it was selected by this active probe).
                .Where(probeState =>
                    !state
                        .Manipulators.Where(searchManipulatorState =>
                            searchManipulatorState.Id != state.ActiveManipulatorId
                        )
                        .Select(otherManipulator => otherManipulator.TargetInsertionProbeName)
                        .Contains(probeState.Name)
                )
                .ToList();
            if (
                TargetInsertionProbeStates == null
                || !TargetInsertionProbeStates.SequenceEqual(targetableInsertionProbeStates)
            )
            {
                TargetInsertionProbeStates = targetableInsertionProbeStates;
            }

            // Get the index of the selected target insertion probe.
            var selectedTargetInsertionProbeState = state.Probes.FirstOrDefault(probeState =>
                probeState.Name == state.ActiveManipulatorState.TargetInsertionProbeName
            );
            if (
                selectedTargetInsertionProbeState == null
                || !TargetInsertionProbeStates.Contains(selectedTargetInsertionProbeState)
            )
                TargetInsertionProbeIndex = -1;
            else
                TargetInsertionProbeIndex = TargetInsertionProbeStates.IndexOf(
                    selectedTargetInsertionProbeState
                );

            // Update dura offset.
            DuraOffset = state.ActiveManipulatorState.DuraOffset;

            // Update insertion base speed index.
            SelectedInsertionSpeedIndex = state.ActiveManipulatorState.InsertionSpeed switch
            {
                1 => 0,
                2 => 1,
                5 => 2,
                10 => 3,
                20 => 4,
                50 => 5,
                500 => 6,
                _ => 7, // Custom speed
            };

            // Update custom insertion speed or use default if not set.
            CustomInsertionSpeed = state.ActiveManipulatorState.InsertionSpeed;

            return;

            bool IsCoterminal(Vector3 first, Vector3 second)
            {
                return Mathf.Abs(first.x - second.x) % 360 < 0.01f
                    && Mathf.Abs(first.y - second.y) % 360 < 0.01f
                    && Mathf.Abs(first.z - second.z) % 360 < 0.01f;
            }
        }

        private void OnShuttingDown()
        {
            App.shuttingDown -= OnShuttingDown;
            _sceneStateSubscription.Dispose();
        }

        #region Commands

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
        private void SelectTargetInsertionProbe(int index)
        {
            // Mark the selected index.
            TargetInsertionProbeIndex = index;

            // Set the selected target insertion probe name in the store.
            var selectedTargetInsertionProbeState = TargetInsertionProbeStates.ElementAt(
                TargetInsertionProbeIndex
            );
            _storeService.Store.Dispatch(
                SceneActions.SET_TARGET_INSERTION_PROBE_NAME,
                (ActiveManipulatorId, selectedTargetInsertionProbeState.Name)
            );

            // TODO: Call ComputeEntryCoordinateTrajectory once it has been converted.
        }

        [ICommand]
        private void ResetTargetInsertionProbeSelection()
        {
            // Unset the selected target index.
            TargetInsertionProbeIndex = -1;

            // Clear the selected target insertion probe name in the store.
            _storeService.Store.Dispatch(
                SceneActions.SET_TARGET_INSERTION_PROBE_NAME,
                (ActiveManipulatorId, string.Empty)
            );
        }

        [ICommand]
        private void DriveToTargetEntryCoordinate()
        {
            ProbeService
                .DriveActiveProbeToTargetEntryCoordinate()
                .ContinueWith(task =>
                {
                    // Do not proceed if the drive failed.
                    if (!task.Result)
                        return;

                    // Complete the drive state if successful.
                    _storeService.Store.Dispatch(
                        SceneActions.COMPLETE_AUTOMATION_INTERMEDIATE_PROGRESS,
                        ActiveManipulatorId
                    );
                });
        }

        [ICommand]
        private void StopDriveToTargetEntryCoordinate()
        {
            ProbeService
                .StopActiveProbeDriveToTargetEntryCoordinate()
                .ContinueWith(task =>
                {
                    // Do not proceed if the drive failed.
                    if (!task.Result)
                        return;

                    // Reset back to calibrated state.
                    _storeService.Store.Dispatch(
                        SceneActions.SET_AUTOMATION_PROGRESS_STATE,
                        (ActiveManipulatorId, AutomationProgressState.IsCalibrated)
                    );
                });
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
        private void InsertionDrive()
        {
            // State is updated externally by the ProbeService.
            // _probeService.InsertionDriveActiveProbe();
        }

        [ICommand]
        private void InsertionExit()
        {
            // State is updated externally by the ProbeService.
            // _ = _probeService.InsertionExitActiveProbe();
        }

        [ICommand]
        private void StopInsertionDrive()
        {
            // State is updated externally by the ProbeService.
            ProbeService
                .StopInsertionDriveActiveProbe()
                .ContinueWith(task =>
                {
                    // Do not proceed if the stop failed.
                    if (!task.Result)
                        return;

                    // If the stop was successful, cancel the intermediate progress state.
                    _storeService.Store.Dispatch(
                        SceneActions.CANCEL_AUTOMATION_INTERMEDIATE_PROGRESS,
                        ActiveManipulatorId
                    );
                });
        }

        #endregion
    }
}
