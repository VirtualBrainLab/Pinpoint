using Unity.AppUI.Redux;

namespace Models.Settings
{
    public static class SettingsReducers
    {
        public static SettingsState SetTabIndexReducer(SettingsState state, IAction<int> action)
        {
            return state with { TabIndex = action.payload };
        }
    }

    public static class SettingsActions
    {
        public static readonly ActionCreator<int> SET_TAB_INDEX =
            $"{SliceNames.SETTINGS_SLICE}/SetTabIndex";
    }
}
