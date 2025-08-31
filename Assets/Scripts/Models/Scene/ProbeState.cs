using System;
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

        public Vector3 Dimensions;

        public bool IsRightHanded;

        #endregion

        #region Automation

        public AutomationProgressState AutomationProgressState;
        
        public Vector4 ReferenceCoordinateOffset;
        
        public string SelectedTargetInsertionProbeUUID;
        
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

    public enum ProbeTypeState
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

    public enum ProbeDisplayTypeState
    {
        Opaque,
        Transparent,
        Line,
    }

    /// <summary>
    /// Progress state in the automation process.
    /// </summary>
    public enum AutomationProgressState
    {
        /// <summary>
        ///     Initial, uncalibrated state.
        /// </summary>
        IsUncalibrated,

        /// <summary>
        ///     Is calibrated to the reference coordinate. Could be positioned anywhere.
        /// </summary>
        IsCalibrated,

        /// <summary>
        ///     Moving to the target entry coordinate.
        /// </summary>
        DrivingToTargetEntryCoordinate,

        /// <summary>
        ///     At the target entry coordinate.
        /// </summary>
        AtTargetEntryCoordinate,

        /// <summary>
        ///     Calibrated to the Dura; ready for insertion drive.
        /// </summary>
        AtDuraInsert,

        /// <summary>
        ///     Driving to near target depth (insertion drive).
        /// </summary>
        DrivingToNearTarget,

        /// <summary>
        ///     At near target depth (insertion drive). Need to switch to 2/3 speed.
        /// </summary>
        AtNearTargetInsert,

        /// <summary>
        ///     Driving to past target depth (insertion drive).
        /// </summary>
        DrivingToPastTarget,

        /// <summary>
        ///     At past target depth (insertion drive).
        /// </summary>
        AtPastTarget,

        /// <summary>
        ///     Driving back up to target depth (insertion drive).
        /// </summary>
        ReturningToTarget,

        /// <summary>
        ///     At target depth (insertion drive).
        /// </summary>
        AtTarget,

        /// <summary>
        ///     Driving back up to the Dura (exit drive).
        /// </summary>
        ExitingToDura,

        /// <summary>
        ///     At the Dura (exit drive). Should not re-insert.
        /// </summary>
        AtDuraExit,

        /// <summary>
        ///     Driving above the Dura by a safe margin (exit drive).
        /// </summary>
        ExitingToMargin,

        /// <summary>
        ///     At the safe margin above the Dura (exit drive).
        /// </summary>
        AtExitMargin,

        /// <summary>
        ///     Driving back up to the target entry coordinate (exit drive).
        /// </summary>
        ExitingToTargetEntryCoordinate,
    }
}
