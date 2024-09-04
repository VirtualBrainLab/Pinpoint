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
        private ProbeAutomationState _probeAutomationState = ProbeAutomationState.IsUncalibrated;

        #endregion

        #region Actions

        /// <summary>
        ///     Set the probe's state to be calibrated.
        /// </summary>
        /// <remarks>This can be set from any state. This is the reset point.</remarks>
        public void SetCalibrated()
        {
            _probeAutomationState = ProbeAutomationState.IsCalibrated;
        }

        /// <summary>
        ///     Set the probe's state to be driving to the target entry coordinate.
        /// </summary>
        /// <exception cref="InvalidOperationException">Probe is not calibrated or at entry coordinate.</exception>
        public void SetDrivingToTargetEntryCoordinate()
        {
            if (!IsCalibrated() && _probeAutomationState != ProbeAutomationState.AtEntryCoordinate)
                throw new InvalidOperationException(
                    "Cannot set probe to driving to target entry coordinate if it is not calibrated."
                );

            _probeAutomationState = ProbeAutomationState.DrivingToTargetEntryCoordinate;
        }

        /// <summary>
        ///     Set the probe's state to be at the target entry coordinate.
        /// </summary>
        /// <exception cref="InvalidOperationException">Probe is not driving there or exiting to there.</exception>
        public void SetAtEntryCoordinate()
        {
            if (
                _probeAutomationState != ProbeAutomationState.DrivingToTargetEntryCoordinate
                && _probeAutomationState != ProbeAutomationState.ExitingToTargetEntryCoordinate
            )
                throw new InvalidOperationException(
                    "Cannot set probe to entry coordinate if it was not driving there or exiting to there."
                );

            _probeAutomationState = ProbeAutomationState.AtEntryCoordinate;
        }

        /// <summary>
        ///     Set the probe's state to be at the Dura for insertion.
        /// </summary>
        /// <exception cref="InvalidOperationException">Probe is not at the entry coordinate or exiting to Dura.</exception>
        public void SetAtDuraInsert()
        {
            if (
                _probeAutomationState != ProbeAutomationState.AtEntryCoordinate
                && _probeAutomationState != ProbeAutomationState.ExitingToDura
            )
                throw new InvalidOperationException(
                    "Cannot set probe to dura if it was not at the entry coordinate or exiting to Dura."
                );
            _probeAutomationState = ProbeAutomationState.AtDuraInsert;
        }

        #endregion

        #region Queries

        /// <summary>
        ///     Checks if the probe is past the calibration phase.
        /// </summary>
        /// <returns>True if the state is past the calibration phase, false otherwise.</returns>
        public bool IsCalibrated()
        {
            return _probeAutomationState >= ProbeAutomationState.IsCalibrated;
        }

        /// <summary>
        ///     Checks if the probe is driving to the target entry coordinate.
        /// </summary>
        /// <returns>True if the probe is in the driving to entry coordinate state, false otherwise.</returns>
        public bool IsDrivingToEntryCoordinate()
        {
            return _probeAutomationState == ProbeAutomationState.DrivingToTargetEntryCoordinate;
        }

        /// <summary>
        ///     Checks if the probe has been to the target entry coordinate.
        /// </summary>
        /// <returns>True if the state is past reaching the target entry coordinate, false otherwise.</returns>
        public bool HasReachedTargetEntryCoordinate()
        {
            return _probeAutomationState >= ProbeAutomationState.AtEntryCoordinate;
        }

        /// <summary>
        ///     Checks if the probe has been calibrated to the dura for insertion.
        /// </summary>
        /// <returns>True if the probe has been calibrated to the dura and is currently there, false otherwise.</returns>
        public bool IsAtDuraInsert()
        {
            return _probeAutomationState == ProbeAutomationState.AtDuraInsert;
        }

        #endregion
    }
}
