using System.Collections.Generic;
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
    public partial class AutomationInspectorViewModel
    {
        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;

        [Service]
        private ProbeService _probeService;

        #endregion

        #region Properties

        [ObservableProperty]
        private bool _isAutomationEnabled = true;

        [ObservableProperty]
        private AutomationProgressState _automationProgressState;

        [ObservableProperty]
        private Vector4 _referenceCoordinate;

        /// <summary>
        ///     Selected target insertion probe manager dropdown index (including the none option).
        /// </summary>
        [ObservableProperty]
        private int _selectedTargetInsertionProbeIndex;

        /// <summary>
        ///     Filtered list of targetable insertion probes for the active manipulator probe.
        ///     For insertions that are co-terminal and have not been selected yet. Does not include the "None" option.
        /// </summary>
        [ObservableProperty]
        private List<ProbeState> _targetInsertionProbeStates;

        [ObservableProperty]
        private float _duraOffset;

        [ObservableProperty]
        private int _selectedInsertionBaseSpeedIndex;

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
        ///     ETA to reach the target or to exit (seconds).
        /// </summary>
        [ObservableProperty]
        private int _eta;

        #endregion

        public AutomationInspectorViewModel(StoreService storeService)
        {
            // Register services.
            _storeService = storeService;

            // Subscribe to state changes and initialize properties.
            _sceneStateSubscription = _storeService.Store.Subscribe(
                state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                OnSceneStateChanged,
                new SubscribeOptions<SceneState> { fireImmediately = true }
            );
            PropertyChanged += OnPropertyChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnSceneStateChanged(SceneState state)
        {
            // Check if an active manipulator probe is selected.
            IsAutomationEnabled = state.ActiveProbeState != null; //is { IsEphysLinkControlled: true };

            // Exit if not enabled.
            if (!IsAutomationEnabled)
                return;

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
                            otherManipulatorProbes.SelectedTargetInsertionProbeName
                        )
                        .Contains(probeState.Name)
                )
                .ToList();

            // Get the index of the selected target insertion probe.
            var selectedTargetInsertionProbeUUID = state
                .ActiveProbeState
                .SelectedTargetInsertionProbeName;
            var selectedTargetInsertionProbeState = state.Probes.FirstOrDefault(probeState =>
                probeState.Name == selectedTargetInsertionProbeUUID
            );
            if (
                selectedTargetInsertionProbeState == null
                || !TargetInsertionProbeStates.Contains(selectedTargetInsertionProbeState)
            )
                SelectedTargetInsertionProbeIndex = 0;
            else
                SelectedTargetInsertionProbeIndex = TargetInsertionProbeStates
                    .ToList()
                    .IndexOf(selectedTargetInsertionProbeState);

            // Update dura offset.
            DuraOffset = state.ActiveProbeState.DuraDepth;

            // Update insertion base speed index.
            SelectedInsertionBaseSpeedIndex = state.ActiveProbeState.InsertionBaseSpeed switch
            {
                2 => 0,
                5 => 1,
                10 => 2,
                500 => 3,
                _ => 4, // Custom speed
            };

            // Update custom insertion speed or use default if not set.
            CustomInsertionSpeed = state.ActiveProbeState.InsertionBaseSpeed is 2 or 5 or 10 or 500
                ? 20
                : state.ActiveProbeState.InsertionBaseSpeed;

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
                            SceneActions.SET_SELECTED_TARGET_INSERTION_PROBE_NAME,
                            string.Empty
                        );
                    }
                    // Otherwise, subtract the None option and set the selected target insertion probe UUID.
                    else
                    {
                        var selectedTargetInsertionProbeState =
                            TargetInsertionProbeStates.ElementAt(
                                SelectedTargetInsertionProbeIndex - 1
                            );
                        _storeService.Store.Dispatch(
                            SceneActions.SET_SELECTED_TARGET_INSERTION_PROBE_NAME,
                            selectedTargetInsertionProbeState.Name
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
                case nameof(SelectedInsertionBaseSpeedIndex) or nameof(CustomInsertionSpeed):
                    var pickedInsertionBaseSpeed = SelectedInsertionBaseSpeedIndex switch
                    {
                        0 => 2,
                        1 => 5,
                        2 => 10,
                        3 => 500,
                        _ => CustomInsertionSpeed,
                    };
                    _storeService.Store.Dispatch(
                        SceneActions.SET_ACTIVE_PROBE_INSERTION_BASE_SPEED,
                        pickedInsertionBaseSpeed
                    );
                    break;
                case nameof(DrivePastDistance):
                    _storeService.Store.Dispatch(
                        SceneActions.SET_ACTIVE_PROBE_DRIVE_PAST_DISTANCE,
                        DrivePastDistance
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
                        return;

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
                        return;

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
                        return;

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
                        return;

                    // If the reset was successful, set calibrated to the Dura.
                    _storeService.Store.Dispatch(
                        SceneActions.SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE,
                        AutomationProgressState.AtDuraInsert
                    );
                });
        }

        [ICommand]
        private void InsertionDrive()
        {
            // State is updated externally by the ProbeService.
            _probeService.InsertionDriveActiveProbe();
        }

        [ICommand]
        private void InsertionExit()
        {
            // State is updated externally by the ProbeService.
            _ = _probeService.InsertionExitActiveProbe();
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
                        SceneActions.CANCEL_ACTIVE_PROBE_AUTOMATION_INTERMEDIATE_PROGRESS
                    );
                });
        }

        #endregion
    }
}
