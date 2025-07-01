using UI.Utils;
using Unity.AppUI.Redux;

namespace UI.Models.Automation
{
    public static class AutomationActions
    {
        internal static readonly ActionCreator ADD_PROBE =
            $"{SliceNames.AUTOMATION_SLICE}/AddProbe";
        internal static readonly ActionCreator<int> REMOVE_PROBE =
            $"{SliceNames.AUTOMATION_SLICE}/RemoveProbe";
        internal static readonly ActionCreator<int> SET_ACTIVE_PROBE_INDEX =
            $"{SliceNames.AUTOMATION_SLICE}/SetActiveProbeIndex";
    }
}
