using UnityEditor;
using UnityEditor.SceneManagement;
using Unisave.BackendFolders;

namespace Unisave.Editor.BackendFolders
{
    /// <summary>
    /// Watches scene openings and closings and fires the backend folder
    /// definitions change event, so that definitions that depend on open
    /// scenes are updated properly by anyone watching.
    ///
    /// The delay between scene change event and backend refresh is there to:
    /// 1) debounce the changes
    /// 2) avoid unity crashes when we access assets while scenes are changing
    /// </summary>
    public static class EditorSceneWatcher
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => {
                // UnityEngine.Debug.Log("OPENED: " + scene.name);
                ScheduleRefresh();
            };
            
            EditorSceneManager.sceneClosed += scene => {
                // UnityEngine.Debug.Log("CLOSED: " + scene.name);
                ScheduleRefresh();
            };
        }

        private static int framesToWait = 0;
        private static bool updateRegistered = false;

        private static void ScheduleRefresh()
        {
            framesToWait = 5;

            if (!updateRegistered)
            {
                EditorApplication.update += EditorUpdate;
                updateRegistered = true;
            }
        }

        private static void PerformRefresh()
        {
            BackendFolderDefinition.InvokeAnyChangeEvent();
        }

        private static void EditorUpdate()
        {
            framesToWait -= 1;

            if (framesToWait <= 0)
            {
                PerformRefresh();
                
                EditorApplication.update -= EditorUpdate;
                updateRegistered = false;
            }
            // else
            // {
            //     UnityEngine.Debug.Log("Waiting, " + framesToWait);
            // }
        }
    }
}