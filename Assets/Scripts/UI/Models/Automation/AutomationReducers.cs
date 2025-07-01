using Unity.AppUI.Redux;

namespace UI.Models.Automation
{
    public static class AutomationReducers
    {
        public static AutomationState AddProbe(AutomationState state, IAction action)
        {
            // TODO
            return state;
        }

        public static AutomationState RemoveProbe(AutomationState state, IAction<int> action)
        {
            var newProbesList = state.Probes;
            newProbesList.RemoveAt(action.payload);
            return state with { Probes = newProbesList };
        }

        public static AutomationState SetActiveProbeIndex(
            AutomationState state,
            IAction<int> action
        )
        {
            return state with { ActiveProbeIndex = action.payload };
        }
    }
}
