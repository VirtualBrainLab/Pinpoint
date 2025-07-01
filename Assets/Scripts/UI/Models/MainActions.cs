using UI.Utils;
using Unity.AppUI.Redux;

namespace UI.Models
{
    public static class MainActions
    {
        internal static readonly ActionCreator<MainMode> SET_MODE =
            $"{SliceNames.MAIN_SLICE}/SetMode";
        internal static readonly ActionCreator TOGGLE_LEFT_SIDE_PANEL =
            $"{SliceNames.MAIN_SLICE}/ToggleLeftSidePanel";
        internal static readonly ActionCreator TOGGLE_RIGHT_SIDE_PANEL =
            $"{SliceNames.MAIN_SLICE}/ToggleRightSidePanel";
    }
}
