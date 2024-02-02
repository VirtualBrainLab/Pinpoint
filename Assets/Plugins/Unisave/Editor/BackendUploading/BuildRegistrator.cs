using LightJson;
using Unisave.Foundation;
using Unisave.Utils;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Application = UnityEngine.Application;

namespace Unisave.Editor.BackendUploading
{
    public class BuildRegistrator
    {
        private readonly UnisavePreferences preferences;

        private readonly ApiUrl apiUrl;
        
        /// <summary>
        /// Creates the default instance of the registrator
        /// that uses correct preferences.
        /// </summary>
        public static BuildRegistrator GetDefaultInstance()
        {
            return new BuildRegistrator(
                UnisavePreferences.Resolve()
            );
        }
        
        private BuildRegistrator(UnisavePreferences preferences)
        {
            this.preferences = preferences;

            apiUrl = new ApiUrl(preferences.ServerUrl);
        }
        
        /// <summary>
        /// Registers the build in the Unisave cloud
        /// </summary>
        public void RegisterBuild(BuildReport report)
        {
            Debug.Log("[Unisave] Registering the build...");
            
            // check server reachability
            if (!Http.UrlReachable(apiUrl.Index()))
            {
                Debug.LogError(
                    "[Unisave] Skipping build registration, because the " +
                    $"domain '{apiUrl.Index()}' is not reachable."
                );
                return;
            }
            
            // register the build
            Http.Post(
                apiUrl.RegisterBuild(),
                new JsonObject()
                    .Add("gameToken", preferences.GameToken)
                    .Add("editorKey", preferences.EditorKey)
                    
                    .Add("backendHash", preferences.BackendHash)
                    .Add("buildGuid", report.summary.guid.ToString())
                    .Add("versionString", Application.version)
                    .Add("platform", report.summary.platform.ToString())
                    .Add("compiledAt", report.summary.buildStartedAt)
            );
            
            Debug.Log(
                $"[Unisave] Build '{report.summary.guid}' has been " +
                $"registered to backend '{preferences.BackendHash}'."
            );
        }
    }
}