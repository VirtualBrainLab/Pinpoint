using System;
using System.Collections.Generic;
using System.Linq;
using BrainAtlas.CoordinateSystems;
using Pinpoint.CoordinateSystems;
using UnityEngine;
using Utils.Types;

namespace Models.Scene
{
    [Serializable]
    public record SceneState
    {
        public List<ProbeState> Probes = new();
        public List<ManipulatorState> Manipulators = new();

        #region Active Probe

        [NonSerialized]
        public string ActiveProbeName;

        // Helper to get the active probe state based on the ActiveProbeName.
        public ProbeState ActiveProbeState =>
            Probes.FirstOrDefault(state => state.Name == ActiveProbeName);

        // Helper to get the active probe index.
        public int ActiveProbeIndex => Probes.IndexOf(ActiveProbeState);

        #endregion

        #region Active Manipulator

        [NonSerialized]
        public string ActiveManipulatorId;

        // Helper to get the active probe state based on the ActiveProbeName.
        public ManipulatorState ActiveManipulatorState =>
            Manipulators.FirstOrDefault(state => state.Id == ActiveManipulatorId);

        // Helper to get the active probe index.
        public int ActiveManipulatorIndex => Manipulators.IndexOf(ActiveManipulatorState);

        #endregion

        #region Manipulator Platform Info

        public int NumberOfAxesOnManipulator;
        public Vector4 ManipulatorDimensions;
        public CoordinateSpace ManipulatorCoordinateSpace;

        #endregion

        [NonSerialized]
        public int TotalProbeCount = 0;

        public bool ShowAllProbePanels = true;

        public float ProbePanelHeight = 1440f;

        #region Brain areas

        public string AtlasName;

        public Dictionary<int, AreaDisplayType> BrainAreaVisibility = new();

        #endregion
    }
}
