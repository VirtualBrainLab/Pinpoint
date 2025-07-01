using UnityEngine;

namespace UI.Services
{
    /// <summary>
    /// Provides methods to store and retrieve data in local storage using Unity's PlayerPrefs.
    /// </summary>
    public class LocalStorageService : ILocalStorageService
    {
        /// <summary>
        /// Retrieves a value of type <typeparamref name="T"/> from local storage by key.
        /// </summary>
        /// <remarks>If the key does not exist, stores and returns the provided default value.</remarks>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="key">The key associated with the value.</param>
        /// <param name="defaultValue">The value to return and store if the key does not exist.</param>
        /// <returns>The value retrieved from local storage, or the default value if not found.</returns>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (PlayerPrefs.HasKey(key))
            {
                var json = PlayerPrefs.GetString(key);
                return JsonUtility.FromJson<T>(json);
            }

            SetValue(key, defaultValue);
            return defaultValue;
        }

        /// <summary>
        /// Stores a value of type <typeparamref name="T"/> in local storage under the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the value to store.</typeparam>
        /// <param name="key">The key to associate with the value.</param>
        /// <param name="value">The value to store.</param>
        public void SetValue<T>(string key, T value)
        {
            var json = JsonUtility.ToJson(value);
            PlayerPrefs.SetString(key, json);
        }
    }
}
