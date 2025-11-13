using System;
using UnityEngine;
using UnityEngine.Serialization;
using Utils.Types;

namespace Models.Scene
{
    [Serializable]
    public record ProbeState
    {
        #region Core Identity

        public string Name = Guid.NewGuid().ToString();

        public ProbeType ProbeType;

        #endregion

        #region Probe Configuration

        public ProbeColor Color = ProbeColor.DarkBlue;
        public Color ColorValue => ProbeProperties.ProbeColors[(int)Color];

        public ProbeDisplayType ProbeDisplayType = ProbeDisplayType.Transparent;

        public bool Locked;

        #endregion

        #region Position and Orientation

        public Vector3 APMLDV;

        /// <summary>
        /// In degrees: (Yaw, Pitch, Roll).
        /// </summary>
        /// <remarks>Defaults to pointing straight down.</remarks>
        public Vector3 Angles = new(0, 90, 0);

        #endregion

        #region Coordinate Space and Transform

        public string AtlasSpaceName;

        public string AtlasTransformName;

        #endregion

        #region Channel Map

        public string SelectionLayerName = "default";

        #endregion

        #region API Integration

        public string APITarget;

        #endregion

        #region UI State

        public bool IsActive = false;

        public bool UIVisible = true;

        #endregion
    }
}
