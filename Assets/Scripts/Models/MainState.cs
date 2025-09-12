using System;
using Unity.AppUI.UI;
using UnityEngine;

namespace Models
{
    [Serializable]
    public record MainState
    {
        public SplitView.State MainSplitViewState;

        public int LeftSidePanelTabIndex;
    }
}
