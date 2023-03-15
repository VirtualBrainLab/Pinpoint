using System.Collections.Generic;
using System.Linq;
using Unisave.Editor.BackendFolders;

namespace Unisave.Editor.BackendUploading.Snapshotting
{
    /// <summary>
    /// Represents a snapshot of all backend folders,
    /// together with the hash and list of files to upload
    /// </summary>
    public class BackendSnapshot
    {
        public List<BackendFile> BackendFiles { get; }
        
        public string BackendHash { get; }
        
        private BackendSnapshot(List<BackendFile> files, string hash)
        {
            BackendFiles = files;
            BackendHash = hash;
        }
        
        public static BackendSnapshot Take()
        {
            // list all backend folders
            string[] backendFolders = ListBackendFolders();

            // get list of files to be uploaded
            var files = new List<BackendFile>();
            if (backendFolders.Length > 0)
            {
                files.AddRange(CSharpFile.FindFiles(backendFolders));
                // add other file types later
            }
            
            // compute file hashes
            files.ForEach(f => f.ComputeHash());
            
            // get all file hashes
            List<string> hashes = files.Select(f => f.Hash).ToList();
            
            // add hashes of contextual data (e.g. framework version)
            hashes.Add(Hash.MD5(FrameworkMeta.Version));
            
            // compute backend hash
            string backendHash = Hash.CompositeMD5(hashes);

            // build the snapshot to return
            return new BackendSnapshot(files, backendHash);
        }

        private static string[] ListBackendFolders()
        {
            BackendFolderDefinition[] defs = BackendFolderDefinition.LoadAll();

            return defs
                .Where(d => d.IsEligibleForUpload())
                .Select(d => d.FolderPath)
                .ToArray();
        }
    }
}