using System.ComponentModel;
using Unity.AppUI.Redux;

namespace Models.Automation
{
    public static class EphysLinkReducers
    {
        public static EphysLinkState SetSelectedPlatformTypeReducer(
            EphysLinkState state,
            IAction<PlatformType> action
        )
        {
            return state with { SelectedPlatformType = action.payload };
        }

        public static EphysLinkState SetNewScalePathfinderMpmPortReducer(
            EphysLinkState state,
            IAction<int> action
        )
        {
            return state with { NewScalePathfinderMpmPort = action.payload };
        }

        public static EphysLinkState SetCustomServerIpAddressReducer(
            EphysLinkState state,
            IAction<string> action
        )
        {
            return state with { CustomServerIpAddress = action.payload };
        }

        public static EphysLinkState SetCustomServerPortReducer(
            EphysLinkState state,
            IAction<int> action
        )
        {
            return state with { CustomServerPort = action.payload };
        }

        public static EphysLinkState SetIsConnectedReducer(
            EphysLinkState state,
            IAction<bool> action
        )
        {
            return state with { IsConnected = action.payload };
        }
    }

    public static class EphysLinkActions
    {
        public static readonly ActionCreator<PlatformType> SET_SELECTED_PLATFORM_TYPE =
            $"{SliceNames.EPHYS_LINK_SLICE}/SetSelectedPlatformType";

        public static readonly ActionCreator<int> SET_NEW_SCALE_PATHFINDER_MPM_PORT =
            $"{SliceNames.EPHYS_LINK_SLICE}/SetNewScalePathfinderMpmPort";

        public static readonly ActionCreator<string> SET_CUSTOM_SERVER_IP_ADDRESS =
            $"{SliceNames.EPHYS_LINK_SLICE}/SetCustomServerIpAddress";
        public static readonly ActionCreator<int> SET_CUSTOM_SERVER_PORT =
            $"{SliceNames.EPHYS_LINK_SLICE}/SetCustomServerPort";
        public static readonly ActionCreator<bool> SET_IS_CONNECTED =
            $"{SliceNames.EPHYS_LINK_SLICE}/SetIsConnected";
    }
}
