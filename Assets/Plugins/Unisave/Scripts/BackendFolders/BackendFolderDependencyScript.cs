using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Unisave.BackendFolders
{
    /// <summary>
    /// Add this script to an example scene, if that scene depends
    /// on some external backend folder and needs that folder to be uploaded
    /// </summary>
    public class BackendFolderDependencyScript : MonoBehaviour
    {
        /// <summary>
        /// Dependency backend folders that should be all enabled
        /// for this scene to work
        /// </summary>
        public BackendFolderDefinition[] dependencies
            = Array.Empty<BackendFolderDefinition>();

        private void OnValidate()
        {
            // do not validate quite yet, have some time to settle
            // (because this gets called right when scene opens and
            // Unity does not like what we do here during awake, etc...)
            StartCoroutine(ValidationCoroutine());
        }

        private IEnumerator ValidationCoroutine()
        {
            // skip a frame
            yield return null;
            
            if (dependencies == null)
                yield break;
            
            var disabledDependencies = dependencies
                .Where(d => d != null && !d.IsEligibleForUpload())
                .ToArray();

            // everything is OK
            if (disabledDependencies.Length == 0)
                yield break;
            
            // show the window, there are disabled dependencies
            #if UNITY_EDITOR
            BackendFolderDependencyWindow.Show(disabledDependencies);
            #endif
        }
    }
}