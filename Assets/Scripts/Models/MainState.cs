using System;
using Unity.AppUI.UI;

namespace Models
{
    [Serializable]
    public record MainState
    {
        public bool IsAutomationEnabled;
        
        public SplitView.State MainSplitViewState;

        public int LeftSidePanelTabIndex;
    }
}
