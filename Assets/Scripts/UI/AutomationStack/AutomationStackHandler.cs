using UI.States;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.AutomationStack
{
    /// <summary>
    ///     Automation Stack UI origin class.<br />
    ///     This script handles getting the components, running UI setup functions, and registering callbacks.
    /// </summary>
    public partial class AutomationStackHandler : MonoBehaviour
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
        private RadioButtonGroup _targetInsertionRadioButtonGroup;

        #endregion


        #region Unity

        private void OnEnable()
        {
            // Get components.
            _automationStackPanel = _root.Q("AutomationStackPanel");
            _resetBregmaCalibrationButton = _automationStackPanel.Q<Button>(
                "ResetBregmaCalibrationButton"
            );
            _targetInsertionRadioButtonGroup = _automationStackPanel.Q<RadioButtonGroup>(
                "TargetInsertionRadioButtonGroup"
            );

            // Register callbacks.
            _resetBregmaCalibrationButton.clicked += ResetBregmaCalibration;
            _targetInsertionRadioButtonGroup.RegisterValueChangedCallback(
                OnTargetInsertionSelectionChanged
            );
        }

        private void OnDisable()
        {
            // Unregister callbacks.
            _resetBregmaCalibrationButton.clicked -= ResetBregmaCalibration;
        }

        private void FixedUpdate()
        {
            // Shortcut exit if not enabled and cleanup.
            if (!_state.IsEnabled)
            {
                FlushTargetInsertionOptionsCache();
                return;
            }

            // Update the target insertion options radio button colors.
            UpdateTargetInsertionOptionsRadioButtonColors();
        }

        #endregion

        #region Stages

        #region Bregma Calibration

        /// <summary>
        ///     Reset the Bregma calibration of the active probe.
        /// </summary>
        /// <remarks>Invariant: a Probe is selected/active, and it is controlled by Ephys Link</remarks>
        private partial void ResetBregmaCalibration();

        #endregion

        #region Target Insertion

        /// <summary>
        ///     Updates the colors of the target insertion options radio buttons to match the target probe's colors.<br />
        ///     Will only update if the cached options mismatch the current options.
        /// </summary>
        private partial void UpdateTargetInsertionOptionsRadioButtonColors();

        /// <summary>
        ///     Flush cached target insertion options.<br />
        ///     Used when switching to a probe that may not be enabled (since the cache will not be updated then). This will fix
        ///     the issue where returning to an enabled probe will not update the radio button colors.
        /// </summary>
        private partial void FlushTargetInsertionOptionsCache();

        /// <summary>
        ///     Callback for when the target insertion selection changes.<br />
        ///     Sets (or unsets) the target insertion on the probe.
        /// </summary>
        /// <param name="changeEvent">The change event holding the new selection.</param>
        /// <remarks>Invariant: The selected probe is Ephys Link controlled.</remarks>
        private partial void OnTargetInsertionSelectionChanged(ChangeEvent<int> changeEvent);

        #endregion

        #endregion
    }
}
