using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using LightJson;

namespace Unisave.Editor.JsonEditor
{
    public static class JsonTreeViewUtils
    {
        /// <summary>
        /// Creates a proper item for a tree view
        /// </summary>
        /// <param name="key">Key if inside an object</param>
        /// <param name="index">Index if inside an array</param>
        /// <param name="value">JSON value this item represents</param>
        /// <param name="treeView">Tree view reference</param>
        public static StringTreeViewItem CreateJsonValueItem(
            string key, int index, JsonValue value, JsonTreeView treeView
        )
        {
            if (value.IsJsonObject)
                return new ObjectTreeViewItem(treeView, value.AsJsonObject) {
                    Key = key,
                    Index = index
                };
            else if (value.IsJsonArray)
                return new ArrayTreeViewItem(treeView, value.AsJsonArray) {
                    Key = key,
                    Index = index
                };
            else if (value.IsNull)
                return new NullTreeViewItem(treeView) {
                    Key = key,
                    Index = index
                };
            else if (value.IsBoolean)
                return new BoolTreeViewItem(treeView, value.AsBoolean) {
                    Key = key,
                    Index = index
                };
            else if (value.IsNumber)
                return new NumberTreeViewItem(treeView, value.AsNumber) {
                    Key = key,
                    Index = index
                };
            else
                return new StringTreeViewItem(treeView, value.AsString) {
                    Key = key,
                    Index = index
                };
        }

        /// <summary>
        /// Returns default value for a given JSON type
        /// </summary>
        public static JsonValue DefaultValueForType(JsonType type)
        {
            switch (type)
            {
                case JsonType.Null: return JsonValue.Null;
                case JsonType.String: return "";
                case JsonType.Number: return 0;
                case JsonType.Boolean: return false;
                case JsonType.Array: return new JsonArray();
                case JsonType.Object: return new JsonObject();
            }

            throw new Exception("Unknown JSON type: " + type.ToString());
        }
    }
}
