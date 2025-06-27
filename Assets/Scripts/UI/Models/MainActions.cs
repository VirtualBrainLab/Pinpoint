using UI.Utils;
using Unity.AppUI.Redux;

namespace UI.Models
{
    public static class MainActions
    {
        internal static readonly ActionCreator TOGGLE_SIDE_PANEL_LEFT =
            $"{SliceNames.MAIN_SLICE}/ToggleSidePanelLeft";
        internal static readonly ActionCreator TOGGLE_SIDE_PANEL_RIGHT =
            $"{SliceNames.MAIN_SLICE}/ToggleSidePanelRight";
    }
}
