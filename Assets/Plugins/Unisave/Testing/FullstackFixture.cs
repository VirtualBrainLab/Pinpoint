using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unisave.Editor.BackendUploading;
using Unisave.Editor.BackendUploading.Snapshotting;
using Unisave.Foundation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unisave.Testing
{
    /// <summary>
    /// Inherit from this fixture to create a test case that uploads specific
    /// backend code and then runs tests against it, when the backend is
    /// actually executed in the cloud as realistically as possible.
    /// </summary>
    public abstract class FullstackFixture
    {
        /// <summary>
        /// The facet caller script that you can use to start Unisave operations.
        /// </summary>
        protected FacetCallerBehaviour caller;

        /// <summary>
        /// Game object that hosts the caller
        /// </summary>
        private GameObject callerGameObject;

        /// <summary>
        /// List of backend folder paths to use for the test fixture
        /// </summary>
        protected abstract string[] BackendFolders { get; }

        /// <summary>
        /// Unisave preferences before the fixture set itself up
        /// </summary>
        private UnisavePreferences stashedPreferences;
        
        [OneTimeSetUp]
        public void SetUpFixture()
        {
            // create the caller
            callerGameObject = new GameObject("FacetCallerBehaviour");
            caller = callerGameObject.AddComponent<FacetCallerBehaviour>();
            
            // stash current preferences, and cache their separate object instance
            stashedPreferences = UnisavePreferences.Resolve(bustCache: true);
            UnisavePreferences.Resolve(bustCache: true);
            
            // add the fullstack backend folder to the list of backend folders
            string[] backendFolders = BackendFolders
                .Append("Assets/Plugins/Unisave/Testing/FullstackBackend")
                .ToArray();
            
            // upload relevant backend folders
            // (this will mess up unisave preferences - that's why we stash them)
            var uploader = Uploader.Instance;
            uploader.UploadBackend(
                verbose: false,
                blockThread: true,
                snapshot: BackendSnapshot.Take(backendFolders)
            );
        }

        [OneTimeTearDown]
        public void TearDownFixture()
        {
            // restore the stashed preferences
            stashedPreferences.Save();
            UnisavePreferences.Resolve(bustCache: true);
            
            // destroy the caller
            Object.DestroyImmediate(callerGameObject);
        }

        /// <summary>
        /// Clears the database before a test is run.
        /// To disable this behaviour, simply override this method.
        /// </summary>
        [UnitySetUp]
        public virtual IEnumerator SetUp_ClearDatabase()
            => Asyncize.UnityTest(async () =>
        {
            await caller.DB_Clear();
        });
    }
}