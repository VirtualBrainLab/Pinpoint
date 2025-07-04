using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Models;
using Models.Scene;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class AutomationViewModel
    {
        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;

        #endregion
        #region Properties

        [ObservableProperty]
        private bool _isAutomationEnabled = true;

        [ObservableProperty]
        private AutomationProgressState _automationProgressState;

        [ObservableProperty]
        private Vector4 _referenceCoordinate;

        /// <summary>
        /// Selected target insertion probe manager dropdown index (including the none option).
        /// </summary>
        [ObservableProperty]
        private int _selectedTargetInsertionProbeIndex;

        /// <summary>
        /// Filtered list of targetable insertion probes for the active manipulator probe.
        ///
        /// For insertions that are co-terminal and have not been selected yet. Does not include the "None" option.
        /// </summary>
        [ObservableProperty]
        private List<ProbeState> _targetInsertionProbeStates = new();

        [ObservableProperty]
        private float _duraOffset;

        [ObservableProperty]
        private int _selectedInsertionSpeedIndex;

        /// <summary>
        /// Custom base insertion drive speed (µm/s).
        /// </summary>
        [ObservableProperty]
        private int _customInsertionSpeed;

        /// <summary>
        /// Distance to drive past the target entry coordinate (µm).
        /// </summary>
        [ObservableProperty]
        private int _drivePastDistance;

        /// <summary>
        /// ETA to reach the target or to exit (seconds).
        /// </summary>
        [ObservableProperty]
        private int _eta;

        #endregion

        public AutomationViewModel(StoreService storeService)
        {
            // Register services.
            _storeService = storeService;

            // Initialize properties from the store.
            var initialAutomationState = _storeService.Store.GetState<SceneState>(
                SliceNames.SCENE_SLICE
            );
            OnSceneStateChanged(initialAutomationState);

            // Subscribe to state changes.
            _sceneStateSubscription = _storeService.Store.Subscribe(
                state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                OnSceneStateChanged
            );
            PropertyChanged += OnPropertyChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnSceneStateChanged(SceneState state)
        {
            // Check if an active manipulator probe is selected.
            IsAutomationEnabled = state.ActiveProbeState is { IsEphysLinkControlled: true };

            // Exit if not enabled.
            if (!IsAutomationEnabled)
            {
                return;
            }

            // Set the automation progress state from the active probe state.
            AutomationProgressState = state.ActiveProbeState.AutomationProgressState;

            // Get the active manipulator's reference coordinate.
            ReferenceCoordinate = state.ActiveProbeState.ReferenceCoordinateOffset;

            // Get the list of targetable insertion probes for the active manipulator probe.
            TargetInsertionProbeStates = state
                .Probes
                // 1. Not manipulator controlled.
                .Where(probeState => !probeState.IsEphysLinkControlled)
                // 2. Co-terminal with the active probe angles.
                .Where(probeState => IsCoterminal(probeState.Angles, state.ActiveProbeState.Angles))
                // TODO: 3. Is in the brain (non-NaN entry coordinate).
                // 4. Is not already selected by other manipulator probes (unless it was selected by this active probe).
                .Where(probeState =>
                    !state
                        .Probes.Where(searchProbeState =>
                            searchProbeState != state.ActiveProbeState
                        )
                        .Where(searchProbeState => searchProbeState.IsEphysLinkControlled)
                        .Select(otherManipulatorProbes =>
                            otherManipulatorProbes.SelectedTargetInsertionProbeUUID
                        )
                        .Contains(probeState.UUID)
                )
                .ToList();

            // Get the index of the selected target insertion probe.
            var selectedTargetInsertionProbeUUID = state
                .ActiveProbeState
                .SelectedTargetInsertionProbeUUID;
            var selectedTargetInsertionProbeState = state.Probes.FirstOrDefault(probeState =>
                probeState.UUID == selectedTargetInsertionProbeUUID
            );
            if (
                selectedTargetInsertionProbeState == null
                || !TargetInsertionProbeStates.Contains(selectedTargetInsertionProbeState)
            )
            {
                SelectedTargetInsertionProbeIndex = 0;
            }
            else
            {
                SelectedTargetInsertionProbeIndex = TargetInsertionProbeStates.IndexOf(
                    selectedTargetInsertionProbeState
                );
            }

            // Update dura offset.
            DuraOffset = state.ActiveProbeState.DuraDepth;

            return;

            bool IsCoterminal(Vector3 first, Vector3 second)
            {
                return Mathf.Abs(first.x - second.x) % 360 < 0.01f
                    && Mathf.Abs(first.y - second.y) % 360 < 0.01f
                    && Mathf.Abs(first.z - second.z) % 360 < 0.01f;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ReferenceCoordinate):
                    // BUG: This won't update the probe's game object position.
                    _storeService.Store.Dispatch(
                        SceneActions.SET_ACTIVE_PROBE_REFERENCE_COORDINATE,
                        ReferenceCoordinate
                    );
                    break;
                case nameof(SelectedTargetInsertionProbeIndex):
                    // Reset the selected target insertion probe if the index is 0 (None).
                    if (SelectedTargetInsertionProbeIndex == 0)
                    {
                        _storeService.Store.Dispatch(
                            SceneActions.SET_SELECTED_TARGET_INSERTION_PROBE_UUID,
                            string.Empty
                        );
                    }
                    // Otherwise, subtract the None option and set the selected target insertion probe UUID.
                    else
                    {
                        var selectedTargetInsertionProbeState = TargetInsertionProbeStates[
                            SelectedTargetInsertionProbeIndex - 1
                        ];
                        _storeService.Store.Dispatch(
                            SceneActions.SET_SELECTED_TARGET_INSERTION_PROBE_UUID,
                            selectedTargetInsertionProbeState.UUID
                        );
                        // TODO: Call ComputeEntryCoordinateTrajectory once it has been converted.
                    }
                    break;
                case nameof(DuraOffset):
                    // BUG: This won't update the probe's game object position.
                    _storeService.Store.Dispatch(
                        SceneActions.SET_ACTIVE_PROBE_DURA_OFFSET,
                        DuraOffset
                    );
                    break;
            }
        }

        private void OnShuttingDown()
        {
            App.shuttingDown -= OnShuttingDown;
            _sceneStateSubscription.Dispose();
        }

        #region Commands

        [ICommand]
        private void ResetReferenceCoordinate()
        {
            ProbeService
                .ResetActiveProbeReferenceCoordinate()
                .ContinueWith(task =>
                {
                    // Do not proceed if the reset failed.
                    if (!task.Result)
                    {
                        return;
                    }

                    // If the reset was successful, set the active probe's automation progress state to be calibrated.
                    _storeService.Store.Dispatch(
                        SceneActions.SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE,
                        AutomationProgressState.IsCalibrated
                    );
                });
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
                    {
                        return;
                    }

                    // Complete the drive state if successful.
                    _storeService.Store.Dispatch(
                        SceneActions.COMPLETE_ACTIVE_PROBE_AUTOMATION_INTERMEDIATE_PROGRESS
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
                    {
                        return;
                    }

                    // Reset back to calibrated state.
                    _storeService.Store.Dispatch(
                        SceneActions.SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE,
                        AutomationProgressState.IsCalibrated
                    );
                });
        }

        [ICommand]
        private void ResetDuraOffset()
        {
            ProbeService
                .ResetActiveProbeDuraOffset()
                .ContinueWith(task =>
                {
                    // Do not proceed if the reset failed.
                    if (!task.Result)
                    {
                        return;
                    }

                    // If the reset was successful, set calibrated to the Dura.
                    _storeService.Store.Dispatch(
                        SceneActions.SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE,
                        AutomationProgressState.AtDuraInsert
                    );
                });
        }

        #endregion
    }
}
