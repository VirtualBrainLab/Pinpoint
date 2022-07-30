using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Unisave.Editor.BackendUploading.Hooks
{
    /// <summary>
    /// Hooks into game building.
    /// Triggers the hook before a build starts.
    /// </summary>
    public class BuildPreprocessingHook : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            HookImplementations.OnPreprocessBuild(report);
        }
    }
}