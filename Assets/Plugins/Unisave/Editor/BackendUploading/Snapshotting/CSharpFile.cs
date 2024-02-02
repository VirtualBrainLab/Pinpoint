using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Unisave.Editor.BackendUploading.Snapshotting
{
    /// <summary>
    /// Represents a C# source code file in the backend folder
    /// </summary>
    public class CSharpFile : BackendFile
    {
        /// <inheritdoc/>
        public override string FileType => "csharp";
        
        // Cannot keep reference to the MonoScript since it has to
        // be accessed only from the main thread.
        private readonly string scriptText;
        private readonly byte[] scriptBytes;
        
        /// <summary>
        /// Finds all C# scripts inside backend folders
        /// </summary>
        public static IEnumerable<CSharpFile> FindFiles(string[] backendFolders)
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:MonoScript", backendFolders
            );
            
            return guids
                .Select(LoadFromGuid)
                .Where(s => s != null);
        }

        private static CSharpFile LoadFromGuid(string assetGuid)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGuid);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

            // the given asset guid does not belong not a mono script
            if (script == null)
                return null;

            return new CSharpFile(assetGuid, path, script);
        }

        private CSharpFile(
            string assetGuid,
            string path,
            MonoScript script
        ) : base(assetGuid, path)
        {
            scriptText = script.text;
            scriptBytes = script.bytes;
        }
        
        /// <inheritdoc/>
        public override void ComputeHash()
        {
            // Do some preprocessing to keep the hash stable for a given
            // file across multiple platforms
            // (say when a team works on project)
            
            // Normalized line endings to "\n" by removing "\r"
            // This is ok even for old macs since C# compiler ignores whitespace
            string text = scriptText.Replace("\r", "");

            // turn text to bytes, always use UTF-8
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            
            // compute the hash
            Hash = Unisave.Editor.Hash.MD5(bytes);
        }
        
        /// <inheritdoc/>
        public override byte[] ContentForUpload()
        {
            // Use the raw bytes of the file.
            // Server compiler will deal with that.
            return scriptBytes;
        }
    }
}