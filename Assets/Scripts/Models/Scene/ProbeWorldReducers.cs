using System.Linq;
using Unity.AppUI.Redux;
using UnityEngine;

namespace Models.Scene
{
    public static class ProbeWorldReducers
    {
        /// <summary>
        /// Updates or adds a probe's world state
        /// </summary>
        public static ProbeWorldStateSlice UpdateProbeWorldStateReducer(
            ProbeWorldStateSlice state,
            IAction<ProbeWorldState> action
        )
        {
            var probeWorldStatesCopy = state.ProbeWorldStates.ToList();

            // Find if this probe already exists in the list
            var index = probeWorldStatesCopy.FindIndex(p => p.Name == action.payload.Name);

            if (index >= 0)
            {
                // Update existing probe world state
                probeWorldStatesCopy[index] = action.payload;
            }
            else
            {
                // Add new probe world state
                probeWorldStatesCopy.Add(action.payload);
            }

            return state with { ProbeWorldStates = probeWorldStatesCopy };
        }

        /// <summary>
        /// Removes a probe's world state when the probe is removed
        /// </summary>
        public static ProbeWorldStateSlice RemoveProbeWorldStateReducer(
            ProbeWorldStateSlice state,
            IAction<string> action
        )
        {
            var probeWorldStatesCopy = state.ProbeWorldStates.ToList();

            // Remove the probe world state with the specified name
            probeWorldStatesCopy.RemoveAll(p => p.Name == action.payload);

            return state with { ProbeWorldStates = probeWorldStatesCopy };
        }

        /// <summary>
        /// Clears all probe world states
        /// </summary>
        public static ProbeWorldStateSlice ClearAllProbeWorldStatesReducer(
            ProbeWorldStateSlice state,
            IAction action
        )
        {
            return state with { ProbeWorldStates = new() };
        }
    }

    public static class ProbeWorldActions
    {
        /// <summary>
        /// Action to update a probe's world state with computed values
        /// </summary>
        public static readonly ActionCreator<ProbeWorldState> UPDATE_PROBE_WORLD_STATE =
            $"{SliceNames.PROBE_WORLD_SLICE}/UpdateProbeWorldState";

        /// <summary>
        /// Action to remove a probe's world state
        /// </summary>
        public static readonly ActionCreator<string> REMOVE_PROBE_WORLD_STATE =
            $"{SliceNames.PROBE_WORLD_SLICE}/RemoveProbeWorldState";

        /// <summary>
        /// Action to clear all probe world states
        /// </summary>
        public static readonly ActionCreator CLEAR_ALL_PROBE_WORLD_STATES =
            $"{SliceNames.PROBE_WORLD_SLICE}/ClearAllProbeWorldStates";
    }
}
