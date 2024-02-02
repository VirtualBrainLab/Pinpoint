using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LightJson;
using Unisave.Editor.BackendUploading.Snapshotting;
using Unisave.Editor.BackendUploading.States;
using Unisave.Utils;
using UnityEngine;

namespace Unisave.Editor.BackendUploading
{
    public class UploadingJob
    {
        private readonly bool verbose;
        private readonly string gameToken;
        private readonly string editorKey;
        private readonly ApiUrl apiUrl;
        private readonly BackendSnapshot snapshot;
        private readonly StateUploading uploadingState;
        private readonly Action<BaseState> setState;

        public UploadingJob(
            bool verbose,
            string gameToken,
            string editorKey,
            ApiUrl apiUrl,
            BackendSnapshot snapshot,
            StateUploading uploadingState,
            Action<BaseState> setState
        )
        {
            this.verbose = verbose;
            this.gameToken = gameToken;
            this.editorKey = editorKey;
            this.apiUrl = apiUrl;
            this.snapshot = snapshot;
            this.uploadingState = uploadingState;
            this.setState = setState;
        }

        /// <summary>
        /// Runs the uploading task. This may run in a non-UI thread.
        /// </summary>
        public async Task Run(Action doneCallback = null)
        {
            try
            {
                await RunUnwrapped(uploadingState.CancellationTokenSource.Token);
            }
            catch (BackendUploadingException e)
            {
                Debug.LogError(e.Message);
                
                setState.Invoke(new StateException(e));
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                setState.Invoke(new StateException(e));
            }
            finally
            {
                doneCallback?.Invoke();
            }
        }

        private void Progress(int performed, int total)
        {
            uploadingState.PerformedSteps = performed;
            uploadingState.TotalSteps = total;
            setState.Invoke(uploadingState);
        }

        private async Task RunUnwrapped(CancellationToken cancellationToken)
        {
            // NOTE: Debug Methods are thread-safe
            // https://answers.unity.com/questions/714590/
            // thread-safety-and-debuglog.html

            Progress(0, snapshot.BackendFiles.Count + 3);
            
            await TestServerReachability(cancellationToken);
            
            Progress(1, snapshot.BackendFiles.Count + 3);

            HashSet<string> filePathsToUpload = await StartUpload(cancellationToken);

            if (filePathsToUpload == null)
            {   
                Progress(1, 2);
                
                if (verbose)
                    Debug.Log("[Unisave] Backend upload done, this backend has already been uploaded.");
                
                await FinishUpload(cancellationToken);
                return;
            }
            
            Progress(2, filePathsToUpload.Count + 3);

            // filter out files that needn't be uploaded
            IEnumerable<BackendFile> filteredFiles = snapshot.BackendFiles.Where(
                f => filePathsToUpload.Contains(f.Path)
            );

            // send individual files the server has asked for
            int uploadedFiles = 0;
            foreach (var file in filteredFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                await UploadFile(file, cancellationToken);
                
                uploadedFiles += 1;
                
                Progress(uploadedFiles + 2, filePathsToUpload.Count + 3);
            }
            
            if (verbose)
                Debug.Log("[Unisave] Backend upload done, starting server compilation...");

            bool success = await FinishUpload(cancellationToken);
            
            // Twice, because there's a problem on the server side that
            // if the compilation succeeds, it does not return compiler output
            // Once the server is fixed, this can be removed.
            if (success)
                await FinishUpload(cancellationToken);
        }

        private async Task TestServerReachability(CancellationToken cancellationToken)
        {
            if (await Http.UrlReachableAsync(apiUrl.Index(), cancellationToken))
                return;
            
            throw new BackendUploadingException(
                $"[Unisave] Server at '{apiUrl.Index()}' is not reachable.\n"
                + "If you want to work offline, you can disable automatic "
                + "backend uploading."
            );
        }

        private async Task<HashSet<string>> StartUpload(CancellationToken cancellationToken)
        {
            JsonArray files = new JsonArray(
                snapshot.BackendFiles.Select(
                    f => (JsonValue) new JsonObject()
                        .Add("path", f.Path)
                        .Add("hash", f.Hash)
                ).ToArray()
            );
            
            JsonObject startResponse = await Http.PostAsync(
                apiUrl.BackendUpload_Start(),
                new JsonObject()
                    .Add("game_token", gameToken)
                    .Add("editor_key", editorKey)
                    .Add("backend_hash", snapshot.BackendHash)
                    .Add("framework_version", FrameworkMeta.Version)
                    .Add("backend_folder_path", "<not-used-anymore>")
                    .Add("files", files),
                cancellationToken
            );
            
            if (startResponse["upload_has_finished"].AsBoolean)
                return null;
            
            return new HashSet<string>(
                startResponse["files_to_upload"]
                    .AsJsonArray
                    .Select(x => x.AsString)
            );
        }

        private async Task UploadFile(BackendFile file, CancellationToken cancellationToken)
        {
            if (verbose)
                Debug.Log($"[Unisave] Uploading '{file.Path}'...");
            
            string content = Convert.ToBase64String(file.ContentForUpload());
            
            await Http.PostAsync(
                apiUrl.BackendUpload_File(),
                new JsonObject()
                    .Add("game_token", gameToken)
                    .Add("editor_key", editorKey)
                    .Add("backend_hash", snapshot.BackendHash)
                    .Add("file", new JsonObject()
                        .Add("path", file.Path)
                        .Add("hash", file.Hash)
                        .Add("file_type", file.FileType)
                        .Add("content", content)
                    ),
                cancellationToken
            );
        }

        private async Task<bool> FinishUpload(CancellationToken cancellationToken)
        {
            JsonObject finishResponse = await Http.PostAsync(
                apiUrl.BackendUpload_Finish(),
                new JsonObject()
                    .Add("game_token", gameToken)
                    .Add("editor_key", editorKey)
                    .Add("backend_hash", snapshot.BackendHash),
                cancellationToken
            );

            bool compilerSuccess = finishResponse["compiler_success"].AsBoolean;
            string compilerOutput = finishResponse["compiler_output"].AsString;

            if (compilerSuccess)
            {
                if (verbose)
                    Debug.Log("[Unisave] Server compilation done.");
                
                setState.Invoke(new StateSuccess(compilerOutput));
                return true;
            }
            else
            {
                Debug.LogError(
                    "[Unisave] Server compile error:\n" + compilerOutput
                );
                
                setState.Invoke(new StateCompilationError(compilerOutput));
                return false;
            }
        }
    }
}