using Core.Util;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.States
{
    [CreateAssetMenu]
    public class RightSidePanelState : ResettingScriptableObject
    {
        #region Panel

        public bool IsVisible;

        [CreateProperty]
        public DisplayStyle VisibilityDisplayStyle =>
            IsVisible ? DisplayStyle.Flex : DisplayStyle.None;

        [CreateProperty]
        public string VisibilityButtonText => IsVisible ? ">>" : "<<";

        [CreateProperty]
        public Color PanelBackgroundColor => IsVisible ? new Color(0.647058824f, 0.647058824f, 0.647058824f) : Color.clear;

        #endregion

        #region Current Probe Label

        [CreateProperty]
        public Color ProbeColor =>
            ProbeManager.ActiveProbeManager ? ProbeManager.ActiveProbeManager.Color : Color.black;

        [CreateProperty]
        public string ProbeName =>
            ProbeManager.ActiveProbeManager
                ? ProbeManager.ActiveProbeManager.name
                : "No Probe Selected";

        #endregion
    }
}
