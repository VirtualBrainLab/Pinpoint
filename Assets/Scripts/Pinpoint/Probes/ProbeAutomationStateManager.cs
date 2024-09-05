using System;

namespace Pinpoint.Probes
{
    /// <summary>
    ///     Define and manage the automation state of a probe.
    /// </summary>
    public class ProbeAutomationStateManager
    {
        #region Properties

        /// <summary>
        ///     The state of the probe.
        /// </summary>
        public ProbeAutomationState ProbeAutomationState { get; private set; } =
            ProbeAutomationState.IsUncalibrated;

        #endregion

        #region Actions

        /// <summary>
        ///     Set the probe's state to be calibrated.
        /// </summary>
        /// <remarks>This can be set from any state. This is the reset point.</remarks>
        public void SetCalibrated()
        {
            ProbeAutomationState = ProbeAutomationState.IsCalibrated;
        }

        /// <summary>
        ///     Set the probe's state to be driving to the target entry coordinate.
        /// </summary>
        /// <exception cref="InvalidOperationException">Probe is not calibrated or at entry coordinate.</exception>
        public void SetDrivingToTargetEntryCoordinate()
        {
            if (!IsCalibrated() && ProbeAutomationState != ProbeAutomationState.AtEntryCoordinate)
                throw new InvalidOperationException(
                    "Cannot set probe to driving to target entry coordinate if it is not calibrated."
                );

            ProbeAutomationState = ProbeAutomationState.DrivingToTargetEntryCoordinate;
        }

        /// <summary>
        ///     Set the probe's state to be at the target entry coordinate.
        /// </summary>
        /// <exception cref="InvalidOperationException">Probe is not driving there or exiting to there.</exception>
        public void SetAtEntryCoordinate()
        {
            if (
                ProbeAutomationState != ProbeAutomationState.DrivingToTargetEntryCoordinate
                && ProbeAutomationState != ProbeAutomationState.ExitingToTargetEntryCoordinate
            )
                throw new InvalidOperationException(
                    "Cannot set probe to entry coordinate if it was not driving there or exiting to there."
                );

            ProbeAutomationState = ProbeAutomationState.AtEntryCoordinate;
        }

        /// <summary>
        ///     Set the probe's state to be at the Dura for insertion.
        /// </summary>
        /// <exception cref="InvalidOperationException">Probe is not at the entry coordinate or exiting to Dura.</exception>
        public void SetAtDuraInsert()
        {
            if (
                ProbeAutomationState != ProbeAutomationState.AtEntryCoordinate
                && ProbeAutomationState != ProbeAutomationState.ExitingToDura
            )
                throw new InvalidOperationException(
                    "Cannot set probe to dura if it was not at the entry coordinate or exiting to Dura."
                );
            ProbeAutomationState = ProbeAutomationState.AtDuraInsert;
        }

        /// <summary>
        ///     Increment the state according to the normal loop for the insertion cycle.
        /// </summary>
        /// <exception cref="InvalidOperationException">Probe is not in the insertion cycle.</exception>
        public void IncrementInsertionCycleState()
        {
            // Throw exception if required state is not met.
            if (
                ProbeAutomationState
                is < ProbeAutomationState.AtDuraInsert
                    or > ProbeAutomationState.ExitingToTargetEntryCoordinate
            )
                throw new InvalidOperationException(
                    "Cannot increment the insertion cycle state if the probe is not in the insertion cycle."
                );

            // Increment state.
            ProbeAutomationState++;
        }

        /// <summary>
        ///     Set the probe's state to be in the next insertion driving state.
        /// </summary>
        /// <exception cref="InvalidOperationException">Probe is not in a state that can go drive.</exception>
        public void SetToInsertionDrivingState()
        {
            // Throw exception if required state is not met.
            if (
                ProbeAutomationState
                is < ProbeAutomationState.AtDuraInsert
                    or > ProbeAutomationState.ExitingToDura
            )
                throw new InvalidOperationException(
                    "Cannot set probe to insertion driving state if it is not at the Dura or inside the brain."
                );

            // Set state.
            ProbeAutomationState = ProbeAutomationState switch
            {
                // States for driving to near target depth.
                ProbeAutomationState.AtDuraInsert
                or ProbeAutomationState.ExitingToDura
                    => ProbeAutomationState.DrivingToNearTarget,

                // States for driving to the target.
                ProbeAutomationState.AtNearTargetInsert
                or ProbeAutomationState.ExitingToNearTarget
                or ProbeAutomationState.AtNearTargetExit
                    => ProbeAutomationState.DrivingToPastTarget,

                // States for returning to the target.
                ProbeAutomationState.AtPastTarget
                    => ProbeAutomationState.ReturningToTarget,

                // Do nothing for driving states.
                _ => ProbeAutomationState
            };
        }

        #endregion

        #region Queries

        /// <summary>
        ///     Checks if the probe is past the calibration phase.
        /// </summary>
        /// <returns>True if the state is past the calibration phase, false otherwise.</returns>
        public bool IsCalibrated()
        {
            return ProbeAutomationState >= ProbeAutomationState.IsCalibrated;
        }

        /// <summary>
        ///     Checks if the probe has been to the target entry coordinate.
        /// </summary>
        /// <returns>True if the state is past reaching the target entry coordinate, false otherwise.</returns>
        public bool HasReachedTargetEntryCoordinate()
        {
            return ProbeAutomationState >= ProbeAutomationState.AtEntryCoordinate;
        }

        /// <summary>
        ///     Checks if the probe can be inserted (driven into the brain).
        /// </summary>
        /// <returns>Returns true if the probe is calibrated to the Dura and has not exited back out of the Dura.</returns>
        public bool IsInsertable()
        {
            return ProbeAutomationState
                is >= ProbeAutomationState.AtDuraInsert
                    and < ProbeAutomationState.AtDuraExit;
        }

        /// <summary>
        ///     Checks if the probe can be retracted (drive back out of the brain).
        /// </summary>
        /// <returns>Returns true if the probe has gone through/past the Dura.</returns>
        public bool IsExitable()
        {
            return ProbeAutomationState > ProbeAutomationState.AtDuraInsert;
        }

        #endregion
    }
}
