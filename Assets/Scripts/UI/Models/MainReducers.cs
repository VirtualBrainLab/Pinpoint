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
        public static MainState SetModeReducer(MainState state, Action<int> action)
        {
            return state with { Mode = (MainModes)action.payload };
        }
        
    }
}