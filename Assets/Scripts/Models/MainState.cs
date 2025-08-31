using System;
using Unity.AppUI.UI;
using UnityEngine;

namespace Models
{
    [Serializable]
    public record MainState
    {
        public bool IsAutomationModeActive;

        public SplitView.State MainSplitViewState;

        public int LeftSidePanelTabIndex;
    }
}
