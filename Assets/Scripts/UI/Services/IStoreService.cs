using Unity.AppUI.Redux;

namespace UI.Services
{
    /// <summary>
    /// Provides access to the application's Redux store.
    /// </summary>
    public interface IStoreService
    {
        /// <summary>
        /// Gets the Redux store instance used by the application.
        /// </summary>
        Store Store { get; }
    }
}