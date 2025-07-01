using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Models.Automation
{
    [Serializable]
    public record AutomationState
    {
        [SerializeField]
        public List<ProbeAutomationState> Probes = new();

        [SerializeField]
        public int ActiveProbeIndex = -1;
    }
}
