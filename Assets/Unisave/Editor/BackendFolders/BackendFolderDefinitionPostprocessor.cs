using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unisave.Editor.BackendFolders
{
    public class BackendFolderDefinitionPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            bool isAnyInteresting = importedAssets
                .Concat(deletedAssets)
                .Concat(movedAssets)
                .Concat(movedFromAssetPaths)
                .Any(IsInteresting);

            if (isAnyInteresting)
                BackendFolderDefinition.InvokeAnyChangeEvent();
        }

        private static bool IsInteresting(string path)
            => path.EndsWith(".asset");
    }
}