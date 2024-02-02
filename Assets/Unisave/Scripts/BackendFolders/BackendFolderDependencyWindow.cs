#if UNITY_EDITOR

using System;
using Unisave.Foundation;
using UnityEditor;
using UnityEngine;

namespace Unisave.BackendFolders
{
    /// <summary>
    /// This window is displayed by the <see cref="BackendFolderDependencyScript"/>
    /// which is used by Example Scenes to ensure enabled backend folder dependencies
    /// </summary>
    public class BackendFolderDependencyWindow : EditorWindow
    {
        public BackendFolderDefinition[] dependencies;

        public static void Show(BackendFolderDefinition[] dependencies)
        {
            foreach (var dep in dependencies)
            {
                Debug.LogWarning(
                    $"{dep.name} must be enabled for this example " +
                    $"scene to work. See the popped-up window."
                );
            }
            
            var window = EditorWindow.GetWindow<BackendFolderDependencyWindow>(
                utility: false,
                title: "Backend Folder Dependencies",
                focus: true
            );
            var size = new Vector2(400, 300);
            window.maxSize = size;
            window.minSize = size;
            window.dependencies = dependencies;
            window.Show();
        }
        
        void OnGUI()
        {
            if (dependencies == null)
                return;
            
            EditorGUILayout.HelpBox(
                "The following backend folders need to be uploaded for " +
                "this example scene to work. Click the button below to " +
                "enable them.",
                MessageType.Warning
            );
            
            foreach (var dep in dependencies)
            {
                EditorGUILayout.ObjectField(
                    dep,
                    typeof(BackendFolderDefinition),
                    allowSceneObjects: false
                );
            }
            
            if (GUILayout.Button("Enable All"))
            {
                EnableFolders();
                
                // close the window
                this.Close();
            }
        }

        private void EnableFolders()
        {
            foreach (var dep in dependencies)
            {
                dep.ToggleBackendState();
                
                // save the backend definition file
                EditorUtility.SetDirty(dep);
                AssetDatabase.SaveAssetIfDirty(dep);
            }
            
            // save preferences (may have been edited)
            var preferences = UnisavePreferences.Resolve();
            preferences.Save();
                
            // trigger backend upload
            BackendFolderDefinition.InvokeAnyChangeEvent();
        }
    }
}

#endif