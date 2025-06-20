using Core.Util;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UI.Views
{
    [CreateAssetMenu(fileName = "MainViewBindings", menuName = "View Bindings/MainViewBindings")]
    public class MainViewBindings : ResettingScriptableObject
    {
        #region Constants

        /// <summary>
        ///     Unity defined panel background color.
        /// </summary>
        private static readonly Color PANEL_BACKGROUND_COLOR = new(
            0.647058824f,
            0.647058824f,
            0.647058824f
        );

        #endregion

        #region Side Panels

        public PickingMode SidePanelLeftPickingMode = PickingMode.Position;
        public PickingMode SidePanelRightPickingMode = PickingMode.Position;

        public Color SidePanelLeftColor = PANEL_BACKGROUND_COLOR;
        public Color SidePanelRightColor = PANEL_BACKGROUND_COLOR;

        public string SidePanelLeftToggleText = "◀";
        public string SidePanelRightToggleText = "▶";

        #endregion
    }
}
