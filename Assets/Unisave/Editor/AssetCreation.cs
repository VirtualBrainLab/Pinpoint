using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using Unisave.Editor.BackendFolders;
using Unisave.BackendFolders;

namespace Unisave.Editor
{
    /// <summary>
    /// This class is responsible for the "Create >> Unisave >> *" context menu
    /// </summary>
    public static class AssetCreation
    {
        [MenuItem("Assets/Create/Unisave/Entity", false, 2)]
        public static void CreateEntity()
        {
            CreateScriptFromTemplate(
                defaultName: "NewEntity",
                templateName: "Entity.txt",
                wildcard: "#ENTITYNAME#"
            );
        }

        [MenuItem("Assets/Create/Unisave/Facet", false, 3)]
        public static void CreateFacet()
        {
            CreateScriptFromTemplate(
                defaultName: "NewFacet",
                templateName: "Facet.txt",
                wildcard: "#FACETNAME#"
            );
        }
        
        [MenuItem("Assets/Create/Unisave/Broadcasting/Channel", false, 4)]
        public static void CreateBroadcastingChannel()
        {
            CreateScriptFromTemplate(
                defaultName: "NewChannel",
                templateName: "Broadcasting/Channel.txt",
                wildcard: "#CLASSNAME#"
            );
        }
        
        [MenuItem("Assets/Create/Unisave/Broadcasting/Message", false, 5)]
        public static void CreateBroadcastingMessage()
        {
            CreateScriptFromTemplate(
                defaultName: "NewMessage",
                templateName: "Broadcasting/Message.txt",
                wildcard: "#CLASSNAME#"
            );
        }
        
        [MenuItem("Assets/Create/Unisave/Broadcasting/Client", false, 6)]
        public static void CreateBroadcastingClient()
        {
            CreateScriptFromTemplate(
                defaultName: "NewClient",
                templateName: "Broadcasting/Client.txt",
                wildcard: "#CLASSNAME#"
            );
        }
        
        [MenuItem("Assets/Create/Unisave/Steam authentication/Backend", false, 23)]
        public static void CreateSteamAuthenticationBackend()
        {
            var path = GetCurrentDirectoryPath();
            
            if (AssetDatabase.IsValidFolder(path + "/SteamAuthentication"))
            {
                EditorUtility.DisplayDialog(
                    "Steam authentication",
                    "Folder named 'SteamAuthentication' already exists in this directory.",
                    "OK"
                );
                return;
            }

            AssetDatabase.CreateFolder(
                path,
                "SteamAuthentication"
            );
            
            Templates.CreateScriptFromTemplate(
                path + "/SteamAuthentication/SteamLoginFacet.cs",
                "SteamAuthentication/SteamLoginFacet.txt",
                null
            );
        }
        
        [MenuItem("Assets/Create/Unisave/Steam authentication/Login client", false, 24)]
        public static void CreateSteamAuthenticationLoginClient()
        {
            var path = GetCurrentDirectoryPath();
            
            Templates.CreateScriptFromTemplate(
                path + "/SteamLoginClient.cs",
                "SteamAuthentication/SteamLoginClient.txt",
                null
            );
        }

        [MenuItem("Assets/Create/Unisave/Steam microtransactions/Backend", false, 25)]
        public static void CreateSteamMicrotransactionsBackend()
        {
            var path = GetCurrentDirectoryPath();
            
            if (AssetDatabase.IsValidFolder(path + "/SteamMicrotransactions"))
            {
                EditorUtility.DisplayDialog(
                    "Steam microtransactions",
                    "Folder named 'SteamMicrotransactions' already exists in this directory.",
                    "OK"
                );
                return;
            }

            AssetDatabase.CreateFolder(
                path,
                "SteamMicrotransactions"
            );
            AssetDatabase.CreateFolder(
                path + "/SteamMicrotransactions",
                "VirtualProducts"
            );
            
            Templates.CreateScriptFromTemplate(
                path + "/SteamMicrotransactions/VirtualProducts/ExampleVirtualProduct.cs",
                "SteamMicrotransactions/ExampleVirtualProduct.txt",
                null
            );
            Templates.CreateScriptFromTemplate(
                path + "/SteamMicrotransactions/IVirtualProduct.cs",
                "SteamMicrotransactions/IVirtualProduct.txt",
                null
            );
            Templates.CreateScriptFromTemplate(
                path + "/SteamMicrotransactions/SteamPurchasingServerFacet.cs",
                "SteamMicrotransactions/SteamPurchasingServerFacet.txt",
                null
            );
            Templates.CreateScriptFromTemplate(
                path + "/SteamMicrotransactions/SteamTransactionEntity.cs",
                "SteamMicrotransactions/SteamTransactionEntity.txt",
                null
            );
        }
        
        [MenuItem("Assets/Create/Unisave/Steam microtransactions/Client", false, 26)]
        public static void CreateSteamMicrotransactionsClient()
        {
            var path = GetCurrentDirectoryPath();
            
            Templates.CreateScriptFromTemplate(
                path + "/SteamPurchasingClient.cs",
                "SteamMicrotransactions/SteamPurchasingClient.txt",
                null
            );
        }
        
        [MenuItem("Assets/Create/Unisave/Player entity", false, 95)]
        public static void CreatePlayerEntity()
        {
            var path = GetCurrentDirectoryPath();
            
            Templates.CreateScriptFromTemplate(
                path + "/PlayerEntity.cs",
                "PlayerEntity.txt",
                null
            );
        }

        [MenuItem("Assets/Create/Unisave/Backend folder definition file", false, 99)]
        public static void CreateBackendFolderDefinition()
        {
            CreateAsset(
                GetCurrentDirectoryPath() + "/MyBackend.asset",
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    EditorGUIUtility.isProSkin ?
                        "Assets/Plugins/Unisave/Images/NewAssetIconWhite.png" :
                        "Assets/Plugins/Unisave/Images/NewAssetIcon.png"
                ),
                path => {
                    var def = ScriptableObject.CreateInstance<BackendFolderDefinition>();
                    
                    AssetDatabase.CreateAsset(def, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            );
        }
        
        [MenuItem("Assets/Create/Unisave/Backend folder", false, 100)]
        public static void CreateBackendFolder()
        {
            var path = GetCurrentDirectoryPath();

            BackendFolderUtility.CreateBackendFolder(path);
        }
        
        [MenuItem("Assets/Set folder as Unisave Backend", false, 500)]
        public static void SetFolderAsBackend()
        {
            var path = GetSelectedDirectoryPath();

            if (path == null)
            {
                Debug.LogError("Selected asset is not a folder");
                return;
            }
            
            BackendFolderUtility.CreateDefinitionFileInFolder(path);
        }

        /////////////
        // Helpers //
        /////////////

        /// <summary>
        /// Creates a text asset at given path from a given template
        /// (no wildcard substitution performed)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="templateName"></param>
        private static void CreateTextAssetFromTemplate(
            string path,
            string templateName,
            string wildcard,
            string wildcardValue
        )
        {
            AssetDatabase.CreateAsset(new TextAsset(), path);
                    
            File.WriteAllText(
                path,
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Assets/Plugins/Unisave/Templates/" + templateName
                ).text.Replace(wildcard, wildcardValue)
            );

            AssetDatabase.ImportAsset(path);
        }

        /// <summary>
        /// Creates a CS script from template
        /// </summary>
        /// <param name="defaultName">Default name of the file and the main class</param>
        /// <param name="templateName">Name of the template resource file</param>
        /// <param name="wildcard">Wildcard for the class name</param>
        private static void CreateScriptFromTemplate(
            string defaultName,
            string templateName,
            string wildcard
        )
        {
            CreateAsset(
                GetCurrentDirectoryPath() + "/" + defaultName + ".cs",
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    EditorGUIUtility.isProSkin ?
                    "Assets/Plugins/Unisave/Images/NewAssetIconWhite.png" :
                    "Assets/Plugins/Unisave/Images/NewAssetIcon.png"
                ),
                (pathName) => {
                    string name = Path.GetFileNameWithoutExtension(pathName);
                    
                    File.WriteAllText(
                        pathName,
                        AssetDatabase.LoadAssetAtPath<TextAsset>(
                            "Assets/Plugins/Unisave/Templates/" + templateName
                        ).text.Replace(wildcard, name)
                    );

                    AssetDatabase.ImportAsset(pathName);
                }
            );
        }

        /// <summary>
        /// Get directory where we want to create the new asset
        /// </summary>
        private static string GetCurrentDirectoryPath()
        {
            if (Selection.activeObject == null)
                return "Assets";

            var path = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());

            if (File.Exists(path))
                return Path.GetDirectoryName(path);

            return path;
        }
        
        /// <summary>
        /// Get directory that we stand in, or that is selected
        /// </summary>
        private static string GetSelectedDirectoryPath()
        {
            if (Selection.activeObject == null)
                return null;

            var path = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());

            if (Directory.Exists(path))
                return path;

            return null;
        }

        /// <summary>
        /// Starts the asset creation UI in project window and calls back the inserted name on success
        /// </summary>
        private static void CreateAsset(string defaultPathName, Texture2D icon, Action<string> callback)
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<CreateAssetCallback>().SetCallback(callback),
                defaultPathName,
                icon,
                null
            );
        }

        // inherits indirectly from ScriptableObject, so cannot be created using constructor
        private class CreateAssetCallback : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            private Action<string> callback;

            public CreateAssetCallback SetCallback(Action<string> callback)
            {
                this.callback = callback;
                return this;
            }

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                callback(pathName);
            }
        }
    }
}
