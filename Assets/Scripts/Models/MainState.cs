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

        public int LeftSidePanelTabIndex;
    }

    public enum MainMode
    {
        Planning,
        Automation,
    }
}
