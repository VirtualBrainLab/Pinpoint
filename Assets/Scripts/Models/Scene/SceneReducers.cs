using System.Linq;
using Unity.AppUI.Redux;

namespace Models.Scene
{
    public static class SceneReducers
    {
        public static SceneState AddProbeReducer(SceneState state, IAction action)
        {
            // TODO
            return state;
        }

        public static SceneState RemoveProbeReducer(SceneState state, IAction<int> action)
        {
            var newProbesList = state.Probes;
            newProbesList.RemoveAt(action.payload);
            return state with { Probes = newProbesList };
        }

        public static SceneState SetActiveProbeIndexReducer(SceneState state, IAction<int> action)
        {
            return state with { ActiveProbeIndex = action.payload };
        }

        /// <summary>
        /// Update the selected target insertion probe state object.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="action">Chosen probe state object in the payload.</param>
        /// <returns>State with chosen target updated.</returns>
        public static SceneState SetSelectedTargetInsertionReducer(
            SceneState state,
            IAction<ProbeManager> action
        )
        {
            var probesCopy = state.Probes.ToList();
            var oldProbeState = probesCopy[state.ActiveProbeIndex];
            var newProbeState = oldProbeState with
            {
                SelectedTargetInsertionProbeState = action.payload,
            };
            probesCopy[state.ActiveProbeIndex] = newProbeState;

            return state with
            {
                Probes = probesCopy,
            };
        }
    }
    
    public static class SceneActions
    {
        public static readonly ActionCreator ADD_PROBE =
            $"{SliceNames.SCENE_SLICE}/AddProbe";
        public static readonly ActionCreator<int> REMOVE_PROBE =
            $"{SliceNames.SCENE_SLICE}/RemoveProbe";
        public static readonly ActionCreator<int> SET_ACTIVE_PROBE_INDEX =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeIndex";
        public static readonly ActionCreator<ProbeManager> SET_SELECTED_TARGET_INSERTION_PROBE_STATE =
            $"{SliceNames.SCENE_SLICE}/SetSelectedTargetInsertionProbeState";
    }
}
