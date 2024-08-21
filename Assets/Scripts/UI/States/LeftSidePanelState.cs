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

        public int ModeIndex;
        
        [CreateProperty]
        public DisplayStyle InspectorStackDisplayStyle => ModeIndex != 2 ? DisplayStyle.Flex : DisplayStyle.None;
        
        [CreateProperty]
        public DisplayStyle AutomationStackDisplayStyle => InspectorStackDisplayStyle == DisplayStyle.Flex ? DisplayStyle.None : DisplayStyle.Flex;

        #endregion

        #region Converters

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        public static void RegisterConverters()
        {
            // Visibility boolean.
            var visibilityGroup = new ConverterGroup("Visibility Boolean to Hide Button Text");
            visibilityGroup.AddConverter((ref bool isVisible) => isVisible ? "<<" : ">>");

            // Mode index.
            var modeIndexGroup = new ConverterGroup("Mode Index to Right Side Panel Header Text");
            modeIndexGroup.AddConverter(
                (ref int modeIndex) =>
                    modeIndex switch
                    {
                        0 => "Probe Inspector",
                        1 => "Manipulator Inspector",
                        _ => "Automation Pipeline"
                    }
            );

            // Register converter groups.
            ConverterGroups.RegisterConverterGroup(visibilityGroup);
            ConverterGroups.RegisterConverterGroup(modeIndexGroup);
        }

        #endregion
    }
}
