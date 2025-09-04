using Unity.AppUI.Redux;
using Unity.AppUI.UI;

namespace Models
{
    /// <summary>
    /// Contains reducer methods for updating the main application state.
    /// </summary>
    public static class MainReducers
    {
        public static MainState SetIsAutomationModeActiveReducer(MainState state, IAction<bool> action)
        {
            return state with { IsAutomationModeActive = action.payload };
        }


        public static MainState SetMainSplitViewStateReducer(MainState state, IAction<SplitView.State> action)
        {
            return state with { MainSplitViewState = action.payload };
        }

        public static MainState SetLeftSidePanelTabIndexReducer(MainState state, IAction<int> action)
        {
            return state with { LeftSidePanelTabIndex = action.payload };
        }
    }

    public static class MainActions
    {
        public static readonly ActionCreator<bool> SET_IS_AUTOMATION_MODE_ACTIVE =
            $"{SliceNames.MAIN_SLICE}/SetIsAutomationModeActive";
        public static readonly ActionCreator<SplitView.State> SET_MAIN_SPLIT_VIEW_STATE =
            $"{SliceNames.MAIN_SLICE}/SetMainSplitViewState";
        public static readonly ActionCreator<int> SET_LEFT_SIDE_PANEL_TAB_INDEX =
            $"{SliceNames.MAIN_SLICE}/SetLeftSidePanelTabIndex";
    }
}
