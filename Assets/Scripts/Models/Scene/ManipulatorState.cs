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

        #region Automation

        public AutomationProgressState AutomationProgressState;

        public string TargetInsertionProbeName;

        /// <summary>
        /// Manipulator depth when at the surface coordinate (mm).
        /// </summary>
        public float DuraDepth;

        /// <summary>
        /// Atlas coordinate of the surface coordinate at the point of calibration (entry).
        /// </summary>
        public Vector4 DuraCoordinate;

        /// <summary>
        /// Base insertion speed (µm/s).
        /// </summary>
        public int InsertionSpeed;
        
        /// <summary>
        /// Drive past target distance (µm).
        /// </summary>
        public int DrivePastDistance = 50;

        #endregion

        #region Demo

        public Vector4 DemoHomeCoordinate;
        
        public Vector4 DemoTargetCoordinate;

        /// <summary>
        /// Indicates if the demo mode is currently running.
        /// </summary>
        /// <remarks>Not saved. Will always start as off.</remarks>
        [NonSerialized]
        public bool IsDemoRunning;

        #endregion
    }
}
