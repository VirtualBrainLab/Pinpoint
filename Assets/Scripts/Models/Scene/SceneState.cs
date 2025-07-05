using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Models.Scene
{
    [Serializable]
    public record SceneState
    {
        public List<ProbeState> Probes = new();

        [NonSerialized]
        public string ActiveProbeUUID;

        // Helper to get the active probe state based on the ActiveProbeUUID.
        public ProbeState ActiveProbeState =>
            Probes.FirstOrDefault(state => state.UUID == ActiveProbeUUID);
        
        // Helper to get the active probe index.
        public int ActiveProbeIndex => Probes.IndexOf(ActiveProbeState);

        public int TotalProbeCount = 0;

        public bool ShowAllProbePanels = true;

        public float ProbePanelHeight = 1440f;
    }
}
