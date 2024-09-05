using System.Collections;
using System.IO;
using System.Net;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Editor
{
    public static class ModelsManager
    {
        [MenuItem("Tools/Update Schemas")]
        public static void UpdateSchemas()
        {
            var webClient = new WebClient();

            EditorCoroutineUtility.StartCoroutineOwnerless(
                DownloadFile(
                    "https://raw.githubusercontent.com/VirtualBrainLab/vbl-aquarium/main/models/csharp/EphysLinkModels.cs",
                    "Assets/Scripts/EphysLink/EphysLinkModels.cs"
                )
            );

            Debug.Log("Schemas updated successfully!");
        }

        private static void GetSchemas(string srcURL, string outFile)
        {
            if (!Directory.Exists(outFile)) Directory.CreateDirectory(outFile);

            var files = Directory.GetFiles(srcURL, "*.cs");

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var destFilePath = Path.Combine(outFile, fileName);
                File.Copy(file, destFilePath, true);
            }

            AssetDatabase.Refresh();
        }

        private static IEnumerator DownloadFile(string url, string outputPath)
        {
            using var request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (
                request.result
                is UnityWebRequest.Result.ConnectionError
                or UnityWebRequest.Result.ProtocolError
            )
            {
                Debug.LogError(request.error);
            }
            else
            {
                File.WriteAllBytes(outputPath, request.downloadHandler.data);
                Debug.Log("File downloaded successfully!");
            }
        }
    }
}