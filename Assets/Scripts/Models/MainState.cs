using System;
using UI.Utils;
using UnityEngine;

namespace Models
{
    [Serializable]
    public record MainState
    {
        public bool IsAutomationModeActive;

        public bool IsLeftSidePanelOpen = true;

        public bool IsRightSidePanelOpen = true;

        public int LeftSidePanelTabIndex;
    }
}
