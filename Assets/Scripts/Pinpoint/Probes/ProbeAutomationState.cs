namespace Pinpoint.Probes
{
    /// <summary>
    ///     Drive state in the automation cycle.
    /// </summary>
    public enum ProbeAutomationState
    {
        /// <summary>
        ///     Initial, uncalibrated state.
        /// </summary>
        IsUncalibrated,

        /// <summary>
        ///     Is calibrated to Bregma. Could be positioned anywhere.
        /// </summary>
        IsCalibrated,

        /// <summary>
        ///     Moving to the target entry coordinate.
        /// </summary>
        DrivingToEntryCoordinate,

        /// <summary>
        ///     At the target entry coordinate.
        /// </summary>
        AtEntryCoordinate,

        /// <summary>
        ///     Exiting to the entry coordinate (safe position from dura).
        /// </summary>
        ExitingToOutside,

        /// <summary>
        ///     Calibrated to the dura.
        /// </summary>
        AtDura
    }
}
