using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unisave.Editor.BackendUploading
{
    public class SOFile : BackendFile
    {
        /// <inheritdoc/>
        public override string FileType => "scriptable-object";

        /// <summary>
        /// Finds all scriptable objects inside backend folders
        /// </summary>
        public static IEnumerable<SOFile> FindFiles(string[] backendFolders)
        {
            // NOTE: Will be implemented in the future
            
            return new SOFile[0];
            
//            string[] guids = AssetDatabase.FindAssets(
//                "t:ScriptableObject", backendFolders
//            );
//
//            return guids.Select(g => new SOFile(g));
        }

        private SOFile(string assetGuid) : base(assetGuid)
        {
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(Path);
            
            throw new System.NotImplementedException();
        }
        
        /// <inheritdoc/>
        public override void ComputeHash()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public override byte[] ContentForUpload()
        {
            throw new System.NotImplementedException();
        }
    }
}