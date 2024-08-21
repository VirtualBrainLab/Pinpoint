using Core.Util;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.States
{
    [CreateAssetMenu]
    public class LeftSidePanelState : ResettingScriptableObject
    {
        #region Properties

        public bool IsVisible;

        [CreateProperty]
        public DisplayStyle VisibilityDisplayStyle =>
            IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
        
        [CreateProperty]
        public string VisibilityButtonText => IsVisible ? "<<" : ">>";

        public int ModeIndex;
        
        [CreateProperty]
        public string InspectorHeader =>
            ModeIndex switch
            {
                0 => "Probe Inspector",
                1 => "Manipulator Inspector",
                _ => "Automation Pipeline"
            };
        
        [CreateProperty]
        public DisplayStyle InspectorStackDisplayStyle => ModeIndex != 2 ? DisplayStyle.Flex : DisplayStyle.None;
        
        [CreateProperty]
        public DisplayStyle AutomationStackDisplayStyle => InspectorStackDisplayStyle == DisplayStyle.Flex ? DisplayStyle.None : DisplayStyle.Flex;

        #endregion
    }
}
