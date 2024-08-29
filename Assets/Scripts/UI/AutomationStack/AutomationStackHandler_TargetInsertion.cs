using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.AutomationStack
{
    /// <summary>
    ///     Handles picking and driving to a target insertion in the Automation Stack.<br />
    /// </summary>
    public partial class AutomationStackHandler : MonoBehaviour
    {
        #region Properties

        private IEnumerable<string> targetInsertionOptionsCache = Enumerable.Empty<string>();

        #endregion

        #region Implementations

        private partial void UpdateTargetInsertionOptionsRadioButtonColors()
        {
            // Shortcut exit if the target insertion options have not changed.
            if (targetInsertionOptionsCache.SequenceEqual(_targetInsertionRadioButtonGroup.choices))
                return;

            // Update the target insertion options cache.
            targetInsertionOptionsCache = _targetInsertionRadioButtonGroup.choices;

            // Loop through each child in the target insertion radio button group (skipping first option, "None").
            for (var i = 1; i < _targetInsertionRadioButtonGroup.contentContainer.childCount; i++)
            {
                // Get button visual element.
                var buttonVisualElement = _targetInsertionRadioButtonGroup
                    .contentContainer[i]
                    .Q("unity-checkmark")
                    .parent;

                // Set the color of the button visual element.
                buttonVisualElement.style.backgroundColor = _state
                    .SurfaceCoordinateStringToTargetInsertionOptionProbeManagers[
                        _state.TargetInsertionOptions.ElementAt(i)
                    ]
                    .Color;
            }
        }

        #endregion
    }
}
