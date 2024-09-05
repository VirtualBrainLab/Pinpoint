using System;
using BrainAtlas;
using EphysLink;
using UnityEngine;

namespace Pinpoint.Probes.ManipulatorBehaviorController
{
    public partial class ManipulatorBehaviorController
    {
        #region Constants

        #region Relative distances

        /// <summary>
        ///     Extra safety margin for the Dura to outside to ensure probe is fully retracted (mm).
        /// </summary>
        private const float DURA_MARGIN_DISTANCE = 0.2f;

        /// <summary>
        ///     Distance from target to start slowing down probe (mm).
        /// </summary>
        private const float NEAR_TARGET_DISTANCE = 1f;

        #endregion

        #region Speed multipliers

        /// <summary>
        ///     Slowdown factor for the probe when it is near the target.
        /// </summary>
        private const float NEAR_TARGET_SPEED_MULTIPLIER = 2f / 3f;

        /// <summary>
        ///     Extra speed multiplier for the probe when it is exiting.
        /// </summary>
        private const int EXIT_DRIVE_SPEED_MULTIPLIER = 6;

        /// <summary>
        ///     Speed multiplier of the probe once outside the brain.
        /// </summary>
        private const int OUTSIDE_DRIVE_SPEED_MULTIPLIER = 50;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        ///     Is the probe driving in the insertion cycle?
        /// </summary>
        /// <remarks>Used to identify which buttons should be made available.</remarks>
        public bool IsMoving { get; private set; }

        #region Caches

        private Vector3 _cachedTargetCoordinate = Vector3.negativeInfinity;
        private Vector3 _cachedOffsetAdjustedTargetCoordinate = Vector3.negativeInfinity;

        #endregion

        #endregion

        #region Drive Functions

        /// <summary>
        ///     Start or resume inserting the probe to the target insertion.
        /// </summary>
        /// <param name="targetInsertionProbeManager">Probe manager for the target insertion.</param>
        /// <param name="baseSpeed">Base driving speed in mm/s.</param>
        /// <param name="drivePastDistance">Distance to drive past target in mm.</param>
        /// <exception cref="InvalidOperationException">Probe is not in a drivable state.</exception>
        public void Drive(
            ProbeManager targetInsertionProbeManager,
            float baseSpeed,
            float drivePastDistance
        )
        {
            // Throw exception if invariant is violated.
            if (!ProbeAutomationStateManager.IsInsertable())
                throw new InvalidOperationException(
                    "Cannot drive to target insertion if the probe is not in a drivable state."
                );

            // Get distance to target.
            var distanceToTarget = GetCurrentDistanceToTarget(
                targetInsertionProbeManager.ProbeController.Insertion
            );

            // Get target depth.
            var targetDepth =
                _duraDepth
                + GetCurrentDistanceToTarget(targetInsertionProbeManager.ProbeController.Insertion);

            // Switch behavior based on state.
            switch (ProbeAutomationStateManager.GetState())
            {
                case ProbeAutomationState.AtDuraInsert:
                    ProbeAutomationStateManager.SetDrivingToNearTarget();
                    goto case ProbeAutomationState.DrivingToNearTarget;
                case ProbeAutomationState.DrivingToNearTarget:
                    // Drive to near target if not already there.
                    if (distanceToTarget > NEAR_TARGET_DISTANCE)
                        CommunicationManager.Instance.SetDepth(
                            new SetDepthRequest(
                                ManipulatorID,
                                targetDepth - NEAR_TARGET_DISTANCE,
                                baseSpeed
                            ),
                            _ =>
                            {
                                // TODO: Complete drive
                            },
                            Debug.LogError
                        );

                    break;
            }

            // Set probe to be moving.
            IsMoving = true;
        }

        /// <summary>
        ///     Stop the probe's movement.
        /// </summary>
        public void Stop()
        {
            // Set probe to be not moving.
            IsMoving = false;
        }

        #endregion

        #region Helper Functions

        /// <summary>
        ///     Compute the target coordinate adjusted for the probe's actual position.
        /// </summary>
        /// <param name="targetInsertion">Target insertion to computer with.</param>
        /// <returns>APMLDV coordinates of where the probe should actually go.</returns>
        private Vector3 GetOffsetAdjustedTargetCoordinate(ProbeInsertion targetInsertion)
        {
            // Shortcut exit if already computed and targetInsertion did not change.
            if (
                targetInsertion.APMLDV == _cachedTargetCoordinate
                && !float.IsNegativeInfinity(_cachedOffsetAdjustedTargetCoordinate.x)
            )
                return _cachedOffsetAdjustedTargetCoordinate;

            var targetWorldT = targetInsertion.PositionWorldT();
            var relativePositionWorldT = _probeController.Insertion.PositionWorldT() - targetWorldT;
            var probeTipTForward = _probeController.ProbeTipT.forward;
            var offsetAdjustedRelativeTargetPositionWorldT = Vector3.ProjectOnPlane(
                relativePositionWorldT,
                probeTipTForward
            );
            var offsetAdjustedTargetCoordinateWorldT =
                targetWorldT + offsetAdjustedRelativeTargetPositionWorldT;

            // Converting worldT to AtlasT (to capture new Bregma offset when there is scaling)
            // then switch axes to get APMLDV.
            var offsetAdjustedTargetCoordinateAtlasT =
                BrainAtlasManager.ActiveReferenceAtlas.World2Atlas(
                    offsetAdjustedTargetCoordinateWorldT
                );
            var offsetAdjustedTargetCoordinateT = BrainAtlasManager.ActiveAtlasTransform.U2T_Vector(
                offsetAdjustedTargetCoordinateAtlasT
            );

            // Cache the computed values.
            _cachedTargetCoordinate = targetInsertion.APMLDV;
            _cachedOffsetAdjustedTargetCoordinate = offsetAdjustedTargetCoordinateT;

            return _cachedOffsetAdjustedTargetCoordinate;
        }

        /// <summary>
        ///     Compute the absolute distance from the target insertion to the Dura.
        /// </summary>
        /// <param name="targetInsertion">Target insertion object to calculate with.</param>
        /// <returns>Distance in mm to the target from the Dura.</returns>
        private float GetTargetDistanceToDura(ProbeInsertion targetInsertion)
        {
            return Vector3.Distance(
                GetOffsetAdjustedTargetCoordinate(targetInsertion),
                _duraCoordinate
            );
        }

        /// <summary>
        ///     Compute the current distance to the target insertion.
        /// </summary>
        /// <param name="targetInsertion">Target insertion to compute distance to.</param>
        /// <returns>Distance in mm to the target from the probe.</returns>
        private float GetCurrentDistanceToTarget(ProbeInsertion targetInsertion)
        {
            return Vector3.Distance(
                _probeController.Insertion.APMLDV,
                GetOffsetAdjustedTargetCoordinate(targetInsertion)
            );
        }

        #endregion
    }
}
