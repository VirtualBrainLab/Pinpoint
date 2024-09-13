using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class ModelsManager
    {
        [MenuItem("Tools/Update Schemas")]
        public static void UpdateSchemas()
        {
            var webClient = new WebClient();
            
            webClient.DownloadFile(
                "https://raw.githubusercontent.com/VirtualBrainLab/vbl-aquarium/main/models/csharp/EphysLinkModels.cs",
                "Assets/Scripts/EphysLink/EphysLinkModels.cs");

            webClient.DownloadFile(
                "https://raw.githubusercontent.com/VirtualBrainLab/vbl-aquarium/main/models/csharp/PinpointModels.cs",
                "Assets/Scripts/Pinpoint/JSON/PinpointModels.cs");

            Debug.Log("Schemas updated successfully!");
        }

        private static void GetSchemas(string srcURL, string outFile)
        {

            if (!Directory.Exists(outFile))
            {
                Directory.CreateDirectory(outFile);
            }

            string[] files = Directory.GetFiles(srcURL, "*.cs");

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string destFilePath = Path.Combine(outFile, fileName);
                File.Copy(file, destFilePath, true);
            }

            AssetDatabase.Refresh();
        }
    }
}