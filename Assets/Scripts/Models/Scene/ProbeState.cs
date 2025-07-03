using System;
using UI.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Models.Scene
{
    [Serializable]
    public record ProbeState
    {
        #region Core Identity

        public string UUID;

        public string OverrideName;

        public string Name;

        public bool Saved = true;

        #endregion

        #region Probe Configuration

        public ProbeTypeState ProbeType = ProbeTypeState.Neuropixels1;

        public Color Color = Color.white;

        public ProbeDisplayTypeState ProbeDisplayType = ProbeDisplayTypeState.Opaque;

        public bool Locked = false;

        #endregion

        #region Position and Orientation

        public Vector3 APMLDV;

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

        public Vector4 ZeroCoordOffset;

        public Vector3 Dimensions;

        public float BrainSurfaceOffset;

        public bool Drop2SurfaceWithDepth;

        public bool IsRightHanded;

        #endregion

        #region Automation

        public ProbeAutomationProgress Progress;

        public Vector4 ReferenceCoordinateOffset;

        public int ProbeAutomationStateIndex = -1;

        public string SelectedTargetInsertionProbeUUID;

        #endregion
    }

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
        Pipette200 = 200,
    }

    [Serializable]
    public enum ProbeDisplayTypeState
    {
        Opaque,
        Transparent,
        Line,
    }
}
