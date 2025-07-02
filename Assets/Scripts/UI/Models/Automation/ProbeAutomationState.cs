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

        // TODO: This should be a probe data model object when that is implemented.
        [SerializeField]
        public ProbeManager SelectedTargetInsertionProbeManager;
    }
}
