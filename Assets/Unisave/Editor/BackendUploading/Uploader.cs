using System;
using System.Threading;
using System.Threading.Tasks;
using Unisave.Editor.BackendUploading.Snapshotting;
using Unisave.Editor.BackendUploading.States;
using Unisave.Editor.Windows.Main;
using Unisave.Foundation;
using Unisave.Utils;
using UnityEngine;

namespace Unisave.Editor.BackendUploading
{
    public class Uploader
    {
        // singleton instance backing field
        private static Uploader instance;
        
        /// <summary>
        /// Singleton instance of the uploader
        /// </summary>
        public static Uploader Instance
        {
            get
            {
                if (instance == null)
                    instance = new Uploader();
                else
                    instance.RefreshPreferences();

                return instance;
            }
        }

        private UnisavePreferences preferences;
        private ApiUrl apiUrl;

        public bool AutomaticUploadingEnabled =>
            preferences.AutomaticBackendUploading;

        public bool IsCloudConnectionSetUp =>
            !string.IsNullOrEmpty(preferences.GameToken)
            && !string.IsNullOrEmpty(preferences.EditorKey);

        /// <summary>
        /// Backend uploader state
        /// </summary>
        public BaseState State { get; private set; }
        
        /// <summary>
        /// Triggered when the uploader state changes
        /// </summary>
        public event Action OnStateChange;
        
        /// <summary>
        /// Holds the uploading task, when one is running
        /// </summary>
        private Task uploadingTask;

        private Uploader()
        {
            RefreshPreferences();

            State = BaseState.RestoreFromEditorPrefs(preferences.GameToken);
        }

        /// <summary>
        /// UnisavePreferences should not be kept for long, therefore we need
        /// to re-resolve them often enough to prevent their spoiling.
        /// </summary>
        private void RefreshPreferences()
        {
            preferences = UnisavePreferences.Resolve();
            apiUrl = new ApiUrl(preferences.ServerUrl);
        }

        /// <summary>
        /// Performs all the uploading
        /// </summary>
        /// <param name="verbose">Print additional info to console</param>
        /// <param name="blockThread">
        /// When called from UI, it should be non-blocking. When called after
        /// assembly compilation it needs to be blocking otherwise the .NET
        /// runtime refreshes the new assembly code and the upload gets killed
        /// half-way through.
        /// </param>
        /// <param name="snapshot">
        /// An optional backend snapshot to be provided. If left at null,
        /// a snapshot according to all backend definition files is taken.
        /// </param>
        public void UploadBackend(
            bool verbose,
            bool blockThread,
            BackendSnapshot snapshot = null
        )
        {
            RefreshPreferences();
            
            // check that there isn't another upload running
            if (uploadingTask != null && !uploadingTask.IsCompleted)
            {
                Debug.LogWarning(
                    "[Unisave] Ignoring backend upload, " +
                    "because another one is still running."
                );
                return;
            }
            
            // announce the beginning
            if (verbose)
                Debug.Log("[Unisave] Starting backend upload...");
            
            // recalculate backend hash and store it in preferences
            if (snapshot == null)
                snapshot = BackendSnapshot.Take();
            StoreBackendHash(snapshot);
            
            preferences.LastBackendUploadAt = DateTime.Now;
            preferences.LastUploadedBackendHash = snapshot.BackendHash;
            preferences.Save();

            // forget whatever state is persisted
            BaseState.ClearEditorPrefs(preferences.GameToken);
            
            // switch to the uploading state
            var uploadingState = new StateUploading();
            State = uploadingState;
            OnStateChange?.Invoke();
            
            // prepare the uploading job
            var job = new UploadingJob(
                verbose: verbose,
                gameToken: preferences.GameToken,
                editorKey: preferences.EditorKey,
                apiUrl: apiUrl,
                snapshot: snapshot,
                uploadingState: uploadingState,
                setState: s => {
                    State = s;
                    if (!blockThread) // only when in UI thread
                        OnStateChange?.Invoke();
                }
            );

            // called when the uploading job finishes
            void DoneCallback()
            {
                uploadingTask = null;
                
                // persist uploader state
                State?.StoreToEditorPrefs(preferences.GameToken);
                
                // Focus unisave window on error
                if (State is StateException || State is StateCompilationError)
                    UnisaveMainWindow.ShowTab(MainWindowTab.Backend);
            }

            // run the uploading job
            if (blockThread)
            {
                // needs to run on different thread,
                // otherwise we deadlock ourselves by doing .Wait()
                Task.Run(() => job.Run()).Wait();
                
                // we trigger the state change event once, after everything is done
                OnStateChange?.Invoke();
                
                DoneCallback();
            }
            else
            {
                uploadingTask = job.Run(DoneCallback);
            }
        }

        /// <summary>
        /// If there's a running upload, it gets cancelled, otherwise nothing happens
        /// </summary>
        public void CancelRunningUpload()
        {
            StateUploading state = State as StateUploading;

            if (state == null)
                return;
            
            state.CancellationTokenSource.Cancel();
        }
        
        /// <summary>
        /// Goes through the backend folder and updates the backend hash
        /// stored in unisave preferences
        /// </summary>
        /// <returns>
        /// True if the backend has changed and should be uploaded
        /// </returns>
        public bool RecalculateBackendHash()
        {
            string lastUploadedHash = preferences.LastUploadedBackendHash;
            
            var snapshot = BackendSnapshot.Take();
            StoreBackendHash(snapshot);

            return lastUploadedHash != snapshot.BackendHash;
        }

        private void StoreBackendHash(BackendSnapshot snapshot)
        {
            if (preferences.BackendHash != snapshot.BackendHash)
            {
                preferences.BackendHash = snapshot.BackendHash;
                preferences.Save();
            }
        }
    }
}