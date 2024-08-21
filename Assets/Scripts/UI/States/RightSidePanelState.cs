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

        [CreateProperty]
        public Color ProbeColor => ProbeManager.ActiveProbeManager ? ProbeManager.ActiveProbeManager.Color : Color.black;
        
        [CreateProperty]
        public string ProbeName => ProbeManager.ActiveProbeManager ? ProbeManager.ActiveProbeManager.name : "No Probe Selected";

        #endregion
    }
}
