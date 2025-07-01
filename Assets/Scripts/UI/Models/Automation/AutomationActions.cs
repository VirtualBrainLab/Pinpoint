using UI.Utils;
using Unity.AppUI.Redux;

namespace UI.Models.Automation
{
    public static class AutomationActions
    {
        public static readonly ActionCreator ADD_PROBE =
            $"{SliceNames.AUTOMATION_SLICE}/AddProbe";
        public static readonly ActionCreator<int> REMOVE_PROBE =
            $"{SliceNames.AUTOMATION_SLICE}/RemoveProbe";
        public static readonly ActionCreator<int> SET_ACTIVE_PROBE_INDEX =
            $"{SliceNames.AUTOMATION_SLICE}/SetActiveProbeIndex";
    }
}
