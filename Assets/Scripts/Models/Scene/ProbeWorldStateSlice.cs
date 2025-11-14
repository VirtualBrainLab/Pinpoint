using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.Scene
{
    /// <summary>
    /// Unsaved state slice containing world-space computed values for all probes.
    /// This state is updated after ProbeManager and ProbeController run their updates
    /// on CCF coordinates (APMLDV) and compute world-space positions and orientations.
    /// Other components can subscribe to this slice to react to computed value changes.
    /// </summary>
    [Serializable]
    public record ProbeWorldStateSlice
    {
        /// <summary>
        /// List of world-space states for all probes
        /// </summary>
        public List<ProbeWorldState> ProbeWorldStates = new();

        /// <summary>
        /// Helper to get a specific probe's world state by name
        /// </summary>
        public ProbeWorldState GetProbeWorldState(string probeName)
        {
            return ProbeWorldStates.FirstOrDefault(state => state.Name == probeName);
        }
    }
}
