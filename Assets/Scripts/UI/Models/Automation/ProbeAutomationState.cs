using System;
using UI.Utils;
using UnityEngine;

namespace UI.Models.Automation
{
    [Serializable]
    public record ProbeAutomationState
    {
        [SerializeField]
        public ProbeAutomationProgress Progress;
    }
}