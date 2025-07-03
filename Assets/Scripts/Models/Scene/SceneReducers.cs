using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.Redux;

namespace Models.Scene
{
    public static class SceneReducers
    {
        public static SceneState AddProbeReducer(SceneState state, IAction action)
        {
            var newProbesList = state.Probes.ToList();
            newProbesList.Add(new ProbeState());
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
                var selectedTarget = state.Probes.First(probeState => probeState.UUID == action.payload);
                if (selectedTarget.IsEphysLinkControlled)
                {
                    throw new ArgumentException("Selected target is not targetable.");
                }
                
                // Update the selected target insertion probe for the active probe.
                var probesCopy = state.Probes.ToList();
                probesCopy
                    .First(probeState => probeState.UUID == state.ActiveProbeUUID)
                    .SelectedTargetInsertionProbeUUID = action.payload;

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
    }

    public static class SceneActions
    {
        public static readonly ActionCreator ADD_PROBE = $"{SliceNames.SCENE_SLICE}/AddProbe";
        public static readonly ActionCreator<string> REMOVE_PROBE =
            $"{SliceNames.SCENE_SLICE}/RemoveProbe";
        public static readonly ActionCreator REMOVE_ALL_PROBES =
            $"{SliceNames.SCENE_SLICE}/RemoveAllProbes";

        public static readonly ActionCreator<string> SET_ACTIVE_PROBE_UUID =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeUUID";
        public static readonly ActionCreator<string> SET_SELECTED_TARGET_INSERTION_PROBE_UUID =
            $"{SliceNames.SCENE_SLICE}/SetSelectedTargetInsertionProbeUUID";
    }
}
