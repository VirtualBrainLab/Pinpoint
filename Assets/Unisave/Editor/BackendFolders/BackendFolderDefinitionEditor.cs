using System;
using Unisave.Foundation;
using UnityEditor;
using UnityEngine;
using Unisave.BackendFolders;

namespace Unisave.Editor.BackendFolders
{
    [CustomEditor(typeof(BackendFolderDefinition))]
    public class BackendFolderDefinitionEditor : UnityEditor.Editor
    {
        private BackendFolderDefinition definition;

        private readonly UploadBehaviour[] uploadBehaviours = new[] {
            UploadBehaviour.Never,
            UploadBehaviour.Always,
            UploadBehaviour.CheckScenes,
            UploadBehaviour.CheckPreferences, 
        };

        private readonly string[] uploadBehaviourLabels = new[] {
            "Never",
            "Always",
            "When Scene Is Used",
            "When Listed In Unisave Preferences"
        };

        private SerializedProperty uploadBehaviourProperty;
        private SerializedProperty scenesToCheckProperty;
        private SerializedProperty unisavePreferencesKeyProperty;

        public void OnEnable()
        {
            definition = (BackendFolderDefinition) target;

            uploadBehaviourProperty = serializedObject.FindProperty(
                nameof(BackendFolderDefinition.uploadBehaviour)
            );
            scenesToCheckProperty = serializedObject.FindProperty(
                nameof(BackendFolderDefinition.scenesToCheck)
            );
            unisavePreferencesKeyProperty = serializedObject.FindProperty(
                nameof(BackendFolderDefinition.unisavePreferencesKey)
            );
        }
        
        public override void OnInspectorGUI()
        {
            bool triggerSave = false;
            
            // === Header & intro ===
            
            GUILayout.Label("Backend Folder Definition File", EditorStyles.largeLabel);
            
            EditorGUILayout.HelpBox(
                "This file marks the folder as a *backend folder*, meaning that " +
                "files inside will be uploaded to the server and compiled there.",
                MessageType.Info,
                wide: true
            );
            
            // === Upload behaviour ===

            int i = EditorGUILayout.Popup(
                "Upload Behaviour",
                Array.IndexOf(uploadBehaviours, definition.UploadBehaviour),
                uploadBehaviourLabels
            );
            uploadBehaviourProperty.stringValue = uploadBehaviours[i].ToString();
            
            if (definition.UploadBehaviour == UploadBehaviour.Never)
            {
                EditorGUILayout.HelpBox(
                    "This backend folder is disabled. If you want to " +
                    "use server-side resources defined here, you should allow " +
                    "its uploading.",
                    MessageType.Warning,
                    wide: true
                );
            }

            if (definition.UploadBehaviour == UploadBehaviour.CheckScenes)
            {
                EditorGUILayout.HelpBox(
                    "This backend folder is uploaded only if at least one of " +
                    "the listed scenes below is used (meaning it is loaded in " +
                    "the editor or is included in the build settings and enabled)",
                    MessageType.Info,
                    wide: true
                );
            }
            
            if (definition.UploadBehaviour == UploadBehaviour.CheckPreferences)
            {
                EditorGUILayout.HelpBox(
                    "This backend folder is uploaded only when it is listed " +
                    "in the Unisave Preferences file. You must specify the " +
                    "listing key (name) to be used, and you can toggle " +
                    "the listing using the button below.",
                    MessageType.Info,
                    wide: true
                );
            }
            
            // === Referenced scenes ===

            if (definition.UploadBehaviour == UploadBehaviour.CheckScenes)
            {
                EditorGUILayout.PropertyField(
                    scenesToCheckProperty,
                    new GUIContent("Scenes to Check")
                );
            }
            
            // === Unisave preferences key ===

            if (definition.UploadBehaviour == UploadBehaviour.CheckPreferences)
            {
                EditorGUILayout.PropertyField(
                    unisavePreferencesKeyProperty,
                    new GUIContent("Name Used in Unisave Preferences")
                );
            }
            
            // === Unisave preferences toggle button ===

            if (definition.UploadBehaviour == UploadBehaviour.CheckPreferences)
            {
                string buttonText;
                
                if (definition.IsEligibleForUpload())
                {
                    EditorGUILayout.HelpBox(
                        "This backend folder is ENABLED.",
                        MessageType.Info,
                        wide: true
                    );
                    buttonText = "Disable";
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "This backend folder is DISABLED.",
                        MessageType.Warning,
                        wide: true
                    );
                    buttonText = "Enable";
                }
                
                if (GUILayout.Button(buttonText))
                {
                    definition.ToggleBackendState();
                    triggerSave = true;
                }
            }
            
            // === Apply and save changes immediately ===
            
            if (triggerSave || serializedObject.hasModifiedProperties)
            {
                // apply changes and set dirty
                serializedObject.ApplyModifiedProperties();
                
                // save the asset to filesystem
                AssetDatabase.SaveAssetIfDirty(serializedObject.targetObject);
                
                // save preferences (may have been edited)
                var preferences = UnisavePreferences.Resolve();
                preferences.Save();
                
                // emit event that a backend definition file was changed
                BackendFolderDefinition.InvokeAnyChangeEvent();
            }
        }
    }
}