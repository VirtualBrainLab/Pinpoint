using System.Collections.Generic;
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
        private ListView _targetInsertionListView;

        // Interface.
        private Button _resetBregmaCalibrationButton;

        #endregion

        #region Properties

        private readonly Dictionary<
            string,
            ProbeManager
        > _manipulatorIDToSelectedTargetProbeManager = new();

        #endregion

        #region Unity

        private void OnEnable()
        {
            // Get components.
            _automationStackPanel = _root.Q("AutomationStackPanel");
            _resetBregmaCalibrationButton = _automationStackPanel.Q<Button>(
                "ResetBregmaCalibrationButton"
            );
            _targetInsertionListView = _automationStackPanel.Q<ListView>("TargetInsertionListView");

            // Register callbacks.
            _resetBregmaCalibrationButton.clicked += ResetBregmaCalibration;

            // Setup List.
            _targetInsertionListView.bindItem = (element, index) =>
            {
                var targetInsertionOptions = GetTargetInsertionOptions();
                element.Q("ProbeColor").style.backgroundColor = targetInsertionOptions[index].Item1;
                element.Q<Label>("ProbeID").text = targetInsertionOptions[index].Item2;
            };
            // _targetInsertionListView.itemsSource = GetTargetInsertionOptions();
            _targetInsertionListView.bindingPath = "TargetInsertionOptions";
        }

        private void OnDisable()
        {
            // Unregister callbacks.
            _resetBregmaCalibrationButton.clicked -= ResetBregmaCalibration;
        }

        // TODO: See if this can be avoided by using states (does not appear to be supported right now).
        /// <summary>
        ///     Refresh the target insertion list view while the Automation Stack is enabled.
        /// </summary>
        private void FixedUpdate()
        {
            if (!_state.IsEnabled)
                return;
            _targetInsertionListView.RefreshItems();
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


        #endregion

        #endregion
    }
}
