using System;
using Unisave.Serialization;
using UnityEditor;
using UnityEngine;

namespace Unisave.Editor.BackendUploading.States
{
    public abstract class BaseState
    {
        private static string Key(string gameToken) =>
            "unisave.backendUploaderState:" + gameToken;
        
        /// <summary>
        /// Loads the last uploader state from editor prefs,
        /// may return null if there is nothing stored or on error
        /// </summary>
        public static BaseState RestoreFromEditorPrefs(string gameToken)
        {
            var data = EditorPrefs.GetString(Key(gameToken), null);

            if (string.IsNullOrEmpty(data))
                return null;

            try
            {
                return Serializer.FromJsonString<BaseState>(data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        /// <summary>
        /// Clears whatever is stored in editor prefs
        /// </summary>
        public static void ClearEditorPrefs(string gameToken)
        {
            EditorPrefs.DeleteKey(Key(gameToken));
        }

        /// <summary>
        /// Call this to persist the uploader state between editor restarts
        /// </summary>
        public void StoreToEditorPrefs(string gameToken)
        {
            EditorPrefs.SetString(
                Key(gameToken),
                Serializer.ToJson<BaseState>(this).ToString()
            );
        }
    }
}