using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Unisave.Editor.BackendUploading.Hooks
{
    /// <summary>
    /// Hooks into compilation finished event.
    /// Abstracts away compilation of individual assemblies
    /// into a single recompilation event.
    /// This single event then triggers further logic.
    /// </summary>
    public static class CompilationFinishedHook
    {
        /// <summary>
        /// Registers the assembly compilation finished hook
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnInitializeOnLoad()
        {
            CompilationPipeline.assemblyCompilationFinished
                += OnAssemblyCompilationFinished;
        }

        /// <summary>
        /// When any single assembly inside the project gets compiled
        /// </summary>
        private static void OnAssemblyCompilationFinished(
            string assemblyPath, CompilerMessage[] messages
        )
        {
            HookImplementations.OnAssemblyCompilationFinished();
        }
    }
}
