using System;

namespace UI.AutomationStack
{
    /// <summary>
    ///     Implements Dura calibration in the Automation Stack.<br />
    /// </summary>
    public partial class AutomationStackHandler
    {
        private partial void OnResetDuraCalibrationPressed()
        {
            if (!_state.IsEnabled)
                throw new InvalidOperationException(
                    "Cannot reset Dura calibration if automation is not enabled on probe "
                        + ProbeManager.ActiveProbeManager.name
                );
            
            // Reset Dura calibration on the active probe manager.
            ActiveManipulatorBehaviorController.ResetDuraOffset();
            
            // Set probe's automation state to be at Dura.
            ActiveProbeStateManager.SetAtDuraInsert();
        }
    }
}
