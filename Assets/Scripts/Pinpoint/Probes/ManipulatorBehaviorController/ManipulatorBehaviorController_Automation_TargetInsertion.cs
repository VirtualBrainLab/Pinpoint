using BrainAtlas;
using UnityEngine;

namespace Pinpoint.Probes.ManipulatorBehaviorController
{
    public partial class ManipulatorBehaviorController
    {
        #region Constants

        // Axes colors.
        private static readonly Color AP_COLOR = new(1, 0.3215686f, 0.3215686f);
        private static readonly Color ML_COLOR = new(0.2039216f, 0.6745098f, 0.8784314f);
        private static readonly Color DV_COLOR = new(1, 0.854902f, 0.4745098f);

        // Trajectory line properties.
        private const float LINE_WIDTH = 0.1f;
        private const int NUM_SEGMENTS = 2;

        // Trajectory values.
        public static readonly Vector3 PRE_DEPTH_DRIVE_DV_OFFSET = new(0, 3.5f, 0);

        #endregion

        #region Components

        private (GameObject ap, GameObject ml, GameObject dv) _trajectoryLineGameObjects;
        private (LineRenderer ap, LineRenderer ml, LineRenderer dv) _trajectoryLineLineRenderers;

        #endregion

        #region Properties

        private (
            ProbeInsertion ap,
            ProbeInsertion ml,
            ProbeInsertion dv
        ) _trajectoryProbeInsertions;

        #endregion

        #region Public Functions

        /// <summary>
        ///     Compute the entry coordinate and trajectory for the target insertion. Also, draw the trajectory lines.
        /// </summary>
        /// <param name="targetInsertionProbeManager">Probe manager of the target insertion</param>
        /// <returns>The computed entry coordinate in AP, ML, DV coordinates. Negative infinity if target is unset.</returns>
        public Vector3 ComputeEntryCoordinateTrajectory(ProbeManager targetInsertionProbeManager)
        {
            // If set to null, cleanup and remove insertion trajectory.
            if (targetInsertionProbeManager == null)
            {
                RemoveTrajectoryLines();
                return Vector3.negativeInfinity;
            }

            // Compute the trajectory.
            ComputeTrajectory(targetInsertionProbeManager);

            // Create trajectory lines.
            CreateTrajectoryLines();

            // Set trajectory lines.
            UpdateTrajectoryLines();

            // Return final entry coordinate (coordinate of the ML movement, the last in the trajectory).
            return _trajectoryProbeInsertions.ml.APMLDV;
        }

        #endregion

        #region Internal Functions

        private void ComputeTrajectory(ProbeManager targetInsertionProbeManager)
        {
            // Set DV axis.
            _trajectoryProbeInsertions.dv = new ProbeInsertion(
                _probeManager.ProbeController.Insertion
            )
            {
                DV = BrainAtlasManager
                    .ActiveAtlasTransform.U2T_Vector(
                        BrainAtlasManager.ActiveReferenceAtlas.World2Atlas_Vector(
                            PRE_DEPTH_DRIVE_DV_OFFSET
                        )
                    )
                    .z
            };

            // Recalculate AP and ML axes.
            var brainSurfaceTransformed = targetInsertionProbeManager
                .GetSurfaceCoordinateT()
                .surfaceCoordinateT;

            // Set AP axis.
            _trajectoryProbeInsertions.ap = new ProbeInsertion(_trajectoryProbeInsertions.dv)
            {
                AP = brainSurfaceTransformed.x
            };

            // Set ML axis.
            _trajectoryProbeInsertions.ml = new ProbeInsertion(_trajectoryProbeInsertions.ap)
            {
                ML = brainSurfaceTransformed.y
            };
        }

        /// <summary>
        ///     Create the trajectory line game objects and line renderers (if needed).
        /// </summary>
        private void CreateTrajectoryLines()
        {
            // Shortcut exit if they already exist.
            if (_trajectoryLineGameObjects.ap != null)
                return;

            // Create the trajectory line game objects.
            _trajectoryLineGameObjects = (
                new GameObject("APTrajectoryLine") { layer = 5 },
                new GameObject("MLTrajectoryLine") { layer = 5 },
                new GameObject("DVTrajectoryLine") { layer = 5 }
            );

            // Create the line renderers.
            _trajectoryLineLineRenderers = (
                _trajectoryLineGameObjects.ap.AddComponent<LineRenderer>(),
                _trajectoryLineGameObjects.ml.AddComponent<LineRenderer>(),
                _trajectoryLineGameObjects.dv.AddComponent<LineRenderer>()
            );

            // Apply materials.
            var defaultSpriteShader = Shader.Find("Sprites/Default");
            _trajectoryLineLineRenderers.ap.material = new Material(defaultSpriteShader)
            {
                color = AP_COLOR
            };
            _trajectoryLineLineRenderers.ml.material = new Material(defaultSpriteShader)
            {
                color = ML_COLOR
            };
            _trajectoryLineLineRenderers.dv.material = new Material(defaultSpriteShader)
            {
                color = DV_COLOR
            };

            // Set line widths.
            _trajectoryLineLineRenderers.ap.startWidth = _trajectoryLineLineRenderers.ap.endWidth =
                LINE_WIDTH;
            _trajectoryLineLineRenderers.ml.startWidth = _trajectoryLineLineRenderers.ml.endWidth =
                LINE_WIDTH;
            _trajectoryLineLineRenderers.dv.startWidth = _trajectoryLineLineRenderers.dv.endWidth =
                LINE_WIDTH;

            // Set segment counts.
            _trajectoryLineLineRenderers.ap.positionCount = NUM_SEGMENTS;
            _trajectoryLineLineRenderers.ml.positionCount = NUM_SEGMENTS;
            _trajectoryLineLineRenderers.dv.positionCount = NUM_SEGMENTS;
        }

        /// <summary>
        ///     Update the trajectory line positions.
        /// </summary>
        private void UpdateTrajectoryLines()
        {
            _trajectoryLineLineRenderers.dv.SetPosition(0, _probeController.ProbeTipT.position);
            _trajectoryLineLineRenderers.dv.SetPosition(
                1,
                _trajectoryProbeInsertions.dv.PositionWorldT()
            );

            _trajectoryLineLineRenderers.ap.SetPosition(
                0,
                _trajectoryProbeInsertions.dv.PositionWorldT()
            );
            _trajectoryLineLineRenderers.ap.SetPosition(
                1,
                _trajectoryProbeInsertions.ap.PositionWorldT()
            );

            _trajectoryLineLineRenderers.ml.SetPosition(
                0,
                _trajectoryProbeInsertions.ap.PositionWorldT()
            );
            _trajectoryLineLineRenderers.ml.SetPosition(
                1,
                _trajectoryProbeInsertions.ml.PositionWorldT()
            );
        }

        /// <summary>
        ///     Destroy the trajectory line game objects and line renderers. Also reset the references.
        /// </summary>
        private void RemoveTrajectoryLines()
        {
            // Destroy the objects.
            Destroy(_trajectoryLineGameObjects.ap);
            Destroy(_trajectoryLineGameObjects.ml);
            Destroy(_trajectoryLineGameObjects.dv);

            // Reset the references.
            _trajectoryLineGameObjects = (null, null, null);
            _trajectoryLineLineRenderers = (null, null, null);
        }

        #endregion
    }
}
