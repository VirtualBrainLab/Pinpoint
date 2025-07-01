using Unity.AppUI.Redux;

namespace Services
{
    /// <summary>
    /// Provides access to the application's Redux store.
    /// </summary>
    public interface IStoreService
    {
        /// <summary>
        /// Gets the application's Redux store instance.
        /// </summary>
        IStore<PartitionedState> Store { get; }

        /// <summary>
        /// Persists the current state of the store.
        /// </summary>
        void Save();
    }
}
