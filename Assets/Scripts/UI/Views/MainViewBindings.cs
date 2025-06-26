using Core.Util;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UI.Views
{
    [CreateAssetMenu(fileName = "MainViewBindings", menuName = "View Bindings/MainViewBindings")]
    public class MainViewBindings : ResettingScriptableObject
    {

        #region Side Panels

        public PickingMode SidePanelLeftPickingMode = PickingMode.Position;
        public PickingMode SidePanelRightPickingMode = PickingMode.Position;

        public string SidePanelLeftToggleText = "◀";
        public string SidePanelRightToggleText = "▶";
        
        public DisplayStyle SidePanelLeftItemDisplayStyle = DisplayStyle.Flex;
        
        #endregion
    }
}
