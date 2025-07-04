using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.Redux;
using UnityEngine;

namespace Models.Scene
{
    public static class SceneReducers
    {
        #region Probe List Reducers

        public static SceneState AddProbeReducer(SceneState state, IAction<string> action)
        {
            var newProbesList = state.Probes.ToList();
            newProbesList.Add(new ProbeState { UUID = action.payload });
            return state with { Probes = newProbesList };
        }

        public static SceneState RemoveProbeReducer(SceneState state, IAction<string> action)
        {
            // Remove all probes with the specified UUID.
            var newProbesList = state.Probes.ToList();

            // If no probes were removed, return the state unchanged.
            if (newProbesList.RemoveAll(probeState => probeState.UUID == action.payload) == 0)
            {
                return state;
            }

            // Update the state with the new probes list, and update the active probe UUID if it was removed.
            return state with
            {
                Probes = newProbesList,
                ActiveProbeUUID =
                    state.ActiveProbeUUID == action.payload ? string.Empty : state.ActiveProbeUUID,
            };
        }

        public static SceneState RemoveAllProbesReducer(SceneState state, IAction action)
        {
            return state with { Probes = new List<ProbeState>(), ActiveProbeUUID = string.Empty };
        }

        #endregion

        #region Active Probe Reducers

        public static SceneState SetActiveProbeUUIDReducer(SceneState state, IAction<string> action)
        {
            // If not found, return the state unchanged.
            if (!state.Probes.Exists(probe => probe.UUID == action.payload))
            {
                return state;
            }

            // Update the active probe UUID.
            return state with
            {
                ActiveProbeUUID = action.payload,
            };
        }

        #endregion

        #region Automation Reducers

        /// <summary>
        /// Set the selected target insertion probe for the active probe.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="action">Chosen probe UUID in the payload.</param>
        /// <returns>State with chosen target updated on the active probe.</returns>
        public static SceneState SetSelectedTargetInsertionProbeUUIDReducer(
            SceneState state,
            IAction<string> action
        )
        {
            try
            {
                // Verify selected target exists and is targetable.
                var selectedTarget = state.Probes.First(probeState =>
                    probeState.UUID == action.payload
                );
                if (selectedTarget.IsEphysLinkControlled)
                {
                    throw new ArgumentException("Selected target is not targetable.");
                }

                // Update the selected target insertion probe for the active probe.
                var probesCopy = state.Probes.ToList();
                probesCopy[state.ActiveProbeIndex].SelectedTargetInsertionProbeUUID =
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
            {
                return state;
            }

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
            {
                return state;
            }

            // Move to next driving state if possible. If not, return the state unchanged.
            var newProgressState = state.ActiveProbeState.AutomationProgressState switch
            {
                AutomationProgressState.IsCalibrated =>
                    AutomationProgressState.DrivingToTargetEntryCoordinate,
                AutomationProgressState.AtDuraInsert => AutomationProgressState.DrivingToNearTarget,
                AutomationProgressState.AtNearTargetInsert =>
                    AutomationProgressState.DrivingToPastTarget,
                AutomationProgressState.AtPastTarget => AutomationProgressState.ReturningToTarget,
                AutomationProgressState.ExitingToDura =>
                    AutomationProgressState.DrivingToNearTarget,
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
            {
                return state;
            }

            // Move to next exiting state if possible. If not, return the state unchanged.
            var newProgressState = state.ActiveProbeState.AutomationProgressState switch
            {
                AutomationProgressState.DrivingToNearTarget
                or AutomationProgressState.AtNearTargetInsert
                or AutomationProgressState.DrivingToPastTarget
                or AutomationProgressState.ReturningToTarget
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
            {
                return state;
            }

            // move to next complete progress state if possible. If not, return the state unchanged.
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

        public static SceneState SetActiveProbeReferenceCoordinateReducer(
            SceneState state,
            IAction<Vector4> action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
            {
                return state;
            }

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
            {
                return state;
            }

            // Set the active probe's Dura offset.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].DuraDepth = action.payload;

            return state with
            {
                Probes = probesCopy,
            };
        }

        #endregion
    }

    public static class SceneActions
    {
        #region Probe List Actions

        public static readonly ActionCreator<string> ADD_PROBE =
            $"{SliceNames.SCENE_SLICE}/AddProbe";
        public static readonly ActionCreator<string> REMOVE_PROBE =
            $"{SliceNames.SCENE_SLICE}/RemoveProbe";
        public static readonly ActionCreator REMOVE_ALL_PROBES =
            $"{SliceNames.SCENE_SLICE}/RemoveAllProbes";

        #endregion

        #region Active Probe Actions

        public static readonly ActionCreator<string> SET_ACTIVE_PROBE_UUID =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeUUID";

        #endregion

        #region Automation Actions

        public static readonly ActionCreator<string> SET_SELECTED_TARGET_INSERTION_PROBE_UUID =
            $"{SliceNames.SCENE_SLICE}/SetSelectedTargetInsertionProbeUUID";

        public static readonly ActionCreator<AutomationProgressState> SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeAutomationProgressState";
        public static readonly ActionCreator SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE_TO_NEXT_DRIVING =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeAutomationProgressStateToNextDriving";
        public static readonly ActionCreator SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE_TO_NEXT_EXITING =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeAutomationProgressStateToNextExiting";
        public static readonly ActionCreator COMPLETE_ACTIVE_PROBE_AUTOMATION_INTERMEDIATE_PROGRESS =
            $"{SliceNames.SCENE_SLICE}/CompleteActiveProbeAutomationIntermediateProgress";

        public static readonly ActionCreator<Vector4> SET_ACTIVE_PROBE_REFERENCE_COORDINATE =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeReferenceCoordinate";

        public static readonly ActionCreator<float> SET_ACTIVE_PROBE_DURA_OFFSET =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeDuraOffset";

        #endregion
    }
}
