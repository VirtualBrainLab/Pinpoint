using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unisave.Editor.BackendFolders
{
    /// <summary>
    /// Utility functions for working with
    /// </summary>
    public static class BackendFolderUtility
    {
        public const string DefaultDefinitionFileName = "MyBackend.asset";
        public const string DefaultBackendFolderName = "Backend";
        
        public static void CreateBackendFolder(string parentFolderPath)
        {
            if (AssetDatabase.IsValidFolder(
                    $"{parentFolderPath}/{DefaultBackendFolderName}"
                ))
            {
                EditorUtility.DisplayDialog(
                    "Backend folder creation failed",
                    $"Folder named '{DefaultBackendFolderName}' already exists in this directory.",
                    "OK"
                );
                return;
            }
            
            AssetDatabase.CreateFolder(
                parentFolderPath,
                DefaultBackendFolderName
            );
            
            CreateDefinitionFileInFolder(
                $"{parentFolderPath}/{DefaultBackendFolderName}"
            );
            
            Templates.CreateScriptFromTemplate(
                $"{parentFolderPath}/{DefaultBackendFolderName}/PlayerEntity.cs",
                "PlayerEntity.txt",
                null
            );
        }
        
        public static void CreateDefinitionFileInFolder(string folderPath)
        {
            // Construct path to the backend folder definition file
            string defPath = folderPath + "/" + DefaultDefinitionFileName;
			
            if (File.Exists(defPath))
            {
                EditorUtility.DisplayDialog(
                    title: "Action failed",
                    message: "Selected folder already contains a backend definition file.",
                    ok: "OK"
                );
                return;
            }
			
            // Create the backend folder definition file
            var def = ScriptableObject.CreateInstance<BackendFolderDefinition>();
            AssetDatabase.CreateAsset(def, defPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}