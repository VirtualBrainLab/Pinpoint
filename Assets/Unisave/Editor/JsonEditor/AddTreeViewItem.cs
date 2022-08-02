using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unisave.Editor.JsonEditor
{
    /// <summary>
    /// When double clicked, new field is added to the container object or array
    /// </summary>
    public class AddTreeViewItem : StringTreeViewItem
    {
        /// <inheritdoc/>
        public override string Label => "";

        /// <inheritdoc/>
        public override string StringValue => "add +";

        /// <inheritdoc/>
        public override Color ValueColor => new Color(0.26f, 0.26f, 0.26f, 1f);

        /// <inheritdoc/>
        public override JsonType JsonType => JsonType.NotJson;

        public AddTreeViewItem(JsonTreeView treeView) : base(treeView, null) { }

        /// <inheritdoc/>
        public override void DoubleClickedItem()
        {
            if (parent is ObjectTreeViewItem)
                ((ObjectTreeViewItem)parent).AddNewField();

            if (parent is ArrayTreeViewItem)
                ((ArrayTreeViewItem)parent).AddNewField();
        }

        public override void EditKey()
        {
            // ignore
        }

        public override void EditValue()
        {
            // ignore
        }

        /// <inheritdoc/>
        public override void RemoveField()
        {
            // nope, nope
        }
    }
}
