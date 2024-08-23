using System;
using System.Collections.Generic;
using System.Linq;
using BrainAtlas;
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
        private ListView _targetInsertionListView;

        // Interface.
        private Button _resetBregmaCalibrationButton;

        #endregion

        #region Properties

        private readonly Dictionary<string, ProbeManager> _manipulatorIDToSelectedTargetProbeManager = new();

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
            var data = new List<string>
            {
                "None",
                "Test",
                "other test",
                "more stuff",
                "even more stuff",
                "Other"
            };
            _targetInsertionListView.bindItem = (element, index) =>
            {
                element.Q("ProbeColor").style.backgroundColor = Color.red;
                element.Q<Label>("ProbeID").text = data[index];
            };

            _targetInsertionListView.itemsSource = data;
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

        #region Helper Functions
        
        

        /// <summary>
        ///     Filter for probe managers this manipulator can target.
        ///     1. Not already selected
        ///     2. Angles are coterminal
        /// </summary>
        private IEnumerable<ProbeManager> TargetInsertionProbeManagerOptions =>
            TargetableInsertionProbeManagers.Where(manager =>
                !_manipulatorIDToSelectedTargetProbeManager
                    .Where(pair =>
                        pair.Key != ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.ManipulatorID
                    )
                    .Select(pair => pair.Value)
                    .Contains(manager)
                && IsCoterminal(
                    manager.ProbeController.Insertion.Angles,
                    ProbeManager.ActiveProbeManager.ProbeController.Insertion.Angles
                )
            );

        /// <summary>
        ///     Filter for probe managers that are targetable.<br/>
        ///     1. Are not ephys link controlled<br/>
        ///     2. Are inside the brain (not NaN)
        /// </summary>
        private static IEnumerable<ProbeManager> TargetableInsertionProbeManagers =>
            ProbeManager
                .Instances.Where(manager => !manager.IsEphysLinkControlled)
                .Where(manager =>
                    !float.IsNaN(
                        manager
                            .FindEntryIdxCoordinate(
                                BrainAtlasManager.ActiveReferenceAtlas.World2AtlasIdx(
                                    manager.ProbeController.Insertion.PositionWorldU()
                                ),
                                BrainAtlasManager.ActiveReferenceAtlas.World2Atlas_Vector(
                                    manager.ProbeController.GetTipWorldU().tipUpWorldU
                                )
                            )
                            .x
                    )
                );
        
        private static bool IsCoterminal(Vector3 first, Vector3 second)
        {
            return Mathf.Abs(first.x - second.x) % 360 < 0.01f
                && Mathf.Abs(first.y - second.y) % 360 < 0.01f
                && Mathf.Abs(first.z - second.z) % 360 < 0.01f;
        }

        #endregion
    }
}
