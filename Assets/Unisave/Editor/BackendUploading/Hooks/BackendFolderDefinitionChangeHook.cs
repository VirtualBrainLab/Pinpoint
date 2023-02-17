using Unisave.Editor.BackendFolders;
using UnityEditor;

namespace Unisave.Editor.BackendUploading.Hooks
{
    public static class BackendFolderDefinitionChangeHook
    {
        /// <summary>
        /// Registers the backend def change hook
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            BackendFolderDefinition.OnAnyChange += OnAnyDefinitionChange;
        }

        /// <summary>
        /// When any backend folder definition file changes or a scene is loaded
        /// </summary>
        private static void OnAnyDefinitionChange()
        {
            HookImplementations.OnBackendFolderStructureChange();
        }
    }
}