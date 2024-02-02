using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unisave.Foundation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unisave.BackendFolders
{
    /// <summary>
    /// This scriptable object, when put into an asset folder, marks this folder
    /// as a backend folder (folder to be uploaded to the server)
    /// </summary>
    public class BackendFolderDefinition : ScriptableObject
    {
        /// <summary>
        /// Event is invoked whenever any backend folder definition file is modified
        /// (modified, created, deleted, moved). Use this event to refresh any
        /// GUI related to backend folder definition files.
        /// </summary>
        public static event Action OnAnyChange;

        /// <summary>
        /// Invokes the event that notifies about backend folder definition changes
        /// </summary>
        public static void InvokeAnyChangeEvent()
        {
            OnAnyChange?.Invoke();
        }
    
        /// <summary>
        /// Use this method to load all existing backend folder definitions
        /// </summary>
        public static BackendFolderDefinition[] LoadAll()
        {
            #if UNITY_EDITOR
            
            return UnityEditor.AssetDatabase
                .FindAssets(
                    "t:" + nameof(BackendFolderDefinition)
                )
                .Select(guid => {
                    string definitionPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    string folderPath = Path.GetDirectoryName(definitionPath);
                    
                    var definition = UnityEditor.AssetDatabase.LoadAssetAtPath
                        <BackendFolderDefinition>(definitionPath);

                    // set asset metadata
                    definition.AssetGuid = guid;
                    definition.FilePath = definitionPath;
                    definition.FolderPath = folderPath;

                    return definition;
                })
                .OrderBy(d => d.FilePath)
                .ToArray();
            
            #else
            return Array.Empty<BackendFolderDefinition>();
            #endif
        }

        /// <summary>
        /// Returns true when this backend folder should be uploaded to the server
        /// </summary>
        public bool IsEligibleForUpload()
        {
            #if UNITY_EDITOR
            
            if (UploadBehaviour == UploadBehaviour.Never)
                return false;

            if (UploadBehaviour == UploadBehaviour.CheckScenes)
            {
                if (scenesToCheck == null)
                    return false;

                foreach (UnityEditor.SceneAsset scene in scenesToCheck)
                {
                    string path = UnityEditor.AssetDatabase.GetAssetPath(scene);

                    if (IsSceneLoaded(path))
                        return true;

                    if (IsSceneInBuildSettingsAndEnabled(path))
                        return true;
                }
                
                return false;
            }

            if (UploadBehaviour == UploadBehaviour.CheckPreferences)
            {
                var preferences = UnisavePreferences.Resolve();
                return preferences.PreferencesEnabledBackendFolders.Contains(
                    unisavePreferencesKey
                );
            }
            
            return true;
            
            #else
            return false;
            #endif
        }

        private bool IsSceneLoaded(string path)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                
                if (scene.path == path)
                    return true;
            }
            
            return false;
        }
        
        private bool IsSceneInBuildSettingsAndEnabled(string path)
        {
            #if UNITY_EDITOR
            
            foreach (UnityEditor.EditorBuildSettingsScene scene
                     in UnityEditor.EditorBuildSettings.scenes)
            {
                if (scene.path == path && scene.enabled)
                    return true;
            }
            
            #endif

            return false;
        }
        
        
        ///////////////////////////////
        // Enabled/disabled toggling //
        ///////////////////////////////

        /// <summary>
        /// Returns true if the backend can be toggled
        /// in the list of backend folders in the Unisave window
        /// </summary>
        public bool CanToggleBackend()
        {
            if (UploadBehaviour == UploadBehaviour.Never)
                return true;
            
            if (UploadBehaviour == UploadBehaviour.Always)
                return true;
            
            if (UploadBehaviour == UploadBehaviour.CheckPreferences)
                return true;

            return false;
        }

        /// <summary>
        /// Toggles the backend state between enabled and disabled
        /// </summary>
        public void ToggleBackendState()
        {
            if (UploadBehaviour == UploadBehaviour.Never)
            {
                UploadBehaviour = UploadBehaviour.Always;
            }
            else if (UploadBehaviour == UploadBehaviour.Always)
            {
                UploadBehaviour = UploadBehaviour.Never;
            }
            else if (UploadBehaviour == UploadBehaviour.CheckPreferences)
            {
                var preferences = UnisavePreferences.Resolve();
                var set = preferences.PreferencesEnabledBackendFolders;

                if (!set.Add(unisavePreferencesKey))
                    set.Remove(unisavePreferencesKey);
            }
        }
        
        /////////////////////////
        // Properties & fields //
        /////////////////////////
        
        /// <summary>
        /// When is this backend folder uploaded
        /// </summary>
        public UploadBehaviour UploadBehaviour
        {
            get => UploadBehaviour.FromString(uploadBehaviour);
            set => uploadBehaviour = value.Value;
        }
        
        [SerializeField]
        public string uploadBehaviour = "always";

        /// <summary>
        /// List of scenes to check, if they are used and if at least one
        /// is, then upload this backend folder.
        /// </summary>
        #if UNITY_EDITOR
        public List<UnityEditor.SceneAsset> scenesToCheck
            = new List<UnityEditor.SceneAsset>();
        #endif

        /// <summary>
        /// Key used for enabled/disabled tracking via Unisave preferences
        /// </summary>
        public string unisavePreferencesKey = "MyCompany.MyUnisaveModule";


        // === Asset-related metadata, set by the LoadAll method ===

        /// <summary>
        /// GUID of the asset that this scriptable object contains
        /// </summary>
        [field: NonSerialized]
        public string AssetGuid { get; private set; }
        
        /// <summary>
        /// Path to the backend folder
        /// </summary>
        [field: NonSerialized]
        public string FolderPath { get; private set; }
        
        /// <summary>
        /// Path to the definition file
        /// </summary>
        [field: NonSerialized]
        public string FilePath { get; private set; }
    }
}