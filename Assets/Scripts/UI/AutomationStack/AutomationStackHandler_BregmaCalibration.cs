using UnityEngine;

namespace UI.AutomationStack
{
    /// <summary>
    ///     Implements Bregma calibration in the Automation Stack.<br />
    /// </summary>
    public partial class AutomationStackHandler
    {
        private partial void ResetBregmaCalibration()
        {
            // Reset the zero coordinate of the active probe manager.
            ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.ResetZeroCoordinate();
            
            // Add the active probe manager to the calibrated to Bregma probes.
            _state.CalibratedToBregmaProbes.Add(ProbeManager.ActiveProbeManager);
        }
    }
}
