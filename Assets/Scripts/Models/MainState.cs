using System;
using UI.Utils;
using UnityEngine;

namespace Models
{
    [Serializable]
    public record MainState
    {
        public MainMode MainMode;

        public bool IsLeftSidePanelOpen = true;

        public bool IsRightSidePanelOpen = true;

        public bool IsInputFocused;
    }

    public enum MainMode
    {
        Planning,
        Visualization,
        Automation,
    }
}
