using UI.Utils;
using Unity.AppUI.Redux;

namespace UI.Models.Scene
{
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
