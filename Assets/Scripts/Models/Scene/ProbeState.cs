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

        public ProbeColor Color = ProbeColor.LightBlue;
        public Color ColorValue => ProbeProperties.ProbeColors[(int)Color];

        public ProbeDisplayType ProbeDisplayType = ProbeDisplayType.Opaque;

        public bool Locked = false;

        #endregion

        #region Position and Orientation

        public Vector3 APMLDV;

        public float DepthT => Vector3.Distance(APMLDV, BrainSurfaceCoordT);

        public Vector3 Angles;

        public Vector3 RecRegionBaseCoordWorldU;

        public Vector3 RecRegionTopCoordWorldU;

        #endregion

        #region Coordinate Space and Transform

        public string AtlasSpaceName;

        public string AtlasTransformName;

        #endregion

        #region Channel Map

        public string SelectionLayerName = "default";

        public float MinChannelHeight;

        public float MaxChannelHeight;

        #endregion

        #region Brain Surface

        public bool ProbeInBrain = false;

        public Vector3 BrainSurfaceCoordT;

        public Vector3 BrainSurfaceWorldU;

        public Vector3 BrainSurfaceWorldT;

        #endregion

        #region API Integration

        public string APITarget;

        #endregion

        #region UI State

        public bool IsActive = false;

        public bool UIVisible = true;

        #endregion

        #region Ephys Link Control

        public bool IsEphysLinkControlled;

        public int NumAxes;

        public string ManipulatorID;

        public Vector3 Dimensions;

        public bool IsRightHanded;

        #endregion

        #region Automation

        public AutomationProgressState AutomationProgressState;

        public Vector4 ReferenceCoordinateOffset;

        public string SelectedTargetInsertionProbeName;

        public float DuraDepth;

        public Vector3 DuraCoordinate;

        public bool Drop2SurfaceWithDepth;

        /// <summary>
        /// Base insertion speed (µm/s).
        /// </summary>
        public int InsertionBaseSpeed;

        /// <summary>
        /// Drive past target distance (µm).
        /// </summary>
        public int DrivePastDistance;

        #endregion
    }
}
