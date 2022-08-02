using System;
using UnityEditor;
using UnityEngine;

namespace Unisave.Editor
{
    public static class UnisaveEditorHelper
    {
        /// <summary>
        /// Renders heading for a custom inspector
        /// </summary>
        public static void InspectorHeading(string text, Texture icon)
        {
            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = (int)EditorGUIUtility.singleLineHeight;
            style.padding = new RectOffset(10, 10, 10, 10);
            GUILayout.Box(new GUIContent("  " + text, icon), style);
        }

        /// <summary>
        /// Renders a labeled box with some content
        /// </summary>
        public static void LabeledBox(string label, Action content)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label(label, EditorStyles.boldLabel);
                content?.Invoke();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Box for creating objects by one string parameter
        /// </summary>
        public static void StringCreationBox(
            string title,
            string fieldLabel,
            string buttonText,
            ref string fieldValue,
            string errorMessage,
            Action submit
        )
        {
            string fv = fieldValue;
            
            LabeledBox(title, () => {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    fieldLabel,
                    GUILayout.Width(EditorGUIUtility.labelWidth - 4)
                );
                fv = EditorGUILayout.TextField(fv);
                if (GUILayout.Button(buttonText))
                    submit?.Invoke();
                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    GUIStyle style = new GUIStyle(EditorStyles.label);
                    style.normal.textColor = Color.red;
                    GUILayout.Label(errorMessage, style);
                }
            });

            fieldValue = fv;
        }
        
        /// <summary>
        /// Renders a read-only field with some label and some text content
        /// </summary>
        public static void ReadOnlyField(string label, string content)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                label,
                GUILayout.Width(EditorGUIUtility.labelWidth - 4)
            );
            EditorGUILayout.SelectableLabel(
                content,
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight)
            );
            EditorGUILayout.EndHorizontal();
        }
    }
}