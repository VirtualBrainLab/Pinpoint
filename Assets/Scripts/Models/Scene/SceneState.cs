using System;
using System.Collections.Generic;
using UnityEngine;

namespace Models.Scene
{
    [Serializable]
    public record SceneState
    {
        [SerializeField]
        public List<ProbeState> Probes = new List<ProbeState>();
        
        [SerializeField]
        public int ActiveProbeIndex = -1;

        [SerializeField]
        public string ActiveProbeUUID;

        [SerializeField]
        public int TotalProbeCount = 0;

        [SerializeField]
        public bool ShowAllProbePanels = true;

        [SerializeField]
        public float ProbePanelHeight = 1440f;
        
    }
}