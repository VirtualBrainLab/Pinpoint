using System;
using UnityEngine;

namespace Models.Scene
{
    /// <summary>
    /// Represents the world-space computed position and orientation of a probe.
    /// This is an unsaved state that reflects computed values from ProbeManager and ProbeController
    /// after they update the CCF coordinates (APMLDV).
    /// </summary>
    [Serializable]
    public record ProbeWorldState
    {
        #region Core Identity

        /// <summary>
        /// Name/identifier of the probe (matches ProbeState.Name)
        /// </summary>
        public string Name = string.Empty;

        #endregion

        #region World-Space Position and Orientation

        /// <summary>
        /// World-space position of the probe tip in untransformed coordinates (WorldU)
        /// </summary>
        public Vector3 TipPositionWorldU;

        /// <summary>
        /// World-space position of the probe tip in transformed coordinates (WorldT)
        /// </summary>
        public Vector3 TipPositionWorldT;

        /// <summary>
        /// World-space right vector of the probe tip
        /// </summary>
        public Vector3 TipRightWorldU;

        /// <summary>
        /// World-space up vector of the probe tip
        /// </summary>
        public Vector3 TipUpWorldU;

        /// <summary>
        /// World-space forward vector of the probe tip
        /// </summary>
        public Vector3 TipForwardWorldU;

        #endregion

        #region Surface Coordinates

        /// <summary>
        /// Brain surface coordinate in transformed space (WorldT)
        /// </summary>
        public Vector3 SurfaceCoordinateWorldT;

        /// <summary>
        /// Brain surface coordinate in untransformed space (WorldU)
        /// </summary>
        public Vector3 SurfaceCoordinateWorldU;

        /// <summary>
        /// Brain surface coordinate in atlas transformed space (CoordT)
        /// </summary>
        public Vector3 SurfaceCoordinateT;

        /// <summary>
        /// Whether the probe is currently in the brain
        /// </summary>
        public bool IsProbeInBrain;

        #endregion

        #region Recording Region

        /// <summary>
        /// Recording region base coordinate in world space (untransformed)
        /// </summary>
        public Vector3 RecRegionBaseCoordWorldU;

        /// <summary>
        /// Recording region top coordinate in world space (untransformed)
        /// </summary>
        public Vector3 RecRegionTopCoordWorldU;

        #endregion
    }
}
