using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using LightJson;
using LightJson.Serialization;

namespace Unisave.Editor.JsonEditor
{
    public class JsonEditor
    {
        TreeViewState treeViewState;
        JsonTreeView jsonTreeView;

        /// <summary>
        /// When the json data is changed by the user
        /// </summary>
        public event Action OnChange;

        /// <summary>
        /// Are we in the text mode? (editing the JSON source code)
        /// </summary>
        public bool InTextMode { get; private set; }

        // the text edited in the text mode
        private string editedText;

        // font used for the text mode
        private Font fontCache = null;
        private Font MonospaceFont
        {
            get
            {
                if (fontCache == null)
                    fontCache = Resources.Load<Font>("FiraCode-Regular");
                
                return fontCache;
            }
        }

        public JsonEditor()
        {
            treeViewState = new TreeViewState();
            jsonTreeView = new JsonTreeView(treeViewState);
            jsonTreeView.ExpandAll();

            jsonTreeView.OnChange += () => {
                OnChange?.Invoke();
            };
        }

        /// <summary>
        /// Returns the value that's currently inside the editor
        /// </summary>
        public JsonObject GetValue()
        {
            if (InTextMode)
            {
                ExitTextMode();
                JsonObject v = jsonTreeView.GetValue();
                EnterTextMode();
                return v;
            }
            else
            {
                return jsonTreeView.GetValue();
            }
        }

        /// <summary>
        /// Sets the value inside the editor
        /// </summary>
        public void SetValue(JsonObject value)
        {
            if (InTextMode)
            {
                editedText = value.ToString(true).Replace("\t", "    ");
            }
            else
            {
                jsonTreeView.SetValue(value);
            }
        }

        public void EnterTextMode()
        {
            if (InTextMode)
                return;

            editedText = jsonTreeView.GetValue().ToString(true).Replace("\t", "    ");
            InTextMode = true;
        }

        public void ExitTextMode()
        {
            if (!InTextMode)
                return;

            try
            {
                JsonValue v = JsonReader.Parse(editedText);

                if (!v.IsJsonObject)
                {
                    Debug.LogError("This is not a JSON object, but some other JSON type.");
                    return; // abort text mode exitting
                }

                jsonTreeView.SetValue(v.AsJsonObject);
                
                InTextMode = false;
            }
            catch (JsonParseException e)
            {
                Debug.LogException(e);
            }
        }

        public void OnGUI()
        {
            // outer container
            GUIStyle style = EditorStyles.helpBox;
            style.margin.left = 5;
            style.margin.right = 5;
            Rect container = EditorGUILayout.BeginVertical(style);
            {
                // toolbar
                DoToolbarGUI();

                if (!InTextMode)
                {
                    HandleKeyboardShortcuts();

                    // tree view
                    Rect treeViewRect = EditorGUILayout.BeginVertical();
                    {
                        GUILayout.Space(jsonTreeView.GetHeight());

                        // gets drawn over the space
                        jsonTreeView.OnGUI(treeViewRect);
                    }
                    EditorGUILayout.EndVertical();

                    // type toolbar
                    DoTypeToolbarGUI();
                }
                else
                {
                    var textStyle = EditorStyles.textArea;
                    textStyle.font = MonospaceFont;
                    textStyle.fontSize = 14;
                    editedText = EditorGUILayout.TextArea(editedText, textStyle);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void HandleKeyboardShortcuts()
        {
            // listen only for keydown
            if (Event.current.type != EventType.KeyDown)
                return;

            // delete
            if (Event.current.keyCode == KeyCode.Delete)
                jsonTreeView.activeItem?.RemoveField();
        }

        private void DoToolbarGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Remove", EditorStyles.toolbarButton))
                jsonTreeView.activeItem?.RemoveField();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Help", EditorStyles.toolbarButton))
                DisplayHelp();

            if (GUILayout.Toggle(InTextMode, "Source", EditorStyles.toolbarButton) != InTextMode)
            {
                if (InTextMode)
                    ExitTextMode();
                else
                    EnterTextMode();
            }

            GUILayout.EndHorizontal();
        }

        private void DoTypeToolbarGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            DisplaySingleTypeToggle(JsonType.Null, "Null");
            DisplaySingleTypeToggle(JsonType.Boolean, "Bool");
            DisplaySingleTypeToggle(JsonType.Number, "Num");
            DisplaySingleTypeToggle(JsonType.String, "Str");
            DisplaySingleTypeToggle(JsonType.Array, "Arr");
            DisplaySingleTypeToggle(JsonType.Object, "Obj");

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Displays the help dialog
        /// </summary>
        private void DisplayHelp()
        {
            EditorUtility.DisplayDialog(
                "JSON editor help",
                "- Double-click text to change values and keys\n" +
                "- Double-click 'add +' to add new items\n" +
                "- Hit [delete] on your keyboard to remove items\n" +
                "- Change value type by clicking the lower toolbar options\n" +
                "- Drag and drop items 😱\n" +
                "- Hit 'Source' to edit the JSON directly",
                "OK"
            );
        }

        private void DisplaySingleTypeToggle(JsonType type, string text)
        {
            // no active item
            if (jsonTreeView.activeItem == null)
            {
                GUILayout.Toggle(false, text, EditorStyles.toolbarButton);
                return;
            }

            // active item has this type (cannot click)
            if (jsonTreeView.activeItem.JsonType == type)
            {
                GUILayout.Toggle(true, text, EditorStyles.toolbarButton);
                return;
            }

            // can click
            if (GUILayout.Toggle(false, text, EditorStyles.toolbarButton))
            {
                // change the item type:

                var newItem = JsonTreeViewUtils.CreateJsonValueItem(
                    jsonTreeView.activeItem.Key,
                    jsonTreeView.activeItem.Index,
                    JsonTreeViewUtils.DefaultValueForType(type),
                    jsonTreeView
                );

                if (jsonTreeView.activeItem.parent is ObjectTreeViewItem)
                    ((ObjectTreeViewItem)jsonTreeView.activeItem.parent).ReplaceField(
                        jsonTreeView.activeItem.Key,
                        newItem
                    );

                if (jsonTreeView.activeItem.parent is ArrayTreeViewItem)
                    ((ArrayTreeViewItem)jsonTreeView.activeItem.parent).ReplaceItem(
                        jsonTreeView.activeItem.Index,
                        newItem
                    );
            }
        }
    }
}
