using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using BrainAtlas;
using BrainAtlas.CoordinateSystems;
using EphysLink;
using KS.Diagnostics;
using Models;
using Models.Scene;
using Pinpoint.CoordinateSystems;
using Pinpoint.Probes;
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

        #region Trajectory and Visualization Fields

        /// <summary>
        ///     Trajectory broken into 3 stages (for 3 axes of movement).
        /// </summary>
        /// <remarks>Execution order: DV, AP, ML. Defaults to negative infinity when there is no trajectory.</remarks>
        private (Vector3 first, Vector3 second, Vector3 third) _trajectoryCoordinates = (
            Vector3.negativeInfinity,
            Vector3.negativeInfinity,
            Vector3.negativeInfinity
        );

        /// <summary>
        ///     Record the depth at the entry coordinate.
        /// </summary>
        /// <remarks>Used during insertion to calculate the actual distance needed to retract back to the entry coordinate.</remarks>
        private float _entryCoordinateDepth;

        /// <summary>
        ///     Trajectory line GameObjects.
        /// </summary>
        private (GameObject ap, GameObject ml, GameObject dv) _trajectoryLineGameObjects;

        /// <summary>
        ///     Trajectory line renderers.
        /// </summary>
        private (LineRenderer ap, LineRenderer ml, LineRenderer dv) _trajectoryLineRenderers;

        // Axes colors
        private static readonly Color AP_COLOR = new(1, 0.3215686f, 0.3215686f); // Red
        private static readonly Color ML_COLOR = new(0.2039216f, 0.6745098f, 0.8784314f); // Blue
        private static readonly Color DV_COLOR = new(1, 0.854902f, 0.4745098f); // Yellow

        // Trajectory line properties
        private const float LINE_WIDTH = 0.1f;
        private const int NUM_SEGMENTS = 2;

        // Safety margin
        private const float IDEAL_ENTRY_COORDINATE_TO_DURA_DISTANCE = 3.5f;

        // Movement speed
        private const float AUTOMATIC_MOVEMENT_SPEED = 0.5f; // mm/s
        #endregion

        #region Insertion Constants

        /// <summary>
        ///     Distance from target to start slowing down probe (mm).
        /// </summary>
        private const float NEAR_TARGET_DISTANCE = 1f;

        /// <summary>
        ///     Slowdown factor for the probe when it is near the target.
        /// </summary>
        private const float NEAR_TARGET_SPEED_MULTIPLIER = 2f / 3f;

        /// <summary>
        ///     Extra speed multiplier for the probe when it is exiting.
        /// </summary>
        private const int EXIT_DRIVE_SPEED_MULTIPLIER = 6;

        /// <summary>
        ///     Extra safety margin for the Dura to outside (mm).
        /// </summary>
        private const float DURA_MARGIN_DISTANCE = 0.2f;

        /// <summary>
        ///     Cancellation token source for stopping ongoing insertion/exit operations.
        /// </summary>
        private CancellationTokenSource _insertionDriveCts;

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
            TargetInsertionProbeIndex =
                (
                    selectedTargetInsertionProbeState == null
                    || !TargetInsertionProbeStates.Contains(selectedTargetInsertionProbeState)
                )
                    ? -1
                    : TargetInsertionProbeStates.IndexOf(selectedTargetInsertionProbeState);

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

            // Update drive past distance.
            DrivePastDistance = state.ActiveManipulatorState.DrivePastDistance;

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

            // Clean up trajectory lines
            RemoveTrajectoryLines();
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
            // If index is -1, reset the selection.
            if (index == -1)
            {
                ResetTargetInsertionProbeSelection();
                return;
            }

            // Get the selected target insertion probe
            var selectedTargetInsertionProbeState = TargetInsertionProbeStates.ElementAt(index);

            // Get the probe manager for the selected target
            var targetProbeManager = ProbeManager.Instances.FirstOrDefault(manager =>
                manager.name == selectedTargetInsertionProbeState.Name
            );

            if (targetProbeManager == null)
            {
                Debug.LogError(
                    $"Target probe manager not found: {selectedTargetInsertionProbeState.Name}"
                );
                return;
            }

            // Set the selected target insertion probe name in the store
            _storeService.Store.Dispatch(
                SceneActions.SET_TARGET_INSERTION_PROBE_NAME,
                (ActiveManipulatorId, selectedTargetInsertionProbeState.Name)
            );

            // Compute and visualize trajectory with the target probe manager
            var entryCoordinate = ComputeTargetEntryCoordinateTrajectory(targetProbeManager);

            if (float.IsNegativeInfinity(entryCoordinate.x))
            {
                Debug.LogWarning("Failed to compute trajectory for selected target probe");
            }
        }

        [ICommand]
        private void ResetTargetInsertionProbeSelection()
        {
            // Clear the selected target insertion probe name in the store.
            _storeService.Store.Dispatch(
                SceneActions.SET_TARGET_INSERTION_PROBE_NAME,
                (ActiveManipulatorId, string.Empty)
            );

            // Remove trajectory visualization
            RemoveTrajectoryLines();
        }

        [ICommand]
        private async void DriveToTargetEntryCoordinate()
        {
            // Validate trajectory exists
            if (float.IsNegativeInfinity(_trajectoryCoordinates.first.x))
            {
                Debug.LogError($"No trajectory planned for manipulator {ActiveManipulatorId}");
                return;
            }

            // Convert coordinates to manipulator positions
            var dvPosition = ConvertInsertionAPMLDVToManipulatorPosition(
                _trajectoryCoordinates.first
            );
            var apPosition = ConvertInsertionAPMLDVToManipulatorPosition(
                _trajectoryCoordinates.second
            );
            var mlPosition = ConvertInsertionAPMLDVToManipulatorPosition(
                _trajectoryCoordinates.third
            );

            // Check if conversion failed
            if (dvPosition == null || apPosition == null || mlPosition == null)
            {
                HandleDriveFailed(
                    "Failed to convert trajectory coordinates to manipulator positions"
                );
                return;
            }

            // Set state to driving
            _storeService.Store.Dispatch(
                SceneActions.SET_AUTOMATION_PROGRESS_STATE,
                (ActiveManipulatorId, AutomationProgressState.DrivingToTargetEntryCoordinate)
            );

            // Log that movement is starting
            OutputLog.Log(
                new[]
                {
                    "Automation",
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    "DriveToTargetEntryCoordinate",
                    ActiveManipulatorId,
                    "Start",
                }
            );

            // Stage 1: DV movement
            var dvResponse = await _ephysLinkService.SetPosition(
                new SetPositionRequest(
                    ActiveManipulatorId,
                    dvPosition.Value,
                    AUTOMATIC_MOVEMENT_SPEED
                )
            );

            if (EphysLinkService.HasError(dvResponse.Error))
            {
                HandleDriveFailed("Failed to move to DV position");
                return;
            }

            // Stage 2: AP movement
            var apResponse = await _ephysLinkService.SetPosition(
                new SetPositionRequest(
                    ActiveManipulatorId,
                    apPosition.Value,
                    AUTOMATIC_MOVEMENT_SPEED
                )
            );

            if (EphysLinkService.HasError(apResponse.Error))
            {
                HandleDriveFailed("Failed to move to AP position");
                return;
            }

            // Stage 3: ML movement
            var mlResponse = await _ephysLinkService.SetPosition(
                new SetPositionRequest(
                    ActiveManipulatorId,
                    mlPosition.Value,
                    AUTOMATIC_MOVEMENT_SPEED
                )
            );

            if (EphysLinkService.HasError(mlResponse.Error))
            {
                HandleDriveFailed("Failed to move to ML position");
                return;
            }

            // Record entry coordinate depth
            var finalPositionResponse = await _ephysLinkService.GetPosition(ActiveManipulatorId);
            if (EphysLinkService.HasError(finalPositionResponse.Error))
            {
                HandleDriveFailed("Failed to get final position at entry coordinate");
                return;
            }

            _entryCoordinateDepth = finalPositionResponse.Position.w;

            // Remove visualization lines
            RemoveTrajectoryLines();

            // Complete the drive state
            _storeService.Store.Dispatch(
                SceneActions.COMPLETE_AUTOMATION_INTERMEDIATE_PROGRESS,
                ActiveManipulatorId
            );

            // Log completion
            OutputLog.Log(
                new[]
                {
                    "Automation",
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    "DriveToTargetEntryCoordinate",
                    ActiveManipulatorId,
                    "Finish",
                }
            );
        }

        private void HandleDriveFailed(string errorMessage)
        {
            Debug.LogError(errorMessage);
            OutputLog.Log(
                new[]
                {
                    "Automation",
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    "DriveToTargetEntryCoordinate",
                    ActiveManipulatorId,
                    $"Failed: {errorMessage}",
                }
            );

            // Revert state to calibrated
            _storeService.Store.Dispatch(
                SceneActions.CANCEL_AUTOMATION_INTERMEDIATE_PROGRESS,
                ActiveManipulatorId
            );
        }

        [ICommand]
        private async void StopDriveToTargetEntryCoordinate()
        {
            // Log stop request
            OutputLog.Log(
                new[]
                {
                    "Automation",
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    "DriveToTargetEntryCoordinate",
                    ActiveManipulatorId,
                    "Stopped",
                }
            );

            // Send stop command to manipulator
            var stopResponse = await _ephysLinkService.Stop(ActiveManipulatorId);

            // Check for errors
            if (EphysLinkService.HasError(stopResponse))
            {
                Debug.LogError($"Failed to stop drive: {stopResponse}");
                return;
            }

            // Cancel intermediate progress (revert to IsCalibrated state)
            _storeService.Store.Dispatch(
                SceneActions.CANCEL_AUTOMATION_INTERMEDIATE_PROGRESS,
                ActiveManipulatorId
            );

            // Note: Do NOT remove trajectory lines on stop - user may want to restart
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
        private void SetInsertionSpeedIndex(int index)
        {
            // Map the index to the insertion speed value.
            var insertionSpeed = index switch
            {
                0 => 1,
                1 => 2,
                2 => 5,
                3 => 10,
                4 => 20,
                5 => 50,
                6 => 500,
                _ => CustomInsertionSpeed,
            };

            // Dispatch the insertion speed to the store.
            _storeService.Store.Dispatch(
                SceneActions.SET_INSERTION_SPEED,
                (ActiveManipulatorId, insertionSpeed)
            );
        }

        [ICommand]
        private void SetCustomInsertionSpeed(int customSpeed)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_INSERTION_SPEED,
                (ActiveManipulatorId, customSpeed)
            );
        }

        [ICommand]
        private async void InsertionDrive()
        {
            // Validate state is insertable.
            if (!IsInsertable(AutomationProgressState))
            {
                Debug.LogError(
                    $"Cannot drive: not in insertable state. Current state: {AutomationProgressState}"
                );
                return;
            }

            // Get scene state and target probe manager.
            var sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
            var manipulatorState = sceneState.ActiveManipulatorState;

            var targetProbeManager = ProbeManager.Instances.FirstOrDefault(m =>
                m.name == manipulatorState.TargetInsertionProbeName
            );
            if (targetProbeManager == null)
            {
                Debug.LogError(
                    $"Target probe manager not found: {manipulatorState.TargetInsertionProbeName}"
                );
                return;
            }

            // Convert speed from µm/s to mm/s.
            var baseSpeed = CustomInsertionSpeed / 1000f;
            var drivePastDistance = DrivePastDistance / 1000f;

            // Set up cancellation token.
            _insertionDriveCts = new CancellationTokenSource();

            // Store initial ETA for progress calculation.
            _originalETA = ComputeEtaSeconds(targetProbeManager, baseSpeed, drivePastDistance);

            try
            {
                while (AutomationProgressState != AutomationProgressState.AtTarget)
                {
                    // Check for cancellation.
                    _insertionDriveCts.Token.ThrowIfCancellationRequested();

                    // Get target depth.
                    var targetDepth = GetTargetDepth(targetProbeManager);

                    // Set state to next driving state.
                    _storeService.Store.Dispatch(
                        SceneActions.SET_AUTOMATION_PROGRESS_STATE_TO_NEXT_DRIVING,
                        ActiveManipulatorId
                    );

                    // Log set to driving state.
                    LogDriveToTargetInsertion(targetDepth, baseSpeed, drivePastDistance);

                    // Update ETA.
                    Eta = ComputeEtaSeconds(targetProbeManager, baseSpeed, drivePastDistance);
                    DriveProgressPercentage =
                        _originalETA > 0 ? 1f - (float)Eta / _originalETA : 0f;

                    // Handle driving state.
                    switch (AutomationProgressState)
                    {
                        case AutomationProgressState.DrivingToNearTarget:
                            // Drive to near target if not already there.
                            if (
                                GetCurrentDistanceToTarget(targetProbeManager)
                                > NEAR_TARGET_DISTANCE
                            )
                            {
                                var driveToNearTargetResponse = await _ephysLinkService.SetDepth(
                                    new SetDepthRequest(
                                        ActiveManipulatorId,
                                        targetDepth - NEAR_TARGET_DISTANCE,
                                        baseSpeed
                                    )
                                );

                                if (EphysLinkService.HasError(driveToNearTargetResponse.Error))
                                {
                                    Debug.LogError(
                                        $"Failed to drive to near target: {driveToNearTargetResponse.Error}"
                                    );
                                    return;
                                }
                            }
                            break;

                        case AutomationProgressState.DrivingToPastTarget:
                            // Drive past target at reduced speed.
                            var driveToPastTargetResponse = await _ephysLinkService.SetDepth(
                                new SetDepthRequest(
                                    ActiveManipulatorId,
                                    targetDepth + drivePastDistance,
                                    baseSpeed * NEAR_TARGET_SPEED_MULTIPLIER
                                )
                            );

                            if (EphysLinkService.HasError(driveToPastTargetResponse.Error))
                            {
                                Debug.LogError(
                                    $"Failed to drive past target: {driveToPastTargetResponse.Error}"
                                );
                                return;
                            }
                            break;

                        case AutomationProgressState.ReturningToTarget:
                            // Return to target at reduced speed.
                            var returnToTargetResponse = await _ephysLinkService.SetDepth(
                                new SetDepthRequest(
                                    ActiveManipulatorId,
                                    targetDepth,
                                    baseSpeed * NEAR_TARGET_SPEED_MULTIPLIER
                                )
                            );

                            if (EphysLinkService.HasError(returnToTargetResponse.Error))
                            {
                                Debug.LogError(
                                    $"Failed to return to target: {returnToTargetResponse.Error}"
                                );
                                return;
                            }
                            break;
                    }

                    // Complete this phase.
                    _storeService.Store.Dispatch(
                        SceneActions.COMPLETE_AUTOMATION_INTERMEDIATE_PROGRESS,
                        ActiveManipulatorId
                    );

                    // Log the event.
                    LogDriveToTargetInsertion(targetDepth, baseSpeed, drivePastDistance);
                }

                // Drive complete - reset ETA and progress.
                Eta = 0;
                DriveProgressPercentage = 1f;
            }
            catch (OperationCanceledException)
            {
                // Stopped by user - state already handled by StopInsertionDrive.
            }
            finally
            {
                _insertionDriveCts?.Dispose();
                _insertionDriveCts = null;
            }
        }

        [ICommand]
        private async void InsertionExit()
        {
            // Validate state is exitable.
            if (!IsExitable(AutomationProgressState))
            {
                Debug.LogError(
                    $"Cannot exit: not in exitable state. Current state: {AutomationProgressState}"
                );
                return;
            }

            // Get scene state and target probe manager.
            var sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
            var manipulatorState = sceneState.ActiveManipulatorState;

            var targetProbeManager = ProbeManager.Instances.FirstOrDefault(m =>
                m.name == manipulatorState.TargetInsertionProbeName
            );
            if (targetProbeManager == null)
            {
                Debug.LogError(
                    $"Target probe manager not found: {manipulatorState.TargetInsertionProbeName}"
                );
                return;
            }

            // Convert speed from µm/s to mm/s.
            var baseSpeed = CustomInsertionSpeed / 1000f;
            var drivePastDistance = DrivePastDistance / 1000f;

            // Set up cancellation token.
            _insertionDriveCts = new CancellationTokenSource();

            // Store initial ETA for progress calculation.
            _originalETA = ComputeEtaSeconds(targetProbeManager, baseSpeed, drivePastDistance);

            try
            {
                while (AutomationProgressState != AutomationProgressState.AtTargetEntryCoordinate)
                {
                    // Check for cancellation.
                    _insertionDriveCts.Token.ThrowIfCancellationRequested();

                    // Get dura depth for exit calculations.
                    var currentManipulatorState = _storeService
                        .Store.GetState<SceneState>(SliceNames.SCENE_SLICE)
                        .ActiveManipulatorState;
                    var duraDepth = currentManipulatorState.DuraDepth;

                    // Set state to next exiting state.
                    _storeService.Store.Dispatch(
                        SceneActions.SET_AUTOMATION_PROGRESS_STATE_TO_NEXT_EXITING,
                        ActiveManipulatorId
                    );

                    // Log set to exiting state.
                    LogDriveToTargetInsertion(duraDepth, baseSpeed);

                    // Update ETA.
                    Eta = ComputeEtaSeconds(targetProbeManager, baseSpeed, drivePastDistance);
                    DriveProgressPercentage =
                        _originalETA > 0 ? 1f - (float)Eta / _originalETA : 0f;

                    // Handle exiting state.
                    switch (AutomationProgressState)
                    {
                        case AutomationProgressState.ExitingToDura:
                            // Exit back up to the Dura.
                            var exitToDuraResponse = await _ephysLinkService.SetDepth(
                                new SetDepthRequest(
                                    ActiveManipulatorId,
                                    duraDepth,
                                    baseSpeed * EXIT_DRIVE_SPEED_MULTIPLIER
                                )
                            );

                            if (EphysLinkService.HasError(exitToDuraResponse.Error))
                            {
                                Debug.LogError(
                                    $"Failed to exit to dura: {exitToDuraResponse.Error}"
                                );
                                return;
                            }
                            break;

                        case AutomationProgressState.ExitingToMargin:
                            // Reset dura offset.
                            _storeService.Store.Dispatch(
                                SceneActions.RESET_DURA_OFFSET,
                                ActiveManipulatorId
                            );

                            // Exit to the safe margin above the Dura.
                            var exitToMarginResponse = await _ephysLinkService.SetDepth(
                                new SetDepthRequest(
                                    ActiveManipulatorId,
                                    duraDepth - DURA_MARGIN_DISTANCE,
                                    baseSpeed * EXIT_DRIVE_SPEED_MULTIPLIER
                                )
                            );

                            if (EphysLinkService.HasError(exitToMarginResponse.Error))
                            {
                                Debug.LogError(
                                    $"Failed to exit to margin: {exitToMarginResponse.Error}"
                                );
                                return;
                            }
                            break;

                        case AutomationProgressState.ExitingToTargetEntryCoordinate:
                            // Drive to the target entry coordinate.
                            var entryPosition = ConvertInsertionAPMLDVToManipulatorPosition(
                                _trajectoryCoordinates.third
                            );
                            if (entryPosition == null)
                            {
                                Debug.LogError(
                                    "Failed to convert entry coordinate to manipulator position"
                                );
                                return;
                            }

                            var exitToEntryCoordinateResponse = await _ephysLinkService.SetPosition(
                                new SetPositionRequest(
                                    ActiveManipulatorId,
                                    entryPosition.Value,
                                    AUTOMATIC_MOVEMENT_SPEED
                                )
                            );

                            if (EphysLinkService.HasError(exitToEntryCoordinateResponse.Error))
                            {
                                Debug.LogError(
                                    $"Failed to exit to entry coordinate: {exitToEntryCoordinateResponse.Error}"
                                );
                                return;
                            }
                            break;
                    }

                    // Complete this phase.
                    _storeService.Store.Dispatch(
                        SceneActions.COMPLETE_AUTOMATION_INTERMEDIATE_PROGRESS,
                        ActiveManipulatorId
                    );

                    // Log the event.
                    LogDriveToTargetInsertion(duraDepth, baseSpeed);
                }

                // Exit complete - reset ETA and progress.
                Eta = 0;
                DriveProgressPercentage = 1f;
            }
            catch (OperationCanceledException)
            {
                // Stopped by user - state already handled by StopInsertionDrive.
            }
            finally
            {
                _insertionDriveCts?.Dispose();
                _insertionDriveCts = null;
            }
        }

        [ICommand]
        private async void StopInsertionDrive()
        {
            // Cancel ongoing drive operation.
            _insertionDriveCts?.Cancel();

            // Send stop command to manipulator.
            var stopResponse = await _ephysLinkService.Stop(ActiveManipulatorId);

            if (EphysLinkService.HasError(stopResponse))
            {
                Debug.LogError($"Failed to stop: {stopResponse}");
                return;
            }

            // Log stop event.
            OutputLog.Log(
                new[]
                {
                    "Automation",
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    "Drive",
                    ActiveManipulatorId,
                    "Stop",
                }
            );

            // Cancel intermediate progress (revert state).
            _storeService.Store.Dispatch(
                SceneActions.CANCEL_AUTOMATION_INTERMEDIATE_PROGRESS,
                ActiveManipulatorId
            );

            // Reset ETA and progress.
            Eta = 0;
            DriveProgressPercentage = 0f;
        }

        [ICommand]
        private void SetDrivePastDistance(int distance)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_DRIVE_PAST_DISTANCE,
                (ActiveManipulatorId, distance)
            );
        }

        #endregion

        #region Trajectory Computation and Visualization

        /// <summary>
        ///     Compute the entry coordinate and trajectory for the target insertion. Also, create and update the trajectory visualization lines.
        /// </summary>
        /// <param name="targetProbeManager">The probe manager of the target insertion probe.</param>
        /// <returns>
        ///     The computed entry coordinate in AP, ML, DV coordinates. Vector3.negativeInfinity if target is unset or computation fails.
        /// </returns>
        private Vector3 ComputeTargetEntryCoordinateTrajectory(ProbeManager targetProbeManager)
        {
            // Validate target probe manager
            if (targetProbeManager == null)
            {
                Debug.LogError("Target probe manager is null");
                RemoveTrajectoryLines();
                return Vector3.negativeInfinity;
            }

            // Check if BrainAtlasManager is ready
            if (
                BrainAtlasManager.Instance == null
                || BrainAtlasManager.ActiveReferenceAtlas == null
            )
            {
                Debug.LogWarning("BrainAtlasManager not ready for trajectory computation");
                RemoveTrajectoryLines();
                return Vector3.negativeInfinity;
            }

            // Compute entry coordinate in world space
            var surfaceCoordinateWorldT = targetProbeManager.GetSurfaceCoordinateWorldT();
            var tipForwardWorldU = targetProbeManager
                .ProbeController.GetTipWorldU()
                .tipForwardWorldU;

            var entryCoordinateWorld =
                surfaceCoordinateWorldT
                - tipForwardWorldU * IDEAL_ENTRY_COORDINATE_TO_DURA_DISTANCE;

            // Convert to AP/ML/DV coordinates
            var entryCoordinateAtlasU = BrainAtlasManager.ActiveReferenceAtlas.World2Atlas(
                entryCoordinateWorld
            );
            var entryCoordinateAPMLDV = BrainAtlasManager.ActiveAtlasTransform.U2T(
                entryCoordinateAtlasU
            );

            // Get current manipulator coordinate.
            var currentState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
            var visualizationProbeName = currentState.ActiveManipulatorState.VisualizationProbeName;
            var currentCoordinate = currentState
                .Probes.FirstOrDefault(state => state.Name == visualizationProbeName)!
                .APMLDV;

            // Create 3-stage trajectory
            // Stage 1: DV movement (change depth only)
            _trajectoryCoordinates.first = new Vector3(
                currentCoordinate.x,
                currentCoordinate.y,
                entryCoordinateAPMLDV.z
            );

            // Stage 2: AP movement (change AP only)
            _trajectoryCoordinates.second = new Vector3(
                entryCoordinateAPMLDV.x,
                currentCoordinate.y,
                entryCoordinateAPMLDV.z
            );

            // Stage 3: ML movement (final position)
            _trajectoryCoordinates.third = entryCoordinateAPMLDV;

            // Create and update trajectory lines
            CreateTrajectoryLines();
            UpdateTrajectoryLines();

            // Return final entry coordinate
            return _trajectoryCoordinates.third;
        }

        /// <summary>
        ///     Create the trajectory line game objects and line renderers (if needed).
        /// </summary>
        private void CreateTrajectoryLines()
        {
            // Exit if they already exist
            if (_trajectoryLineGameObjects.ap != null)
                return;

            // Create the trajectory line game objects
            _trajectoryLineGameObjects = (
                new GameObject("APTrajectoryLine") { layer = 5 },
                new GameObject("MLTrajectoryLine") { layer = 5 },
                new GameObject("DVTrajectoryLine") { layer = 5 }
            );

            // Create the line renderers
            _trajectoryLineRenderers = (
                _trajectoryLineGameObjects.ap.AddComponent<LineRenderer>(),
                _trajectoryLineGameObjects.ml.AddComponent<LineRenderer>(),
                _trajectoryLineGameObjects.dv.AddComponent<LineRenderer>()
            );

            // Apply materials
            var defaultSpriteShader = Shader.Find("Sprites/Default");
            _trajectoryLineRenderers.ap.material = new Material(defaultSpriteShader)
            {
                color = AP_COLOR,
            };
            _trajectoryLineRenderers.ml.material = new Material(defaultSpriteShader)
            {
                color = ML_COLOR,
            };
            _trajectoryLineRenderers.dv.material = new Material(defaultSpriteShader)
            {
                color = DV_COLOR,
            };

            // Set line widths
            _trajectoryLineRenderers.ap.startWidth = _trajectoryLineRenderers.ap.endWidth =
                LINE_WIDTH;
            _trajectoryLineRenderers.ml.startWidth = _trajectoryLineRenderers.ml.endWidth =
                LINE_WIDTH;
            _trajectoryLineRenderers.dv.startWidth = _trajectoryLineRenderers.dv.endWidth =
                LINE_WIDTH;

            // Set segment counts
            _trajectoryLineRenderers.ap.positionCount = NUM_SEGMENTS;
            _trajectoryLineRenderers.ml.positionCount = NUM_SEGMENTS;
            _trajectoryLineRenderers.dv.positionCount = NUM_SEGMENTS;
        }

        /// <summary>
        ///     Update the trajectory line positions.
        /// </summary>
        private void UpdateTrajectoryLines()
        {
            if (
                BrainAtlasManager.Instance == null
                || BrainAtlasManager.ActiveReferenceAtlas == null
            )
                return;

            // Get active probe manager for current tip position
            var sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
            var activeProbeManager = ProbeManager.Instances.FirstOrDefault(manager =>
                manager.name == sceneState.ActiveManipulatorState.VisualizationProbeName
            );

            if (activeProbeManager == null)
                return;

            // DV line: From current probe tip to first coordinate
            _trajectoryLineRenderers.dv.SetPosition(
                0,
                activeProbeManager.ProbeController.ProbeTipT.position
            );
            _trajectoryLineRenderers.dv.SetPosition(
                1,
                BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(
                    BrainAtlasManager.ActiveAtlasTransform.T2U_Vector(_trajectoryCoordinates.first)
                )
            );

            // AP line: From first to second coordinate
            _trajectoryLineRenderers.ap.SetPosition(
                0,
                BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(
                    BrainAtlasManager.ActiveAtlasTransform.T2U_Vector(_trajectoryCoordinates.first)
                )
            );
            _trajectoryLineRenderers.ap.SetPosition(
                1,
                BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(
                    BrainAtlasManager.ActiveAtlasTransform.T2U_Vector(_trajectoryCoordinates.second)
                )
            );

            // ML line: From second to third coordinate
            _trajectoryLineRenderers.ml.SetPosition(
                0,
                BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(
                    BrainAtlasManager.ActiveAtlasTransform.T2U_Vector(_trajectoryCoordinates.second)
                )
            );
            _trajectoryLineRenderers.ml.SetPosition(
                1,
                BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(
                    BrainAtlasManager.ActiveAtlasTransform.T2U_Vector(_trajectoryCoordinates.third)
                )
            );
        }

        /// <summary>
        ///     Destroy the trajectory line game objects and line renderers. Also reset the references.
        /// </summary>
        private void RemoveTrajectoryLines()
        {
            // Destroy the objects
            if (_trajectoryLineGameObjects.ap != null)
                UnityEngine.Object.Destroy(_trajectoryLineGameObjects.ap);
            if (_trajectoryLineGameObjects.ml != null)
                UnityEngine.Object.Destroy(_trajectoryLineGameObjects.ml);
            if (_trajectoryLineGameObjects.dv != null)
                UnityEngine.Object.Destroy(_trajectoryLineGameObjects.dv);

            // Reset the references
            _trajectoryLineGameObjects = (null, null, null);
            _trajectoryLineRenderers = (null, null, null);

            // Reset trajectory coordinates
            _trajectoryCoordinates = (
                Vector3.negativeInfinity,
                Vector3.negativeInfinity,
                Vector3.negativeInfinity
            );
        }

        /// <summary>
        ///     Convert insertion AP, ML, DV coordinates to manipulator translation stage position.
        /// </summary>
        /// <param name="insertionAPMLDV">AP, ML, DV coordinates from an insertion.</param>
        /// <returns>Computed manipulator translation stage positions to match this coordinate, or null if conversion fails.</returns>
        private Vector4? ConvertInsertionAPMLDVToManipulatorPosition(Vector3 insertionAPMLDV)
        {
            var sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
            var activeManipulatorState = sceneState.ActiveManipulatorState;

            // Validate we have the necessary data
            if (BrainAtlasManager.ActiveReferenceAtlas == null)
            {
                Debug.LogError("BrainAtlasManager.ActiveReferenceAtlas is null");
                return null;
            }

            if (sceneState.ManipulatorCoordinateSpace == null)
            {
                Debug.LogError("ManipulatorCoordinateSpace is null");
                return null;
            }

            // Convert AP/ML/DV to world coordinate
            var convertToWorld = BrainAtlasManager.ActiveReferenceAtlas.Atlas2World_Vector(
                BrainAtlasManager.ActiveAtlasTransform.T2U_Vector(insertionAPMLDV)
            );

            // Create coordinate transform based on manipulator configuration
            var numAxes = sceneState.NumberOfAxesOnManipulator;
            var isRightHanded = activeManipulatorState.Handedness == ManipulatorHandedness.Right;
            var yaw = activeManipulatorState.Angles.x;
            var pitch = activeManipulatorState.Angles.y;

            CoordinateTransform coordinateTransform = numAxes switch
            {
                4 => isRightHanded
                    ? new FourAxisRightHandedManipulatorTransform(yaw)
                    : new FourAxisLeftHandedManipulatorTransform(yaw),
                3 => new ThreeAxisLeftHandedTransform(yaw, pitch),
                _ => null,
            };

            if (coordinateTransform == null)
            {
                Debug.LogError($"Unsupported number of axes: {numAxes}");
                return null;
            }

            // Convert to manipulator space
            var posInManipulatorSpace = sceneState.ManipulatorCoordinateSpace.World2Space(
                convertToWorld
            );
            Vector4 posInManipulatorTransform = coordinateTransform.U2T(posInManipulatorSpace);

            // Apply brain surface offset (DuraOffset)
            var duraOffset = activeManipulatorState.DuraOffset;
            posInManipulatorTransform.w -= float.IsNaN(duraOffset) ? 0 : duraOffset;

            // Apply coordinate offsets and return result
            return posInManipulatorTransform + activeManipulatorState.ReferenceCoordinateOffset;
        }

        #endregion

        #region Insertion Helper Methods

        /// <summary>
        ///     Check if the current state allows starting or resuming insertion drive.
        /// </summary>
        private static bool IsInsertable(AutomationProgressState state)
        {
            return state
                is AutomationProgressState.AtDuraInsert
                    or AutomationProgressState.AtNearTargetInsert
                    or AutomationProgressState.AtPastTarget
                    or AutomationProgressState.AtTarget;
        }

        /// <summary>
        ///     Check if the current state allows starting or resuming exit.
        /// </summary>
        private static bool IsExitable(AutomationProgressState state)
        {
            return state
                is AutomationProgressState.AtDuraInsert
                    or AutomationProgressState.AtNearTargetInsert
                    or AutomationProgressState.AtPastTarget
                    or AutomationProgressState.AtTarget
                    or AutomationProgressState.AtDuraExit
                    or AutomationProgressState.AtExitMargin;
        }

        /// <summary>
        ///     Compute the target coordinate adjusted for the probe's actual position.
        /// </summary>
        /// <param name="targetInsertionProbeManager">Target probe manager.</param>
        /// <returns>APMLDV coordinates of where the probe should actually go.</returns>
        private Vector3 GetOffsetAdjustedTargetCoordinate(ProbeManager targetInsertionProbeManager)
        {
            var sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
            var visualizationProbeName = sceneState.ActiveManipulatorState.VisualizationProbeName;
            var visualizationProbeManager = ProbeManager.Instances.FirstOrDefault(m =>
                m.name == visualizationProbeName
            );

            if (visualizationProbeManager == null)
                return Vector3.negativeInfinity;

            // Extract target insertion.
            var targetInsertion = targetInsertionProbeManager.ProbeController.Insertion;

            var targetWorldT = targetInsertion.PositionWorldT();
            var relativePositionWorldT =
                visualizationProbeManager.ProbeController.Insertion.PositionWorldT() - targetWorldT;
            var probeTipTForward = visualizationProbeManager.ProbeController.ProbeTipT.forward;
            var offsetAdjustedRelativeTargetPositionWorldT = Vector3.ProjectOnPlane(
                relativePositionWorldT,
                probeTipTForward
            );
            var offsetAdjustedTargetCoordinateWorldT =
                targetWorldT + offsetAdjustedRelativeTargetPositionWorldT;

            // Convert worldT to AtlasT then switch axes to get APMLDV.
            var offsetAdjustedTargetCoordinateAtlasT =
                BrainAtlasManager.ActiveReferenceAtlas.World2Atlas(
                    offsetAdjustedTargetCoordinateWorldT
                );
            return BrainAtlasManager.ActiveAtlasTransform.U2T_Vector(
                offsetAdjustedTargetCoordinateAtlasT
            );
        }

        /// <summary>
        ///     Compute the absolute distance from the target insertion to the Dura.
        /// </summary>
        /// <param name="targetInsertionProbeManager">Target to compute distance to.</param>
        /// <returns>Distance in mm to the target from the Dura.</returns>
        private float GetTargetDistanceToDura(ProbeManager targetInsertionProbeManager)
        {
            var sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
            var duraCoordinate = sceneState.ActiveManipulatorState.DuraCoordinate;
            return Vector3.Distance(
                GetOffsetAdjustedTargetCoordinate(targetInsertionProbeManager),
                new Vector3(duraCoordinate.x, duraCoordinate.y, duraCoordinate.z)
            );
        }

        /// <summary>
        ///     Compute the current distance to the target insertion.
        /// </summary>
        /// <param name="targetInsertionProbeManager">Target probe manager.</param>
        /// <returns>Distance in mm to the target from the probe.</returns>
        private float GetCurrentDistanceToTarget(ProbeManager targetInsertionProbeManager)
        {
            var sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
            var visualizationProbeName = sceneState.ActiveManipulatorState.VisualizationProbeName;
            var visualizationProbeState = sceneState.Probes.FirstOrDefault(p =>
                p.Name == visualizationProbeName
            );

            if (visualizationProbeState == null)
                return float.NaN;

            return Vector3.Distance(
                visualizationProbeState.APMLDV,
                GetOffsetAdjustedTargetCoordinate(targetInsertionProbeManager)
            );
        }

        /// <summary>
        ///     Compute the target depth for the probe to drive to.
        /// </summary>
        /// <param name="targetInsertionProbeManager">Target to drive (insert) to.</param>
        /// <returns>The depth the manipulator needs to drive to reach the target insertion.</returns>
        private float GetTargetDepth(ProbeManager targetInsertionProbeManager)
        {
            var sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
            var duraDepth = sceneState.ActiveManipulatorState.DuraDepth;
            return duraDepth + GetTargetDistanceToDura(targetInsertionProbeManager);
        }

        /// <summary>
        ///     Compute the ETA in seconds for a probe to reach a target insertion (or exit).
        /// </summary>
        /// <param name="targetInsertionProbeManager">Target to calculate ETA to.</param>
        /// <param name="baseSpeed">Base driving speed in mm/s.</param>
        /// <param name="drivePastDistance">Distance to drive past target in mm.</param>
        /// <returns>ETA in seconds for reaching a target or exiting.</returns>
        private int ComputeEtaSeconds(
            ProbeManager targetInsertionProbeManager,
            float baseSpeed,
            float drivePastDistance
        )
        {
            var sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
            var manipulatorState = sceneState.ActiveManipulatorState;

            var distanceToTarget = GetCurrentDistanceToTarget(targetInsertionProbeManager);
            var targetDistanceToDura = GetTargetDistanceToDura(targetInsertionProbeManager);
            var actualExitMarginToDuraDistance = manipulatorState.DuraDepth - _entryCoordinateDepth;

            var secondsToDestination = AutomationProgressState switch
            {
                AutomationProgressState.DrivingToNearTarget => Mathf.Max(
                    0,
                    distanceToTarget - NEAR_TARGET_DISTANCE
                ) / baseSpeed
                    + (NEAR_TARGET_DISTANCE + 2 * drivePastDistance)
                        / (baseSpeed * NEAR_TARGET_SPEED_MULTIPLIER),
                AutomationProgressState.DrivingToPastTarget => (
                    distanceToTarget + 2 * drivePastDistance
                ) / (baseSpeed * NEAR_TARGET_SPEED_MULTIPLIER),
                AutomationProgressState.ReturningToTarget => distanceToTarget
                    / (baseSpeed * NEAR_TARGET_SPEED_MULTIPLIER),
                AutomationProgressState.ExitingToDura => (targetDistanceToDura - distanceToTarget)
                    / (baseSpeed * EXIT_DRIVE_SPEED_MULTIPLIER)
                    + DURA_MARGIN_DISTANCE / (baseSpeed * EXIT_DRIVE_SPEED_MULTIPLIER)
                    + actualExitMarginToDuraDistance / AUTOMATIC_MOVEMENT_SPEED,
                AutomationProgressState.ExitingToMargin => (
                    DURA_MARGIN_DISTANCE - (distanceToTarget - targetDistanceToDura)
                ) / (baseSpeed * EXIT_DRIVE_SPEED_MULTIPLIER)
                    + actualExitMarginToDuraDistance / AUTOMATIC_MOVEMENT_SPEED,
                AutomationProgressState.ExitingToTargetEntryCoordinate => (
                    IDEAL_ENTRY_COORDINATE_TO_DURA_DISTANCE
                    - (distanceToTarget - targetDistanceToDura)
                ) / AUTOMATIC_MOVEMENT_SPEED,
                _ => 0,
            };

            return (int)secondsToDestination;
        }

        /// <summary>
        ///     Log a drive event.
        /// </summary>
        /// <param name="targetDepth">Target depth of drive.</param>
        /// <param name="baseSpeed">Base speed of drive.</param>
        /// <param name="drivePastDistance">Distance (mm) driven past the target.</param>
        private void LogDriveToTargetInsertion(
            float targetDepth,
            float baseSpeed,
            float drivePastDistance = 0
        )
        {
            OutputLog.Log(
                new[]
                {
                    "Automation",
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    "DriveToTargetInsertion",
                    ActiveManipulatorId,
                    AutomationProgressState.ToString(),
                    (targetDepth * 1000).ToString(CultureInfo.InvariantCulture),
                    (baseSpeed * 1000).ToString(CultureInfo.InvariantCulture),
                    (drivePastDistance * 1000).ToString(CultureInfo.InvariantCulture),
                }
            );
        }

        #endregion
    }
}
