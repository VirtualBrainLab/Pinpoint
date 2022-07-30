using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using LightJson;

namespace Unisave.Editor.JsonEditor
{
    /// <summary>
    /// Displays a string value but is also a baseclass for all other types
    /// </summary>
    public class StringTreeViewItem : TreeViewItem
    {
        /// <summary>
        /// Key if located inside an object
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Index when located inside an array
        /// </summary>
        /// <value></value>
        public int Index { get; set; }

        /// <summary>
        /// Value of this string primitive
        /// </summary>
        public virtual string StringValue { get; set; }

        /// <summary>
        /// Is this value inside a json object?
        /// </summary>
        public bool InsideObject => parent is ObjectTreeViewItem;

        /// <summary>
        /// Properly formatted key or index
        /// </summary>
        public virtual string Label => InsideObject ? (Key + ": ") : ("[" + Index + "] ");

        /// <summary>
        /// Are we renaming key or value?
        /// </summary>
        public bool RenamingValue { get; private set; }

        /// <summary>
        /// Was the mouse last seen over the label or not?
        /// </summary>
        public bool MouseOverLabel { get; private set; }

        /// <summary>
        /// Is the key currently being edited?
        /// </summary>
        public bool IsKeyEdited { get; private set; }

        /// <summary>
        /// Is the value currently being edited?
        /// </summary>
        /// <value></value>
        public bool IsValueEdited { get; private set; }

        /// <summary>
        /// Color of the value text
        /// </summary>
        public virtual Color ValueColor => Color.red;

        /// <summary>
        /// The type this item represents
        /// </summary>
        public virtual JsonType JsonType => JsonType.String;

        /// <summary>
        /// Reference to the parent tree view
        /// </summary>
        protected JsonTreeView treeView;

        public StringTreeViewItem(JsonTreeView treeView, string initialValue) : base()
        {
            this.treeView = treeView;
            this.StringValue = initialValue;
            id = treeView.NextId();
        }

        /// <summary>
        /// Draw the tree view item
        /// </summary>
        /// <param name="itemRect">Indented rectangle</param>
        public void ItemGUI(Rect itemRect)
        {
            float labelWidth = CalculateLabelWidth();
            Rect labelRect = new Rect(itemRect.x, itemRect.y, labelWidth, itemRect.height);

            MouseOverLabel = labelRect.Contains(
                Event.current.mousePosition
            );

            if (!IsKeyEdited)
            {
                GUI.Label(
                    labelRect,
                    Label,
                    GUIStyle.none
                );
            }

            if (!IsValueEdited)
            {
                GUI.Label(
                    new Rect(itemRect.x + labelWidth, itemRect.y, itemRect.width - labelWidth, itemRect.height),
                    StringValue,
                    new GUIStyle { normal = new GUIStyleState { textColor = ValueColor } }
                );
            }
        }

        /// <summary>
        /// Calculated GUI width of the label
        /// </summary>
        public float CalculateLabelWidth()
            => IsKeyEdited
            ? 50f
            : GUIStyle.none.CalcSize(new GUIContent(Label)).x;

        /// <summary>
        /// Called by the tree view on duble click
        /// </summary>
        public virtual void DoubleClickedItem()
        {
            if (MouseOverLabel)
                EditKey();
            else
                EditValue();
        }

        public virtual void EditValue()
        {
            RenamingValue = true;
            displayName = StringValue;
            treeView.BeginRename(this);
            IsValueEdited = true;
        }

        public virtual void EditKey()
        {
            if (!InsideObject)
                return;

            RenamingValue = false;
            displayName = Key;
            treeView.BeginRename(this);
            IsKeyEdited = true;
        }

        /// <summary>
        /// Called by the tree view to obtain renaming textbox position
        /// </summary>
        /// <param name="rowRect">Indented row rectangle</param>
        public Rect GetRenameRect(Rect rowRect)
        {
            float labelWidth = CalculateLabelWidth();

            if (RenamingValue)
            {
                rowRect.x += labelWidth;
                rowRect.width -= labelWidth;
                return rowRect;
            }
            else
            {
                rowRect.width = labelWidth;
                return rowRect;
            }
        }

        /// <summary>
        /// Called by the tree view
        /// </summary>
        /// <param name="accepted">Has the new value been accepted?</param>
        /// <param name="newValue">The new value</param>
        public virtual void RenameEnded(bool accepted, string newValue)
        {
            IsKeyEdited = false;
            IsValueEdited = false;

            if (!accepted)
                return;

            // no change made
            if (displayName == newValue)
                return;

            if (RenamingValue)
            {
                StringValue = newValue;
            }
            else
            {
                if (((ObjectTreeViewItem)parent).ContainsKey(newValue))
                {
                    EditorUtility.DisplayDialog(
                        "Key taken",
                        $"Provided key '{newValue}' already exists inside the JSON object.",
                        "OK"
                    );
                }
                else
                {
                    Key = newValue;
                }
            }
        }

        /// <summary>
        /// Tries to remove this item from the JSON structure
        /// (whatever that means in the given contenxt)
        /// </summary>
        public virtual void RemoveField()
        {
            if (parent == null)
                return;

            if (parent is ObjectTreeViewItem)
                ((ObjectTreeViewItem)parent).RemoveKey(Key);

            if (parent is ArrayTreeViewItem)
                ((ArrayTreeViewItem)parent).RemoveAt(Index);

            if (treeView.activeItem == this)
                treeView.SelectItem(null);
        }

        /// <summary>
        /// Drop an item at a given child index
        /// </summary>
        public virtual void DropItemAt(int index, StringTreeViewItem item)
        {
            // overriden by containers
        }

        /// <summary>
        /// Converts the tree view item into it's underlying JSON form
        /// </summary>
        public virtual JsonValue ToJson()
        {
            return StringValue;
        }
    }
}
