using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Models.Automation
{
    [Serializable]
    public record AutomationState
    {
        [SerializeField]
        public HashSet<ProbeAutomationState> Probes;
    }
}
