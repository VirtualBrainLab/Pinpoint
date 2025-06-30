using UI.Utils;
using Unity.AppUI.Redux;

namespace UI.Models
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
        public static MainState ToggleSidePanelLeftReducer(MainState state, IAction action)
        {
            return state with { IsSidePanelLeftOpen = !state.IsSidePanelLeftOpen };
        }
        
        /// <summary>
        /// Toggles the open/close state of the right side panel.
        /// </summary>
        /// <param name="state">The current main state.</param>
        /// <param name="action">The action triggering the toggle.</param>
        /// <returns>A new <see cref="MainState"/> with the right side panel state toggled.</returns>
        public static MainState ToggleSidePanelRightReducer(MainState state, IAction action)
        {
            return state with { IsSidePanelRightOpen = !state.IsSidePanelRightOpen };
        }
        
    }
}