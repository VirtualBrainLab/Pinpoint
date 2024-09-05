using System;

namespace UI.AutomationStack
{
    /// <summary>
    ///     Implements Bregma calibration in the Automation Stack.<br />
    /// </summary>
    public partial class AutomationStackHandler
    {
        private async partial void ResetBregmaCalibration()
        {
            // Throw exception if invariant is violated.
            if (!_state.IsEnabled)
                throw new InvalidOperationException(
                    "Cannot reset Bregma calibration if automation is not enabled on probe "
                        + ProbeManager.ActiveProbeManager.name
                );

            // Reset the Bregma calibration of the active probe manager.
            if (await ActiveManipulatorBehaviorController.ResetZeroCoordinate())
                // Set probe's automation state to be calibrated if it did happen.
                ActiveProbeStateManager.SetCalibrated();
        }
    }
}
