using System;
using UI.Utils;
using UnityEngine;

namespace Models.Scene
{
    [Serializable]
    public record ProbeState
    {
        // Automation.

        [SerializeField] public ProbeAutomationProgress Progress;

        [SerializeField]
        public Vector4 ReferenceCoordinateOffset;
        
        // TODO: migrate to ProbeState once the appropriate fields have been implemented.
        [SerializeField]
        public ProbeManager SelectedTargetInsertionProbeState;
    }
}
