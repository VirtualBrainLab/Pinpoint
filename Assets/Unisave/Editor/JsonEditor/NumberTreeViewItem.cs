using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightJson;

namespace Unisave.Editor.JsonEditor
{
    public class NumberTreeViewItem : StringTreeViewItem
    {
        /// <summary>
        /// Number value of this item
        /// </summary>
        public double NumberValue
        {
            get => numberValue;
            
            set
            {
                numberValue = value;
                StringValue = value.ToString().ToLower();
            }
        }
        private double numberValue;

        /// <inheritdoc/>
        public override Color ValueColor => Color.blue;

        /// <inheritdoc/>
        public override JsonType JsonType => JsonType.Number;

        public NumberTreeViewItem(JsonTreeView treeView, double initialValue) : base(treeView, null)
        {
            NumberValue = initialValue;
        }

        /// <inheritdoc/>
        public override void RenameEnded(bool accepted, string newValue)
        {
            base.RenameEnded(accepted, newValue);

            if (!IsKeyEdited && accepted && newValue != displayName)
            {
                NumberValue = NumberifyValue(newValue);
            }
        }

        private double NumberifyValue(string text)
        {
            if (double.TryParse(text, out double result))
            {
                if (double.IsNaN(result) || double.IsInfinity(result))
                    return 0.0;

                return result;
            }
            else
            {
                return 0.0;
            }
        }

        /// <inheritdoc/>
        public override JsonValue ToJson()
        {
            return NumberValue;
        }
    }
}
