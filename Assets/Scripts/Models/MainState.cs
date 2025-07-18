using System;

namespace Models
{
    [Serializable]
    public record MainState
    {
        public bool IsAutomationModeActive;

        public int LeftSidePanelTabIndex;
    }
}
