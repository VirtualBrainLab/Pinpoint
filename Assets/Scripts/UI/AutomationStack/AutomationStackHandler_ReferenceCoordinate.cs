using System;
using UI.States;

namespace UI.AutomationStack
{
    /// <summary>
    ///     Implements Bregma calibration in the Automation Stack.<br />
    /// </summary>
    public partial class AutomationStackHandler
    {
        private async partial void ResetReferenceCoordinate()
        {
            // Throw exception if invariant is violated.
            if (!AutomationStackState.IsEnabled)
                throw new InvalidOperationException(
                    "Cannot reset Bregma calibration if automation is not enabled on probe "
                        + ProbeManager.ActiveProbeManager.name
                );

            // Reset the Bregma calibration of the active probe manager.
            if (await ActiveManipulatorBehaviorController.ResetReferenceCoordinate())
                // Set probe's automation state to be calibrated if it did happen.
                ActiveProbeStateManager.SetCalibrated();
        }
    }
}
