using Core.Util;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.States
{
    [CreateAssetMenu]
    public class RightSidePanelState : ResettingScriptableObject
    {
        #region Properties

        public bool IsVisible;

        [CreateProperty]
        public DisplayStyle VisibilityDisplayStyle =>
            IsVisible ? DisplayStyle.Flex : DisplayStyle.None;

        [CreateProperty]
        public string VisibilityButtonText => IsVisible ? ">>" : "<<";

        #endregion
    }
}
