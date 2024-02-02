using System;
using UnityEditor;

namespace Unisave.Editor.BackendUploading.Snapshotting
{
    /// <summary>
    /// Represents a single backend file that will be uploaded
    ///
    /// WARNING: All data this class access after construction is accessed
    /// from a background thread! Don't keep any Unity object references!
    /// </summary>
    public abstract class BackendFile
    {
        /// <summary>
        /// Path of the file relative to project root
        /// e.g. "Assets/Backend/..."
        /// </summary>
        public string Path { get; }
        
        /// <summary>
        /// GUID of the corresponding asset
        /// </summary>
        public string AssetGuid { get; }
        
        /// <summary>
        /// Type of the file in the server nomenclature
        /// </summary>
        public abstract string FileType { get; }

        /// <summary>
        /// Hash of the file
        /// </summary>
        public string Hash
        {
            get => hash ?? throw new InvalidOperationException(
                "Hash has not been computed yet."
            );

            protected set => hash = value;
        }
        
        /// <summary>
        /// Hash backing field
        /// </summary>
        private string hash = null;
        
        protected BackendFile(string assetGuid, string path)
        {
            AssetGuid = assetGuid;
            Path = path;
        }
        
        /// <summary>
        /// Computes the hash so that the Hash property becomes available
        /// </summary>
        public abstract void ComputeHash();

        /// <summary>
        /// Modifies the file to match the requirements the server needs
        /// </summary>
        public abstract byte[] ContentForUpload();
        
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"BackendFile<{GetType().Name}>[{hash}]({Path})";
        }
    }
}