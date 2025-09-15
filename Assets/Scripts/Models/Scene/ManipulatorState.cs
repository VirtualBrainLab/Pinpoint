using System;
using BrainAtlas.CoordinateSystems;
using UnityEngine;
using Utils.Types;
using CoordinateSpace = System.Drawing.Drawing2D.CoordinateSpace;

namespace Models.Scene
{
    [Serializable]
    public record ManipulatorState
    {
        #region Core Identity

        public string Id;

        #endregion

        #region Visualization Probe

        public string VisualizationProbeName;

        #endregion

        #region Orientation

        public Vector3 Angles = new(0, 90, 0);

        public ManipulatorHandedness Handedness;

        #endregion

        #region Calibration

        public Vector4 ReferenceCoordinateOffset;

        public float DuraOffset;

        #endregion

        #region Controls

        public bool ManualControlEnabled;

        #endregion
    }
}
