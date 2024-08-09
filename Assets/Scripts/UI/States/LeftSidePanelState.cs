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
        public DisplayStyle VisibilityDisplayStyle => IsVisible ? DisplayStyle.Flex : DisplayStyle.None;

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

            // Register converter groups.
            ConverterGroups.RegisterConverterGroup(visibilityGroup);
        }

        #endregion
    }
}