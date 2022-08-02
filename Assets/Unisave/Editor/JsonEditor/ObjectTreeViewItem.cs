using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using LightJson;
using System.Linq;

namespace Unisave.Editor.JsonEditor
{
    public class ObjectTreeViewItem : StringTreeViewItem
    {
        /// <inheritdoc/>
        public override string StringValue => "";

        /// <inheritdoc/>
        public override JsonType JsonType => JsonType.Object;

        public ObjectTreeViewItem(JsonTreeView treeView, JsonObject objectValue) : base(treeView, null)
        {
            foreach (var pair in objectValue)
                AddChild(JsonTreeViewUtils.CreateJsonValueItem(pair.Key, 0, pair.Value, treeView));

            RepairChildStructure();
        }

        /// <summary>
        /// Fixes positions of special children
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
        }

        public override void EditValue()
        {
            // ignore
        }

        /// <summary>
        /// Does this object contain a given key?
        /// </summary>
        public bool ContainsKey(string key)
        {
            if (children == null)
                return false;

            foreach (var child in children)
            {
                if (child is StringTreeViewItem && ((StringTreeViewItem)child).Key == key)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a child with given key
        /// </summary>
        public void RemoveKey(string key)
        {
            if (children == null)
                return;

            children.RemoveAll(child => (child as StringTreeViewItem)?.Key == key);
            RepairChildStructure();
            treeView.Reload();
        }

        /// <summary>
        /// Generates unique key for a new field
        /// </summary>
        public string GenerateNewKey()
        {
            for (int i = 1; i < 10_000; i++)
            {
                var key = "New Field " + i;
                
                if (!ContainsKey(key))
                    return key;
            }

            throw new Exception("Key generation ran into the failsafe iteration count.");
        }

        /// <inheritdoc/>
        public void AddNewField()
        {
            var newChild = new NullTreeViewItem(treeView) {
                Key = GenerateNewKey()
            };
            AddChild(newChild);
            RepairChildStructure();
            treeView.Reload();
            treeView.SelectItem(newChild);
        }

        /// <summary>
        /// Replace an existing field with a new one
        /// </summary>
        /// <param name="key">Key of the field</param>
        /// <param name="newItem">The new item to put here</param>
        public void ReplaceField(string key, StringTreeViewItem newItem)
        {
            if (children == null)
                return;

            int index = children.FindIndex(child => (child as StringTreeViewItem)?.Key == key);

            if (index == -1)
                throw new ArgumentException("Provided key does not exist inside the object.");

            children[index] = newItem;
            newItem.parent = this;
            newItem.Key = key;

            // NOTE: no child repair needed here

            treeView.Reload();
            treeView.SelectItem(newItem);
            treeView.ExpandItem(newItem);
        }

        /// <inheritdoc/>
        public override void DropItemAt(int index, StringTreeViewItem item)
        {
            if (item.Key == null || ContainsKey(item.Key))
                item.Key = GenerateNewKey();

            children.Insert(index, item);
            item.parent = this;

            RepairChildStructure();

            treeView.Reload();
            treeView.SelectItem(item);
        }

        /// <inheritdoc/>
        public override JsonValue ToJson()
        {
            var ret = new JsonObject();

            var fields = children
                .Where(child => ((StringTreeViewItem)child).JsonType != JsonType.NotJson)
                .Select(child => (StringTreeViewItem)child);

            foreach (var field in fields)
                ret.Add(field.Key, field.ToJson());

            return ret;
        }
    }
}
