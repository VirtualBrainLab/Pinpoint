namespace Services
{
    /// <summary>
    /// Defines methods for storing and retrieving values in local storage.
    /// </summary>
    public interface ILocalStorageService
    {
        /// <summary>
        /// Retrieves a value of type <typeparamref name="T"/> from local storage by key.
        /// If the key does not exist, returns the provided default value.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="key">The key associated with the value.</param>
        /// <param name="defaultValue">The value to return if the key does not exist.</param>
        /// <returns>The value retrieved from local storage, or the default value if not found.</returns>
        T GetValue<T>(string key, T defaultValue = default);

        /// <summary>
        /// Stores a value of type <typeparamref name="T"/> in local storage under the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the value to store.</typeparam>
        /// <param name="key">The key to associate with the value.</param>
        /// <param name="value">The value to store.</param>
        void SetValue<T>(string key, T value);
    }
}