using Unity.AppUI.Redux;

namespace UI.Services
{
    /// <summary>
    /// Provides access to the application's Redux store implementation.
    /// </summary>
    public class StoreService : IStoreService
    {
        /// <summary>
        /// Gets the Redux store instance used by the application.
        /// </summary>
        public Store Store { get; } = new();
    }
}