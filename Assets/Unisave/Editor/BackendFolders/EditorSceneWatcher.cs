using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unisave.Editor.BackendFolders
{
    /// <summary>
    /// Watches scene openings and closings and fires the backend folder
    /// definitions change event, so that definitions that depend on open
    /// scenes are updated properly by anyone watching.
    /// </summary>
    public static class EditorSceneWatcher
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => {
                BackendFolderDefinition.InvokeAnyChangeEvent();
            };
            
            EditorSceneManager.sceneClosed += scene => {
                BackendFolderDefinition.InvokeAnyChangeEvent();
            };
        }
    }
}