using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using BrainAtlas;
using EphysLink;
using KS.Diagnostics;
using Models;
using Models.Scene;
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
            var activeProbeManager = ProbeManager.Instances.FirstOrDefault(manager =>
                manager.name == sceneState.ActiveManipulatorState.VisualizationProbeName
            );

            if (activeProbeManager == null || !activeProbeManager.IsEphysLinkControlled)
            {
                Debug.LogError("Active probe manager not found or not EphysLink controlled");
                return null;
            }

            // Use the ManipulatorBehaviorController's conversion method
            return activeProbeManager.ManipulatorBehaviorController.ConvertInsertionAPMLDVToManipulatorPosition(
                insertionAPMLDV
            );
        }

        #endregion
    }
}
