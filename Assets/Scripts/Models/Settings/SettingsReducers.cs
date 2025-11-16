using Unity.AppUI.Redux;
using UnityEngine;
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

        public static SettingsState SetInPlaneZoomReducer(SettingsState state, IAction<int> action)
        {
    return state with { inPlaneZoom = action.payload };
   }

        #region Probe Settings

        public static SettingsState SetDetectCollisionsReducer(
     SettingsState state,
         IAction<bool> action
        )
        {
  return state with { DetectCollisions = action.payload };
    }

      public static SettingsState SetConvertAPML2ProbeReducer(
            SettingsState state,
         IAction<bool> action
   )
   {
       return state with { ConvertAPML2Probe = action.payload };
        }

    #endregion

     #region Graphics Settings

        public static SettingsState SetBackgroundReducer(
     SettingsState state,
        IAction<Color> action
      )
        {
    return state with { Background = action.payload };
        }

        public static SettingsState SetShowSurfaceCoordinateReducer(
   SettingsState state,
         IAction<bool> action
        )
        {
        return state with { ShowSurfaceCoordinate = action.payload };
      }

        public static SettingsState SetShowBregmaAxisReducer(
            SettingsState state,
 IAction<bool> action
        )
        {
       return state with { ShowBregmaAxis = action.payload };
        }

 public static SettingsState SetGhostInactiveProbesReducer(
     SettingsState state,
      IAction<bool> action
        )
        {
         return state with { GhostInactiveProbes = action.payload };
        }

        #endregion

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

        public static SettingsState SetEphysLinkConnectionStateReducer(
    SettingsState state,
            IAction<(EphysLinkConnectionState ConnectionState, string ConnectionSocketId)> action
        )
   {
            return state with { EphysLinkConnectionState = action.payload.ConnectionState };
        }

        #endregion
    }

    public static class SettingsActions
    {
        public static readonly ActionCreator<int> SET_TAB_INDEX =
            $"{SliceNames.SETTINGS_SLICE}/SetTabIndex";

        public static readonly ActionCreator<int> SET_IN_PLANE_ZOOM =
      $"{SliceNames.SETTINGS_SLICE}/SetInPlaneZoom";

     #region Probe Settings

  public static readonly ActionCreator<bool> SET_DETECT_COLLISIONS =
            $"{SliceNames.SETTINGS_SLICE}/SetDetectCollisions";

        public static readonly ActionCreator<bool> SET_CONVERT_APML2PROBE =
 $"{SliceNames.SETTINGS_SLICE}/SetConvertAPML2Probe";

  #endregion

        #region Graphics Settings

   public static readonly ActionCreator<Color> SET_BACKGROUND =
 $"{SliceNames.SETTINGS_SLICE}/SetBackground";

     public static readonly ActionCreator<bool> SET_SHOW_SURFACE_COORDINATE =
       $"{SliceNames.SETTINGS_SLICE}/SetShowSurfaceCoordinate";

   public static readonly ActionCreator<bool> SET_SHOW_BREGMA_AXIS =
 $"{SliceNames.SETTINGS_SLICE}/SetShowBregmaAxis";

        public static readonly ActionCreator<bool> SET_GHOST_INACTIVE_PROBES =
            $"{SliceNames.SETTINGS_SLICE}/SetGhostInactiveProbes";

        #endregion

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
        )> SET_EPHYS_LINK_CONNECTION_STATE = $"{SliceNames.SETTINGS_SLICE}/SetEphysLinkConnectionState";

 #endregion
    }
}
