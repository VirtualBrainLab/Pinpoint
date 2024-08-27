using System.Collections.Generic;
using Core.Util;
using UI.AutomationStack;
using Unity.Properties;
using UnityEngine;

namespace UI.States
{
    [CreateAssetMenu]
    public class AutomationStackState : ResettingScriptableObject
    {
        #region Properties

        // Stack enabled state.
        [CreateProperty]
        public bool IsEnabled =>
            ProbeManager.ActiveProbeManager
            && ProbeManager.ActiveProbeManager.IsEphysLinkControlled;

        // Step enabled states.
        [CreateProperty]
        public bool IsDriveToTargetEntryCoordinateButtonEnabled =>
            ProbeManager.ActiveProbeManager
            && ProbeManager.ActiveProbeManager.IsEphysLinkControlled
            && ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.HasCalibratedToBregma;

        [CreateProperty]
        public bool IsDriveToTargetInsertionButtonEnabled =>
            ProbeManager.ActiveProbeManager
            && ProbeManager.ActiveProbeManager.IsEphysLinkControlled
            && ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.HasResetDura;
        
        // Target insertion options.
        [CreateProperty]
        public List<(Color, string)> TargetInsertionOptions =>
            AutomationStackHandler.GetTargetInsertionOptions();
        #endregion
    }
}
