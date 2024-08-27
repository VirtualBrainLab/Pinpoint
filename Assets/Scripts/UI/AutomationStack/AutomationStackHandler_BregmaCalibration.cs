using UnityEngine;

namespace UI.AutomationStack
{
    /// <summary>
    ///     Implements Bregma calibration in the Automation Stack.<br />
    /// </summary>
    public partial class AutomationStackHandler : MonoBehaviour
    {
        private partial void ResetBregmaCalibration()
        {
            ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.ResetZeroCoordinate();
        }
    }
}
