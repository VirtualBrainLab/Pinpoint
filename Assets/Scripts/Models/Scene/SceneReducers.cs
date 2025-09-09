using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.Redux;
using UnityEngine;
using Utils.Types;

namespace Models.Scene
{
    public static class SceneReducers
    {
        #region Probe List Reducers

        public static SceneState AddProbeReducer(SceneState state, IAction<ProbeType> action)
        {
            var newProbesList = state.Probes.ToList();
            newProbesList.Add(new ProbeState { ProbeType = action.payload });
            return state with { Probes = newProbesList };
        }

        public static SceneState DuplicateProbeReducer(SceneState state, IAction<string> action)
        {
            // Find the probe to duplicate.
            var probeToDuplicate = state.Probes.FirstOrDefault(probe =>
                probe.Name == action.payload
            );
            if (probeToDuplicate == null)
                return state; // If not found, return the state unchanged.

            // Create a copy of the probes list
            var newProbesList = state.Probes.ToList();

            // Create a new probe with a new UUID but same properties as the original.
            var duplicatedProbe = probeToDuplicate with
            {
                Name = Guid.NewGuid().ToString(),
            };

            // Add the duplicated probe to the list.
            newProbesList.Add(duplicatedProbe);

            return state with
            {
                Probes = newProbesList,
            };
        }

        /// <summary>
        ///     Remove all probes with the specified name.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="action">Target probe's name in payload.</param>
        /// <returns></returns>
        public static SceneState RemoveProbeReducer(SceneState state, IAction<string> action)
        {
            // Remove all probes with the specified UUID.
            var newProbesList = state.Probes.ToList();

            // If no probes were removed, return the state unchanged.
            if (newProbesList.RemoveAll(probeState => probeState.Name == action.payload) == 0)
                return state;

            // Update the state with the new probes list, and update the active probe name if it was removed.
            return state with
            {
                Probes = newProbesList,
                ActiveProbeName =
                    state.ActiveProbeName == action.payload ? string.Empty : state.ActiveProbeName,
            };
        }

        public static SceneState RemoveAllProbesReducer(SceneState state, IAction action)
        {
            return state with { Probes = new List<ProbeState>(), ActiveProbeName = string.Empty };
        }

        #endregion

        #region Active Probe Reducers

        public static SceneState SetActiveProbeReducer(SceneState state, IAction<string> action)
        {
            // If not found, return the state unchanged.
            if (!state.Probes.Exists(probe => probe.Name == action.payload))
                return state;

            // Update the active probe UUID.
            return state with
            {
                ActiveProbeName = action.payload,
            };
        }

        #endregion

        #region Probe Reducers

        public static SceneState SetProbePositionReducer(
            SceneState state,
            IAction<(string Name, Vector3 APMLDV, float Depth, Vector3 ForwardT)> action
        )
        {
            // Find the index of the target probe.
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                APMLDV = action.payload.APMLDV + action.payload.ForwardT * action.payload.Depth,
            };

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetProbeAnglesReducer(
            SceneState state,
            IAction<(string Name, Vector3 Angles, Vector2 PitchRange)> action
        )
        {
            // Find the index of the target probe.
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            var pitchClampedAngles = action.payload.Angles;
            pitchClampedAngles.y = Mathf.Clamp(
                action.payload.Angles.y,
                action.payload.PitchRange.x,
                action.payload.PitchRange.y
            );

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                Angles = pitchClampedAngles,
            };

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetProbePositionAndAnglesReducer(
            SceneState state,
            IAction<(
                string Name,
                Vector3 APMLDV,
                float Depth,
                Vector3 ForwardT,
                Vector3 Angles,
                Vector2 PitchRange
            )> action
        )
        {
            // Find the index of the target probe.
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            var pitchClampedAngles = action.payload.Angles;
            pitchClampedAngles.y = Mathf.Clamp(
                action.payload.Angles.y,
                action.payload.PitchRange.x,
                action.payload.PitchRange.y
            );

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                APMLDV = action.payload.APMLDV + action.payload.ForwardT * action.payload.Depth,
                Angles = pitchClampedAngles,
            };

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState ChangeProbePositionByReducer(
            SceneState state,
            IAction<(string Name, Vector3 APMLDV, float Depth, Vector3 ForwardT)> action
        )
        {
            // Find the index of the target probe.
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                APMLDV =
                    probesCopy[index].APMLDV
                    + action.payload.APMLDV
                    + action.payload.ForwardT * action.payload.Depth,
            };

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState ChangeProbeAnglesByReducer(
            SceneState state,
            IAction<(string Name, Vector3 Angles, Vector2 PitchRange)> action
        )
        {
            // Find the index of the target probe.
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            var pitchClampedAngles = action.payload.Angles;
            pitchClampedAngles.y = Mathf.Clamp(
                action.payload.Angles.y,
                action.payload.PitchRange.x,
                action.payload.PitchRange.y
            );

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                Angles = probesCopy[index].Angles + pitchClampedAngles,
            };

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetProbeColorReducer(
            SceneState state,
            IAction<(string Name, ProbeColor Color)> action
        )
        {
            // Find the index of the target probe.
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                Color = action.payload.Color,
            };

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetProbeLockedReducer(
            SceneState state,
            IAction<(string Name, bool Locked)> action
        )
        {
            // Find the index of the target probe.
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                Locked = action.payload.Locked,
            };

            return state with
            {
                Probes = probesCopy,
            };
        }

        #endregion

        #region Automation Reducers

        /// <summary>
        ///     Set the selected target insertion probe for the active probe.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="action">Chosen probe name in the payload.</param>
        /// <returns>State with chosen target updated on the active probe.</returns>
        public static SceneState SetSelectedTargetInsertionProbeNameReducer(
            SceneState state,
            IAction<string> action
        )
        {
            try
            {
                // Verify selected target exists and is targetable.
                var selectedTarget = state.Probes.First(probeState =>
                    probeState.Name == action.payload
                );
                if (selectedTarget.IsEphysLinkControlled)
                    throw new ArgumentException("Selected target is not targetable.");

                // Update the selected target insertion probe for the active probe.
                var probesCopy = state.Probes.ToList();
                probesCopy[state.ActiveProbeIndex].SelectedTargetInsertionProbeName =
                    action.payload;

                return state with
                {
                    Probes = probesCopy,
                };
            }
            catch (Exception)
            {
                // Return the state unchanged if the target is not found or not targetable.
                return state;
            }
        }

        public static SceneState SetActiveProbeAutomationProgressStateReducer(
            SceneState state,
            IAction<AutomationProgressState> action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Set the active probe's automation progress state.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].AutomationProgressState = action.payload;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetActiveProbeAutomationProgressStateToNextDrivingReducer(
            SceneState state,
            IAction action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Move to next driving state if possible. If not, return the state unchanged.
            var newProgressState = state.ActiveProbeState.AutomationProgressState switch
            {
                AutomationProgressState.IsCalibrated =>
                    AutomationProgressState.DrivingToTargetEntryCoordinate,
                AutomationProgressState.AtDuraInsert => AutomationProgressState.DrivingToNearTarget,
                AutomationProgressState.AtNearTargetInsert =>
                    AutomationProgressState.DrivingToPastTarget,
                AutomationProgressState.AtPastTarget => AutomationProgressState.ReturningToTarget,
                AutomationProgressState.AtTarget => AutomationProgressState.DrivingToNearTarget,
                _ => state.ActiveProbeState.AutomationProgressState,
            };

            // Set the active probe's automation progress state to driving to target entry coordinate.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].AutomationProgressState = newProgressState;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetActiveProbeAutomationProgressStateToNextExitingReducer(
            SceneState state,
            IAction action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Move to next exiting state if possible. If not, return the state unchanged.
            var newProgressState = state.ActiveProbeState.AutomationProgressState switch
            {
                AutomationProgressState.AtDuraInsert
                or AutomationProgressState.AtNearTargetInsert
                or AutomationProgressState.AtPastTarget
                or AutomationProgressState.AtTarget => AutomationProgressState.ExitingToDura,
                AutomationProgressState.AtDuraExit => AutomationProgressState.ExitingToMargin,
                AutomationProgressState.AtExitMargin =>
                    AutomationProgressState.ExitingToTargetEntryCoordinate,
                _ => state.ActiveProbeState.AutomationProgressState,
            };

            // Set the active probe's automation progress state to next exiting state.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].AutomationProgressState = newProgressState;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState CompleteActiveProbeAutomationIntermediateProgressReducer(
            SceneState state,
            IAction action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Move to next landmark progress state if possible. If not, return the state unchanged.
            var newProgressState = state.ActiveProbeState.AutomationProgressState switch
            {
                AutomationProgressState.DrivingToTargetEntryCoordinate =>
                    AutomationProgressState.AtTargetEntryCoordinate,
                AutomationProgressState.DrivingToNearTarget =>
                    AutomationProgressState.AtNearTargetInsert,
                AutomationProgressState.DrivingToPastTarget => AutomationProgressState.AtPastTarget,
                AutomationProgressState.ReturningToTarget => AutomationProgressState.AtTarget,
                AutomationProgressState.ExitingToDura => AutomationProgressState.AtDuraExit,
                AutomationProgressState.ExitingToMargin => AutomationProgressState.AtExitMargin,
                AutomationProgressState.ExitingToTargetEntryCoordinate =>
                    AutomationProgressState.AtTargetEntryCoordinate,
                _ => state.ActiveProbeState.AutomationProgressState,
            };

            // Complete the intermediate progress for the active probe.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].AutomationProgressState = newProgressState;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState CancelActiveProbeAutomationIntermediateProgressReducer(
            SceneState state,
            IAction action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Revert to the previous landmark progress state if possible. If not, return the state unchanged.
            var newProgressState = state.ActiveProbeState.AutomationProgressState switch
            {
                AutomationProgressState.DrivingToTargetEntryCoordinate =>
                    AutomationProgressState.IsCalibrated,
                AutomationProgressState.DrivingToNearTarget => AutomationProgressState.AtDuraInsert,
                AutomationProgressState.DrivingToPastTarget =>
                    AutomationProgressState.AtNearTargetInsert,
                AutomationProgressState.ReturningToTarget => AutomationProgressState.AtPastTarget,
                AutomationProgressState.ExitingToDura => AutomationProgressState.AtTarget,
                AutomationProgressState.ExitingToMargin => AutomationProgressState.AtDuraExit,
                AutomationProgressState.ExitingToTargetEntryCoordinate =>
                    AutomationProgressState.AtExitMargin,
                _ => state.ActiveProbeState.AutomationProgressState,
            };

            // Cancel the intermediate progress for the active probe.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].AutomationProgressState = newProgressState;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetActiveProbeReferenceCoordinateReducer(
            SceneState state,
            IAction<Vector4> action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Set the active probe's reference coordinate.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].ReferenceCoordinateOffset = action.payload;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetActiveProbeDuraOffsetReducer(
            SceneState state,
            IAction<float> action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Set the active probe's Dura offset.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].DuraDepth = action.payload;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetActiveProbeTargetInsertionBaseSpeedReducer(
            SceneState state,
            IAction<int> action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Set the active probe's target insertion speed.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].InsertionBaseSpeed = action.payload;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetActiveProbeDrivePastDistanceReducer(
            SceneState state,
            IAction<int> action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Set the active probe's drive past distance.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].DrivePastDistance = action.payload;

            return state with
            {
                Probes = probesCopy,
            };
        }

        #endregion

        #region Brain Area

        public static SceneState RotateAreaVisibilityReducer(SceneState state, IAction<int> action)
        {
            var newBrainAreaVisibility = new Dictionary<int, AreaDisplayType>(
                state.BrainAreaVisibility
            );

            // Get the area ID from the action payload
            var areaID = action.payload;

            // If the area doesn't exist in the dictionary, add it with default value (Opaque)
            if (!newBrainAreaVisibility.TryAdd(areaID, AreaDisplayType.Opaque))
            {
                // Rotate the visibility state for this specific area
                var currentValue = (int)newBrainAreaVisibility[areaID];
                var rotatedValue =
                    (currentValue + 1) % Enum.GetValues(typeof(AreaDisplayType)).Length;
                newBrainAreaVisibility[areaID] = (AreaDisplayType)rotatedValue;
            }

            Debug.Log($"Updated area visibility for {areaID} to {newBrainAreaVisibility[areaID]}");

            return state with
            {
                BrainAreaVisibility = newBrainAreaVisibility,
            };
        }

        #endregion
    }

    public static class SceneActions
    {
        #region Probe List Actions

        public static readonly ActionCreator<ProbeType> ADD_PROBE =
            $"{SliceNames.SCENE_SLICE}/AddProbe";

        public static readonly ActionCreator<string> DUPLICATE_PROBE =
            $"{SliceNames.SCENE_SLICE}/DuplicateProbe";

        public static readonly ActionCreator<string> REMOVE_PROBE =
            $"{SliceNames.SCENE_SLICE}/RemoveProbe";

        public static readonly ActionCreator REMOVE_ALL_PROBES =
            $"{SliceNames.SCENE_SLICE}/RemoveAllProbes";

        #endregion

        #region Active Probe Actions

        public static readonly ActionCreator<string> SET_ACTIVE_PROBE =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbe";

        #endregion

        #region Probe Actions

        public static readonly ActionCreator<(
            string Name,
            Vector3 APMLDV,
            float Depth,
            Vector3 ForwardT
        )> SET_PROBE_POSITION = $"{SliceNames.SCENE_SLICE}/SetProbePosition";

        public static readonly ActionCreator<(
            string Name,
            Vector3 Angles,
            Vector2 PitchRange
        )> SET_PROBE_ANGLES = $"{SliceNames.SCENE_SLICE}/SetProbeAngles";

        public static readonly ActionCreator<(
            string Name,
            Vector3 APMLDV,
            float Depth,
            Vector3 ForwardT,
            Vector3 Angles,
            Vector2 PitchRange
        )> SET_PROBE_POSITION_AND_ANGLES = $"{SliceNames.SCENE_SLICE}/SetProbePositionAndAngles";

        public static readonly ActionCreator<(
            string Name,
            Vector3 APMLDV,
            float Depth,
            Vector3 ForwardT
        )> CHANGE_PROBE_POSITION_BY = $"{SliceNames.SCENE_SLICE}/ChangeProbePositionBy";

        public static readonly ActionCreator<(
            string Name,
            Vector3 Angles,
            Vector2 PitchRange
        )> CHANGE_PROBE_ANGLES_BY = $"{SliceNames.SCENE_SLICE}/ChangeProbeAnglesBy";

        public static readonly ActionCreator<(string, ProbeColor)> SET_PROBE_COLOR =
            $"{SliceNames.SCENE_SLICE}/SetProbeColor";

        public static readonly ActionCreator<(string, bool)> SET_PROBE_LOCKED =
            $"{SliceNames.SCENE_SLICE}/SetProbeLocked";

        #endregion

        #region Automation Actions

        public static readonly ActionCreator<string> SET_SELECTED_TARGET_INSERTION_PROBE_NAME =
            $"{SliceNames.SCENE_SLICE}/SetSelectedTargetInsertionProbeName";

        public static readonly ActionCreator<AutomationProgressState> SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeAutomationProgressState";

        public static readonly ActionCreator SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE_TO_NEXT_DRIVING =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeAutomationProgressStateToNextDriving";

        public static readonly ActionCreator SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE_TO_NEXT_EXITING =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeAutomationProgressStateToNextExiting";

        public static readonly ActionCreator COMPLETE_ACTIVE_PROBE_AUTOMATION_INTERMEDIATE_PROGRESS =
            $"{SliceNames.SCENE_SLICE}/CompleteActiveProbeAutomationIntermediateProgress";

        public static readonly ActionCreator CANCEL_ACTIVE_PROBE_AUTOMATION_INTERMEDIATE_PROGRESS =
            $"{SliceNames.SCENE_SLICE}/CancelActiveProbeAutomationIntermediateProgress";

        public static readonly ActionCreator<Vector4> SET_ACTIVE_PROBE_REFERENCE_COORDINATE =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeReferenceCoordinate";

        public static readonly ActionCreator<float> SET_ACTIVE_PROBE_DURA_OFFSET =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeDuraOffset";

        public static readonly ActionCreator<int> SET_ACTIVE_PROBE_INSERTION_BASE_SPEED =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeInsertionBaseSpeed";

        public static readonly ActionCreator<int> SET_ACTIVE_PROBE_DRIVE_PAST_DISTANCE =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeDrivePastDistance";

        #endregion

        #region Brain Atlas

        public static readonly ActionCreator<int> ROTATE_AREA_VISIBILITY =
            $"{SliceNames.SCENE_SLICE}/RotateAreaVisibility";

        #endregion
    }
}
