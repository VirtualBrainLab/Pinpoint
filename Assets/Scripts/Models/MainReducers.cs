using UI.Utils;
using Unity.AppUI.Redux;

namespace Models
{
    /// <summary>
    /// Contains reducer methods for updating the main application state.
    /// </summary>
    public static class MainReducers
    {
        /// <summary>
        /// Sets the mode of the application.
        /// </summary>
        /// <param name="state">The current main state.</param>
        /// <param name="action">The action containing the new mode as an integer payload.</param>
        /// <returns>A new <see cref="MainState"/> with the updated mode.</returns>
        public static MainState SetModeReducer(MainState state, IAction<MainMode> action)
        {
            return state with { MainMode = action.payload };
        }

        /// <summary>
        /// Toggles the open/close state of the left side panel.
        /// </summary>
        /// <param name="state">The current main state.</param>
        /// <param name="action">The action triggering the toggle.</param>
        /// <returns>A new <see cref="MainState"/> with the left side panel state toggled.</returns>
        public static MainState ToggleLeftSidePanelReducer(MainState state, IAction action)
        {
            return state with { IsLeftSidePanelOpen = !state.IsLeftSidePanelOpen };
        }

        /// <summary>
        /// Toggles the open/close state of the right side panel.
        /// </summary>
        /// <param name="state">The current main state.</param>
        /// <param name="action">The action triggering the toggle.</param>
        /// <returns>A new <see cref="MainState"/> with the right side panel state toggled.</returns>
        public static MainState ToggleRightSidePanelReducer(MainState state, IAction action)
        {
            return state with { IsRightSidePanelOpen = !state.IsRightSidePanelOpen };
        }
        
        public static MainState SetInputFocusedReducer(MainState state, IAction<bool> action)
        {
            return state with { IsInputFocused = action.payload };
        }
    }

    public static class MainActions
    {
        public static readonly ActionCreator<MainMode> SET_MODE =
            $"{SliceNames.MAIN_SLICE}/SetMode";
        public static readonly ActionCreator TOGGLE_LEFT_SIDE_PANEL =
            $"{SliceNames.MAIN_SLICE}/ToggleLeftSidePanel";
        public static readonly ActionCreator TOGGLE_RIGHT_SIDE_PANEL =
            $"{SliceNames.MAIN_SLICE}/ToggleRightSidePanel";
        public static readonly ActionCreator<bool> SET_INPUT_FOCUSED =
            $"{SliceNames.MAIN_SLICE}/SetInputFocused";
    }
}
