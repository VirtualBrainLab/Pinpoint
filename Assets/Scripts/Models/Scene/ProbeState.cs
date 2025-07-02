using System;
using UI.Utils;
using UnityEngine;

namespace Models.Scene
{
    [Serializable]
    public enum ProbeTypeState : int
    {
        Placeholder = -1,
        Neuropixels1 = 0,
        Neuropixels21 = 21,
        Neuropixels24 = 24,
        Neuropixels24x2 = 28,
        UCLA128K = 128,
        UCLA256F = 256,
        Pipette25 = 25,
        Pipette50 = 50,
        Pipette100 = 100,
        Pipette200 = 200
    }

    [Serializable]
    public enum ProbeDisplayTypeState
    {
        Opaque,
        Transparent,
        Line
    }

    [Serializable]
    public record ProbeState
    {
        #region Core Identity
        
        [SerializeField]
        public string UUID;

        [SerializeField]
        public string OverrideName;

        [SerializeField]
        public string Name;

        [SerializeField]
        public bool Saved = true;

        #endregion

        #region Probe Configuration

        [SerializeField]
        public ProbeTypeState ProbeType = ProbeTypeState.Neuropixels1;

        [SerializeField]
        public Color Color = Color.white;

        [SerializeField]
        public ProbeDisplayTypeState ProbeDisplayType = ProbeDisplayTypeState.Opaque;

        [SerializeField]
        public bool Locked = false;

        #endregion

        #region Position and Orientation

        [SerializeField]
        public Vector3 APMLDV;

        [SerializeField]
        public Vector3 Angles;

        [SerializeField]
        public Vector3 RecRegionBaseCoordWorldU;

        [SerializeField]
        public Vector3 RecRegionTopCoordWorldU;

        #endregion

        #region Coordinate Space and Transform

        [SerializeField]
        public string AtlasSpaceName;

        [SerializeField]
        public string AtlasTransformName;

        #endregion

        #region Channel Map

        [SerializeField]
        public string SelectionLayerName = "default";

        [SerializeField]
        public float MinChannelHeight;

        [SerializeField]
        public float MaxChannelHeight;

        #endregion

        #region Brain Surface

        [SerializeField]
        public bool ProbeInBrain = false;

        [SerializeField]
        public Vector3 BrainSurfaceCoordT;

        [SerializeField]
        public Vector3 BrainSurfaceWorldU;

        [SerializeField]
        public Vector3 BrainSurfaceWorldT;

        #endregion

        #region API Integration

        [SerializeField]
        public string APITarget;

        #endregion

        #region UI State

        [SerializeField]
        public bool IsActive = false;

        [SerializeField]
        public bool UIVisible = true;

        #endregion

        #region Ephys Link Control

        [SerializeField]
        public bool IsEphysLinkControlled;

        [SerializeField]
        public int NumAxes;

        [SerializeField]
        public string ManipulatorID;

        [SerializeField]
        public Vector4 ZeroCoordOffset;

        [SerializeField]
        public Vector3 Dimensions;

        [SerializeField]
        public float BrainSurfaceOffset;

        [SerializeField]
        public bool Drop2SurfaceWithDepth;

        [SerializeField]
        public bool IsRightHanded;

        #endregion

        #region Automation

        [SerializeField]
        public ProbeAutomationProgress Progress;

        [SerializeField]
        public Vector4 ReferenceCoordinateOffset;
        
        [SerializeField]
        public int ProbeAutomationStateIndex = -1;

        // TODO: migrate to ProbeState once the appropriate fields have been implemented.
        [SerializeField]
        public ProbeManager SelectedTargetInsertionProbeState;

        #endregion
    }
}
