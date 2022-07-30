using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightJson;

namespace Unisave.Editor.JsonEditor
{
    public class NullTreeViewItem : StringTreeViewItem
    {
        /// <inheritdoc/>
        public override string StringValue => "null";

        /// <inheritdoc/>
        public override Color ValueColor => Color.blue;

        /// <inheritdoc/>
        public override JsonType JsonType => JsonType.Null;

        public NullTreeViewItem(JsonTreeView treeView) : base(treeView, null) { }

        public override void EditValue()
        {
            // ignore
        }

        /// <inheritdoc/>
        public override JsonValue ToJson()
        {
            return JsonValue.Null;
        }
    }
}
