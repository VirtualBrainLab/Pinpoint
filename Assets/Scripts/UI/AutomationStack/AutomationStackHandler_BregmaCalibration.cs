using System;

namespace UI.AutomationStack
{
    /// <summary>
    ///     Implements Bregma calibration in the Automation Stack.<br />
    /// </summary>
    public partial class AutomationStackHandler
    {
        private partial void ResetBregmaCalibration()
        {
            // Throw exception if invariant is violated.
            if (!_state.IsEnabled)
                throw new InvalidOperationException(
                    "Cannot reset Bregma calibration if automation is not enabled on probe "
                        + ProbeManager.ActiveProbeManager.name
                );

            // Reset the zero coordinate of the active probe manager.
            ActiveManipulatorBehaviorController.ResetZeroCoordinate();

            // Set probe's automation state to be calibrated.
            ActiveProbeStateManager.SetCalibrated();
        }
    }
}
