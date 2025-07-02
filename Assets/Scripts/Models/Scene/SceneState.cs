using System;
using System.Collections.Generic;
using UnityEngine;

namespace Models.Scene
{
    [Serializable]
    public record SceneState
    {
        [SerializeField]
        public List<ProbeState> Probes = new();
        
        [SerializeField]
        public int ActiveProbeIndex = -1;
        
    }
}