using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Models.Scene
{
    [Serializable]
    public record SceneState
    {
        public List<ProbeState> Probes = new List<ProbeState>();

        public string ActiveProbeUUID;

        // Helper to get the active probe state based on the ActiveProbeUUID.
        public ProbeState ActiveProbeState =>
            Probes.FirstOrDefault(state => state.UUID == ActiveProbeUUID);

        public int TotalProbeCount = 0;

        public bool ShowAllProbePanels = true;

        public float ProbePanelHeight = 1440f;
    }
}
