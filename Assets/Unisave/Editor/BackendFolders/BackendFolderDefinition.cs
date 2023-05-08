using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unisave.Editor.BackendFolders
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
            return AssetDatabase
                .FindAssets(
                    "t:" + nameof(BackendFolderDefinition)
                )
                .Select(guid => {
                    string definitionPath = AssetDatabase.GUIDToAssetPath(guid);
                    string folderPath = Path.GetDirectoryName(definitionPath);
                    
                    var definition = AssetDatabase.LoadAssetAtPath
                        <BackendFolderDefinition>(definitionPath);

                    // set asset metadata
                    definition.AssetGuid = guid;
                    definition.FilePath = definitionPath;
                    definition.FolderPath = folderPath;

                    return definition;
                })
                .OrderBy(d => d.FilePath)
                .ToArray();
        }

        /// <summary>
        /// Returns true when this backend folder should be uploaded to the server
        /// </summary>
        public bool IsEligibleForUpload()
        {
            if (UploadBehaviour == UploadBehaviour.Never)
                return false;

            if (UploadBehaviour == UploadBehaviour.CheckScenes)
            {
                if (scenesToCheck == null)
                    return false;

                foreach (SceneAsset scene in scenesToCheck)
                {
                    string path = AssetDatabase.GetAssetPath(scene);

                    if (IsSceneLoaded(path))
                        return true;

                    if (IsSceneInBuildSettingsAndEnabled(path))
                        return true;
                }
                
                return false;
            }
            
            return true;
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
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == path && scene.enabled)
                    return true;
            }

            return false;
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
        internal string uploadBehaviour = "always";

        /// <summary>
        /// List of scenes to check, if they are used and if at least one
        /// is, then upload this backend folder.
        /// </summary>
        public List<SceneAsset> scenesToCheck = new List<SceneAsset>();


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