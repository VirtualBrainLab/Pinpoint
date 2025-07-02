using UI.Utils;
using Unity.AppUI.Redux;

namespace UI.Models
{
    public static class MainActions
    {
        public static readonly ActionCreator<MainMode> SET_MODE =
            $"{SliceNames.MAIN_SLICE}/SetMode";
        public static readonly ActionCreator TOGGLE_LEFT_SIDE_PANEL =
            $"{SliceNames.MAIN_SLICE}/ToggleLeftSidePanel";
        public static readonly ActionCreator TOGGLE_RIGHT_SIDE_PANEL =
            $"{SliceNames.MAIN_SLICE}/ToggleRightSidePanel";
    }
}
