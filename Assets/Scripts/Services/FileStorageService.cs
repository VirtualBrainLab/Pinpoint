using System;
using System.IO;
using UnityEngine;

namespace Services
{
    /// <summary>
    /// Provides methods to save and load data to/from local JSON files.
    /// </summary>
    public class FileStorageService
    {
        /// <summary>
        /// Saves a value of type <typeparamref name="T"/> to a JSON file.
        /// </summary>
        /// <typeparam name="T">The type of the value to save.</typeparam>
        /// <param name="filePath">The full file path where the JSON file will be saved.</param>
        /// <param name="value">The value to save.</param>
        /// <returns>True if the save was successful, false otherwise.</returns>
        public bool SaveToFile<T>(string filePath, T value)
        {
            try
            {
                var json = JsonUtility.ToJson(value, true);
                File.WriteAllText(filePath, json);
                Debug.Log($"Saved state to: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save to file {filePath}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads a value of type <typeparamref name="T"/> from a JSON file.
        /// </summary>
        /// <typeparam name="T">The type of the value to load.</typeparam>
        /// <param name="filePath">The full file path to the JSON file to load.</param>
        /// <param name="result">The loaded value, or default if loading failed.</param>
        /// <returns>True if the load was successful, false otherwise.</returns>
        public bool LoadFromFile<T>(string filePath, out T result) where T : new()
        {
            result = default;
            
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"File not found: {filePath}");
                    return false;
                }

                var json = File.ReadAllText(filePath);
                result = JsonUtility.FromJson<T>(json);
                Debug.Log($"Loaded state from: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load from file {filePath}: {e.Message}");
                return false;
            }
        }
    }
}
