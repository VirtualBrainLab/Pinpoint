using System;
using UI.Utils;
using UnityEngine;

namespace UI.Models
{
    [Serializable]
    public record MainState
    {
        [SerializeField]
        public MainModes Mode;

        [SerializeField]
        public bool IsSidePanelLeftOpen = true;

        [SerializeField]
        public bool IsSidePanelRightOpen = true;
    }
}
