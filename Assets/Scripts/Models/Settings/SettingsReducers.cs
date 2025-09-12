using Unity.AppUI.Redux;
using Utils.Types;

namespace Models.Settings
{
    public static class SettingsReducers
    {
        public static SettingsState SetTabIndexReducer(SettingsState state, IAction<int> action)
        {
            // Ignore if the payload is negative (invalid tab index).
            if (action.payload < 0)
                return state;
            return state with { TabIndex = action.payload };
        }

        #region Ephys Link

        public static SettingsState SetSelectedEphysLinkPlatformTypeReducer(
            SettingsState state,
            IAction<EphysLinkPlatformType> action
        )
        {
            return state with { SelectedEphysLinkPlatformType = action.payload };
        }

        public static SettingsState SetNewScalePathfinderMpmPortReducer(
            SettingsState state,
            IAction<int> action
        )
        {
            return state with { NewScalePathfinderMpmPort = action.payload };
        }

        public static SettingsState SetCustomServerIpAddressReducer(
            SettingsState state,
            IAction<string> action
        )
        {
            return state with { CustomServerIpAddress = action.payload };
        }

        public static SettingsState SetCustomServerPortReducer(
            SettingsState state,
            IAction<int> action
        )
        {
            return state with { CustomServerPort = action.payload };
        }

        public static SettingsState SetConnectionStateReducer(
            SettingsState state,
            IAction<(EphysLinkConnectionState ConnectionState, string ConnectionSocketId)> action
        )
        {
            return state with
            {
                ConnectionState = action.payload.ConnectionState,
                ConnectionSocketId = action.payload.ConnectionSocketId,
            };
        }

        #endregion
    }

    public static class SettingsActions
    {
        public static readonly ActionCreator<int> SET_TAB_INDEX =
            $"{SliceNames.SETTINGS_SLICE}/SetTabIndex";

        #region Ephys Link

        public static readonly ActionCreator<EphysLinkPlatformType> SET_SELECTED_EPHYS_LINK_PLATFORM_TYPE =
            $"{SliceNames.SETTINGS_SLICE}/SetSelectedEphysLinkPlatformType";

        public static readonly ActionCreator<int> SET_NEW_SCALE_PATHFINDER_MPM_PORT =
            $"{SliceNames.SETTINGS_SLICE}/SetNewScalePathfinderMpmPort";

        public static readonly ActionCreator<string> SET_CUSTOM_SERVER_IP_ADDRESS =
            $"{SliceNames.SETTINGS_SLICE}/SetCustomServerIpAddress";

        public static readonly ActionCreator<int> SET_CUSTOM_SERVER_PORT =
            $"{SliceNames.SETTINGS_SLICE}/SetCustomServerPort";

        public static readonly ActionCreator<(
            EphysLinkConnectionState ConnectionState,
            string ConnectionSocketId
        )> SET_CONNECTION_STATE = $"{SliceNames.SETTINGS_SLICE}/SetConnectionState";

        #endregion
    }
}
