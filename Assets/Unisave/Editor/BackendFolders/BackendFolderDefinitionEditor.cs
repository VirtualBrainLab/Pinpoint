using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unisave.Editor.BackendFolders
{
    [CustomEditor(typeof(BackendFolderDefinition))]
    public class BackendFolderDefinitionEditor : UnityEditor.Editor
    {
        private BackendFolderDefinition definition;

        private readonly UploadBehaviour[] uploadBehaviours = new[] {
            UploadBehaviour.Never,
            UploadBehaviour.Always,
            UploadBehaviour.CheckScenes
        };

        private readonly string[] uploadBehaviourLabels = new[] {
            "Never", "Always", "When Scene Is Used"
        };

        private SerializedProperty uploadBehaviourProperty;
        private SerializedProperty scenesToCheckProperty;

        public void OnEnable()
        {
            definition = (BackendFolderDefinition) target;

            uploadBehaviourProperty = serializedObject.FindProperty(
                nameof(BackendFolderDefinition.uploadBehaviour)
            );
            scenesToCheckProperty = serializedObject.FindProperty(
                nameof(BackendFolderDefinition.scenesToCheck)
            );
        }
        
        public override void OnInspectorGUI()
        {
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
            
            // === Referenced scenes ===

            if (definition.UploadBehaviour == UploadBehaviour.CheckScenes)
            {
                EditorGUILayout.PropertyField(
                    scenesToCheckProperty,
                    new GUIContent("Scenes to Check")
                );
            }
            
            // === Apply changes and set dirty ===
            bool hadModifiedProperties = serializedObject.hasModifiedProperties;
            serializedObject.ApplyModifiedProperties();

            // we use serializedObject, and it does this automatically
            // EditorUtility.SetDirty(definition);
            
            // === Emit event that a backend definition file was changed ===
            
            if (hadModifiedProperties)
                BackendFolderDefinition.InvokeAnyChangeEvent();
        }
    }
}