using UI.States;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class AutomationStackHandler : MonoBehaviour
    {
        #region Components

        // State
        [SerializeField]
        private AutomationStackState _state;

        // Document.
        [SerializeField]
        private UIDocument _uiDocument;
        private VisualElement _root => _uiDocument.rootVisualElement;

        // Panels.
        private VisualElement _automationStackPanel;

        // Interface.
        private Button _resetBregmaCalibrationButton;

        #endregion

        #region Unity

        private void OnEnable()
        {
            // Get components.
            _automationStackPanel = _root.Q("AutomationStackPanel");
            _resetBregmaCalibrationButton = _automationStackPanel.Q<Button>(
                "ResetBregmaCalibrationButton"
            );

            // Register callbacks.
            _resetBregmaCalibrationButton.clicked += ResetBregmaCalibration;
        }

        private void OnDisable()
        {
            // Unregister callbacks.
            _resetBregmaCalibrationButton.clicked -= ResetBregmaCalibration;
        }

        #endregion

        #region UI Functions

        /// <summary>
        ///     Reset the Bregma calibration of the active probe.
        /// </summary>
        /// <remarks>Invariant: a Probe is selected/active, and it is controlled by Ephys Link</remarks>
        private static void ResetBregmaCalibration()
        {
            ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.ResetZeroCoordinate();
        }

        #endregion
    }
}
