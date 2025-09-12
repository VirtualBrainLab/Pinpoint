using System;
using UnityEngine;
using Utils.Types;

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

        public Vector3 Angles;

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
