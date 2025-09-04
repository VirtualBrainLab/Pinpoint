using System.ComponentModel;
using Unity.AppUI.Redux;
using Utils.Types;

namespace Models.Automation
{
    public static class EphysLinkReducers
    {
        public static EphysLinkState SetSelectedEphysLinkPlatformTypeReducer(
            EphysLinkState state,
            IAction<EphysLinkPlatformType> action
        )
        {
            return state with { SelectedEphysLinkPlatformType = action.payload };
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

        public static EphysLinkState SetConnectionStateReducer(
            EphysLinkState state,
            IAction<EphysLinkConnectionState> action
        )
        {
            return state with { ConnectionState = action.payload };
        }
    }

    public static class EphysLinkActions
    {
        public static readonly ActionCreator<EphysLinkPlatformType> SET_SELECTED_EPHYS_LINK_PLATFORM_TYPE =
            $"{SliceNames.EPHYS_LINK_SLICE}/SetSelectedEphysLinkPlatformType";

        public static readonly ActionCreator<int> SET_NEW_SCALE_PATHFINDER_MPM_PORT =
            $"{SliceNames.EPHYS_LINK_SLICE}/SetNewScalePathfinderMpmPort";

        public static readonly ActionCreator<string> SET_CUSTOM_SERVER_IP_ADDRESS =
            $"{SliceNames.EPHYS_LINK_SLICE}/SetCustomServerIpAddress";
        public static readonly ActionCreator<int> SET_CUSTOM_SERVER_PORT =
            $"{SliceNames.EPHYS_LINK_SLICE}/SetCustomServerPort";
        public static readonly ActionCreator<EphysLinkConnectionState> SET_CONNECTION_STATE =
            $"{SliceNames.EPHYS_LINK_SLICE}/SetConnectionState";
    }
}
