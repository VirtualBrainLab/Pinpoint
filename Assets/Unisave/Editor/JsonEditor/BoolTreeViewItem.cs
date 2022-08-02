using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightJson;

namespace Unisave.Editor.JsonEditor
{
    public class BoolTreeViewItem : StringTreeViewItem
    {
        /// <summary>
        /// Boolean value of this item
        /// </summary>
        public bool BoolValue
        {
            get => boolValue;
            
            set
            {
                boolValue = value;
                StringValue = value.ToString().ToLower();
            }
        }
        private bool boolValue;

        /// <inheritdoc/>
        public override Color ValueColor => Color.blue;

        /// <inheritdoc/>
        public override JsonType JsonType => JsonType.Boolean;

        public BoolTreeViewItem(JsonTreeView treeView, bool initialValue) : base(treeView, null)
        {
            BoolValue = initialValue;
        }

        public override void EditValue()
        {
            // instead of showing a textbox, toggle the value
            BoolValue = !BoolValue;
        }

        /// <inheritdoc/>
        public override JsonValue ToJson()
        {
            return BoolValue;
        }
    }
}
