using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using LightJson;
using System.Linq;

namespace Unisave.Editor.JsonEditor
{
    public class ArrayTreeViewItem : StringTreeViewItem
    {
        /// <inheritdoc/>
        public override string StringValue => "";

        /// <inheritdoc/>
        public override JsonType JsonType => JsonType.Array;

        public ArrayTreeViewItem(JsonTreeView treeView, JsonArray arrayValue) : base(treeView, null)
        {
            for (int i = 0; i < arrayValue.Count; i++)
                AddChild(JsonTreeViewUtils.CreateJsonValueItem(null, i, arrayValue[i], treeView));

            RepairChildStructure();
        }

        /// <summary>
        /// Fixes positions of special children and indices of regular children
        /// </summary>
        protected void RepairChildStructure()
        {
            if (children == null)
            {
                AddChild(new AddTreeViewItem(treeView));
                return;
            }

            // fix "add" button
            children.RemoveAll(child => child is AddTreeViewItem);
            AddChild(new AddTreeViewItem(treeView));

            // fix indices
            int index = 0;
            foreach (var child in children)
            {
                if (!(child is StringTreeViewItem))
                    continue;

                ((StringTreeViewItem)child).Index = index;
                index++;
            }
        }

        public override void EditValue()
        {
            // ignore
        }

        /// <summary>
        /// Removes a child at index
        /// </summary>
        public void RemoveAt(int index)
        {
            if (children == null)
                return;

            children.RemoveAll(child => (child as StringTreeViewItem)?.Index == index);
            RepairChildStructure();
            treeView.Reload();
        }

        /// <inheritdoc/>
        public void AddNewField()
        {
            var newChild = new NullTreeViewItem(treeView) {
                Index = children.Count
            };
            AddChild(newChild);
            RepairChildStructure();
            treeView.Reload();
            treeView.SelectItem(newChild);
        }

        /// <summary>
        /// Replace an existing item with a new one
        /// </summary>
        /// <param name="index">Where to do the replacement</param>
        /// <param name="newItem">The new item</param>
        public void ReplaceItem(int childIndex, StringTreeViewItem newItem)
        {
            if (children == null)
                return;

            // find location of the item with given index
            // (should be the same, but just to make sure...)
            int index = children.FindIndex(child => ((StringTreeViewItem)child).Index == childIndex);

            if (index == -1)
                throw new ArgumentException("Provided index does not exist inside the array.");

            children[index] = newItem;
            newItem.parent = this;
            newItem.Index = index;

            // NOTE: no child repair needed here

            treeView.Reload();
            treeView.SelectItem(newItem);
            treeView.ExpandItem(newItem);
        }

        /// <inheritdoc/>
        public override void DropItemAt(int index, StringTreeViewItem item)
        {
            children.Insert(index, item);
            item.parent = this;
            item.Key = null;

            RepairChildStructure();

            treeView.Reload();
            treeView.SelectItem(item);
        }

        /// <inheritdoc/>
        public override JsonValue ToJson()
        {
            var items = children
                .Where(child => ((StringTreeViewItem)child).JsonType != JsonType.NotJson)
                .Select(child => ((StringTreeViewItem)child).ToJson());

            return new JsonArray(items.ToArray());
        }
    }
}
