using System;
using System.IO;
using Unisave.BackendFolders;
using Unisave.Editor.BackendFolders;
using Unisave.Editor.BackendUploading;
using Unisave.Editor.BackendUploading.States;
using Unisave.Foundation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unisave.Editor.Windows.Main.Tabs
{
    public class BackendTabController : ITabContentController
    {
        public Action<TabTaint> SetTaint { get; set; }
        
        private readonly VisualElement root;

        private readonly Uploader uploader = Uploader.Instance;

        private Toggle automaticUploadToggle;
        private Button manualUploadButton;
        private Label lastUploadAtLabel;
        private Label backendHashLabel;

        private VisualElement uploadingSection;
        private Label uploadingNumbers;
        private ProgressBar uploadingProgressBar;
        private Button cancelUploadButton;

        private VisualElement doneSection;
        private VisualElement uploadingOutputContainer;
        private Label uploadingOutputLabel;
        private HelpBox uploadResultMessage;
        private TextField uploadingOutput;
        private Button printOutputButton;
        
        private VisualTreeAsset backendDefinitionItem;
        private VisualElement enabledBackendDefinitions;
        private VisualElement disabledBackendDefinitions;

        public BackendTabController(VisualElement root)
        {
            this.root = root;
        }

        public void OnCreateGUI()
        {
            // === Backend upload and compilation ===
            
            automaticUploadToggle = root.Q<Toggle>(name: "automatic-upload-toggle");
            manualUploadButton = root.Q<Button>(name: "manual-upload-button");
            lastUploadAtLabel = root.Q<Label>(name: "last-upload-at-label");
            backendHashLabel = root.Q<Label>(name: "backend-hash-label");

            uploadingSection = root.Q(name: "us-uploading");
            uploadingNumbers = root.Q<Label>(name: "us-uploading__numbers");
            uploadingProgressBar = root.Q<ProgressBar>(name: "us-uploading__progress-bar");
            cancelUploadButton = root.Q<Button>(name: "us-uploading__cancel");

            doneSection = root.Q(name: "us-done");
            uploadResultMessage = root.Q<HelpBox>(name: "us-done__message");
            uploadingOutputContainer = root.Q(name: "us-done__output-container");
            uploadingOutputLabel = root.Q<Label>(name: "us-done__output-label");
            uploadingOutput = root.Q<TextField>(name: "us-done__output");
            printOutputButton = root.Q<Button>(name: "us-done__print");
            
            manualUploadButton.clicked += RunManualCodeUpload;
            cancelUploadButton.clicked += CancelBackendUpload;
            printOutputButton.clicked += PrintCompilerOutput;
            
            // === Backend folder definition files ===
            
            backendDefinitionItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Plugins/Unisave/Editor/Windows/Main/UI/BackendDefinitionItem.uxml"
            );
            enabledBackendDefinitions = root.Q(name: "enabled-backend-definitions");
            disabledBackendDefinitions = root.Q(name: "disabled-backend-definitions");

            root.Q<Button>(className: "add-backend-folder__button").clicked
                += AddExistingBackendFolder;
            
            BackendFolderDefinition.OnAnyChange += OnObserveExternalState;
            uploader.OnStateChange += OnObserveExternalState;
            
            // === Other ===
            
            root.RegisterCallback<DetachFromPanelEvent>(e => {
                OnDetachFromPanel();
            });
        }

        private void OnDetachFromPanel()
        {
            BackendFolderDefinition.OnAnyChange -= OnObserveExternalState;
            uploader.OnStateChange -= OnObserveExternalState;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void OnObserveExternalState()
        {
            var preferences = UnisavePreferences.Resolve();
            
            RenderTabTaint();
            
            RenderUploaderState();
            
            // === Backend upload and compilation ===
            
            automaticUploadToggle.value = preferences.AutomaticBackendUploading;
            lastUploadAtLabel.text = preferences.LastBackendUploadAt
                ?.ToString("yyyy-MM-dd H:mm:ss") ?? "Never";
            backendHashLabel.text = string.IsNullOrWhiteSpace(preferences.BackendHash)
                ? "<not computed yet>"
                : preferences.BackendHash;
            
            // === Backend folder definition files ===
            
            var defs = BackendFolderDefinition.LoadAll();
            RenderBackendFolderDefinitions(defs);
        }

        public void OnWriteExternalState()
        {
            var preferences = UnisavePreferences.Resolve();
            
            if (preferences.AutomaticBackendUploading == automaticUploadToggle.value)
                return; // no need to save anything (IMPORTANT!)
            
            SaveUnisavePreferences();
        }
        
        void SaveUnisavePreferences()
        {
            var preferences = UnisavePreferences.Resolve();
            
            preferences.AutomaticBackendUploading = automaticUploadToggle.value;
            
            preferences.Save();
            
            OnObserveExternalState();
        }

        private void RenderTabTaint()
        {
            if (uploader.State is StateException
                || uploader.State is StateCompilationError)
            {
                SetTaint(TabTaint.Error);
            }
            else
            {
                SetTaint(TabTaint.None);
            }
        }

        private void RenderUploaderState()
        {
            uploadingSection.EnableInClassList(
                "is-hidden", !(uploader.State is StateUploading)
            );
            doneSection.EnableInClassList(
                "is-hidden", uploader.State is StateUploading
            );
            uploadingOutputContainer.EnableInClassList(
                "is-hidden", uploader.State == null
            );
            
            switch (uploader.State)
            {
                case null:
                    uploadResultMessage.messageType = HelpBoxMessageType.None;
                    uploadResultMessage.text = "No compilation info available. " +
                        "Click the manual upload button above to download it.";
                    uploadingOutput.value = "";
                    break;
                
                case StateUploading state:
                    uploadingProgressBar.value = state.Progress;
                    uploadingNumbers.text = $"{state.PerformedSteps} / {state.TotalSteps}";
                    break;
                
                case StateException state:
                    uploadResultMessage.messageType = HelpBoxMessageType.Error;
                    uploadResultMessage.text = "An exception was thrown!\n" + state.ExceptionMessage;
                    uploadingOutputLabel.text = "Exception body";
                    uploadingOutput.value = state.ExceptionBody?.Trim() ?? "";
                    break;
                
                case StateCompilationError state:
                    uploadResultMessage.messageType = HelpBoxMessageType.Error;
                    uploadResultMessage.text = "Backend compilation failed!";
                    uploadingOutputLabel.text = "Backend compiler output";
                    uploadingOutput.value = state.CompilerOutput?.Trim() ?? "";
                    break;
                
                case StateSuccess state:
                    uploadResultMessage.messageType = HelpBoxMessageType.Info;
                    uploadResultMessage.text = "Backend has been compiled successfully.";
                    uploadingOutputLabel.text = "Backend compiler output";
                    uploadingOutput.value = state.CompilerOutput?.Trim() ?? "";
                    break;
            }
        }

        private void RenderBackendFolderDefinitions(BackendFolderDefinition[] defs)
        {
            enabledBackendDefinitions.Clear();
            disabledBackendDefinitions.Clear();
            
            foreach (var def in defs)
            {
                bool isEnabled = def.IsEligibleForUpload();
                
                VisualElement item = backendDefinitionItem.Instantiate();
                
                var label = item.Q<Label>(className: "backend-def__label");
                label.text = def.FolderPath?.Replace('\\', '/');
                
                var button = item.Q<Button>(className: "backend-def__button");
                button.text = isEnabled ? "Disable" : "Enable";
                button.SetEnabled(def.CanToggleBackend());
                button.clicked += () => {
                    ToggleBackendFolder(def);
                    OnObserveExternalState(); // refresh window
                };
                
                var field = item.Q<ObjectField>(className: "backend-def__field");
                field.objectType = typeof(BackendFolderDefinition);
                field.value = def;
                
                if (isEnabled)
                    enabledBackendDefinitions.Add(item);
                else
                    disabledBackendDefinitions.Add(item);
            }
        }
        
        ////////////////////
        // Action Methods //
        ////////////////////
		
        void RunManualCodeUpload()
        {
            uploader.UploadBackend(
                verbose: true,
                blockThread: false
            );
        }

        void CancelBackendUpload()
        {
            uploader.CancelRunningUpload();
        }

        void PrintCompilerOutput()
        {
            switch (uploader.State)
            {
                case StateSuccess state:
                    Debug.Log(state.CompilerOutput);
                    break;
                
                case StateException state:
                    Debug.LogError(state.ExceptionBody);
                    break;
                
                case StateCompilationError state:
                    Debug.LogError(state.CompilerOutput);
                    break;
            }
        }
		
        void ToggleBackendFolder(BackendFolderDefinition def)
        {
            def.ToggleBackendState();
            
            // save the backend definition file
            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssetIfDirty(def);
            
            // save preferences (may have been edited)
            var preferences = UnisavePreferences.Resolve();
            preferences.Save();
            
            HighlightBackendFolderInInspector(def);

            // trigger backend upload
            BackendFolderDefinition.InvokeAnyChangeEvent();
        }

        void HighlightBackendFolderInInspector(BackendFolderDefinition def)
        {
            Selection.activeObject = def;
        }
        
        private void AddExistingBackendFolder()
        {
            // display select folder dialog
            string selectedPath = EditorUtility.OpenFolderPanel(
                "Add Existing Backend Folder", "Assets", ""
            );

            // action cancelled
            if (string.IsNullOrEmpty(selectedPath))
                return;

            // get OS-specific directory separators
            selectedPath = Path.GetFullPath(selectedPath);
			
            // get path inside the assets folder
            // (the trailing separator is needed for the local-path conversion to work)
            string assetsPath = Path.GetFullPath("Assets" + Path.DirectorySeparatorChar);
            
            if (selectedPath.StartsWith(assetsPath))
            {
                // convert to project-relative path
                selectedPath = Path.Combine("Assets", selectedPath.Substring(assetsPath.Length));
            }
            else
            {
                EditorUtility.DisplayDialog(
                    title: "Action failed",
                    message: "Selected folder is not inside the Assets " +
                             "folder of this Unity project. It cannot be added.",
                    ok: "OK"
                );
                return;
            }
			
            BackendFolderUtility.CreateDefinitionFileInFolder(selectedPath);
        }
    }
}