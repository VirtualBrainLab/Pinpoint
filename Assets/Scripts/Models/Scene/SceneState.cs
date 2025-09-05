using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Utils.Types;

namespace Models.Scene
{
    [Serializable]
    public record SceneState
    {
        public List<ProbeState> Probes = new();

        #region Active Probe

        public string ActiveProbeName;

        // Helper to get the active probe state based on the ActiveProbeName.
        public ProbeState ActiveProbeState =>
            Probes.FirstOrDefault(state => state.Name == ActiveProbeName);

        // Helper to get the active probe index.
        public int ActiveProbeIndex => Probes.IndexOf(ActiveProbeState);

        #endregion
        [NonSerialized]
        public int TotalProbeCount = 0;

        public bool ShowAllProbePanels = true;

        public float ProbePanelHeight = 1440f;

        #region Brain areas

        public string AtlasName;

        public List<AreaDisplayType> BrainAreaVisibility = new();

        #endregion
    }
}
