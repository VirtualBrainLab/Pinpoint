using System.Linq;
using Unisave.Editor.Auditing;
using UnityEditor;
using UnityEngine;

namespace Unisave.Editor.Windows.Main
{
    public class OpenUnisaveAfterImport : AssetPostprocessor
    {
        private const string TriggeringAssetName = nameof(OpenUnisaveAfterImport) + ".cs";
        
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            foreach (string asset in importedAssets)
            {
                if (asset?.EndsWith(TriggeringAssetName) ?? false)
                {
                    Debug.Log(
                        "[THANK YOU FOR IMPORTING UNISAVE]\nYou can continue " +
                        "with the instructions in the Unisave window that just " +
                        "opened. If not, you can open it manually " +
                        $"from menu '{UnisaveMainWindow.UnityMenuPath}'."
                    );
                    
                    UnisaveAuditing.EmitEvent(
                        eventType: "asset.import",
                        message: "Unity asset has been imported."
                    );
                    
                    var window = UnisaveMainWindow.ShowTab(MainWindowTab.Home);
                    window.CenterOnMainWin();
                    
                    return;
                }
            }
        }
    }
}