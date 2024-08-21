using System.Collections.Generic;
using Core.Util;
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
            && ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.HasResetBregma;

        [CreateProperty]
        public bool IsDriveToTargetInsertionButtonEnabled =>
            ProbeManager.ActiveProbeManager
            && ProbeManager.ActiveProbeManager.IsEphysLinkControlled
            && ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.HasResetDura;
        #endregion
    }
}
