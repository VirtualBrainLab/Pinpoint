using System.Linq;
using TMPro;
using TrajectoryPlanner.Probes;
using UnityEngine;
using UnityEngine.UI;

namespace TrajectoryPlanner.UI.EphysLinkSettings
{
    /// <summary>
    ///     Panel representing an available manipulator to connect to and its settings.
    /// </summary>
    public class ManipulatorConnectionSettingsPanel : MonoBehaviour
    {
        #region Constructor

        /// <summary>
        ///     Set the manipulator ID this panel is representing.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator this panel is representing</param>
        public void SetManipulatorId(string manipulatorId)
        {
            _manipulatorIdText.text = manipulatorId;
            // _manipulatorBehaviorController = ProbeManager.Instances
            //     .Find(manager => manager.ManipulatorBehaviorController.ManipulatorID == manipulatorId)
            //     .ManipulatorBehaviorController;
            // _handednessDropdown.value = _manipulatorBehaviorController.IsRightHanded ? 1 : 0;
        }

        public void Initialize(EphysLinkSettings settingsMenu, string manipulatorID)
        {
            // Set properties
            _ephysLinkSettings = settingsMenu;
            _manipulatorId = manipulatorID;

            // Initialize components
            _manipulatorIdText.text = manipulatorID;
            UpdateLinkableProbeOptions();

            var attachedProbe = ProbeManager.Instances.Find(manager =>
                manager.ManipulatorBehaviorController.ManipulatorID == manipulatorID);
            if (attachedProbe)
            {
                _handednessDropdown.value = attachedProbe.ManipulatorBehaviorController.IsRightHanded ? 1 : 0;
            }
            else
            {
                _handednessDropdown.value =
                    Settings.EphysLinkRightHandedManipulators.Split("\n").Contains(manipulatorID) ? 1 : 0;
            }
        }

        #endregion

        #region UI Function

        /// <summary>
        ///     Handle changing manipulator's registered handedness on UI change.
        /// </summary>
        /// <param name="value">Selected index of the handedness options (0 = left handed, 1 = right handed)</param>
        public void OnManipulatorHandednessValueChanged(int value)
        {
            _manipulatorBehaviorController.IsRightHanded = value == 1;
        }

        /// <summary>
        /// Updates the list of linkable probes and re-selects the previously selected probe if it is still available.
        /// </summary>
        private void UpdateLinkableProbeOptions()
        {
            // Capture current selection
            var previouslyLinkedProbe = _linkedProbeDropdown.options[_linkedProbeDropdown.value].text;
            
            // Repopulate dropdown options
            _linkedProbeDropdown.ClearOptions();
            var availableProbes = ProbeManager.Instances.Except(_ephysLinkSettings.LinkedProbes)
                .Select(manager => manager.UUID).ToList();
            _linkedProbeDropdown.AddOptions(availableProbes);
            
            // Select attached probe if it exists
            _linkedProbeDropdown.value = Mathf.Max(0, availableProbes.IndexOf(previouslyLinkedProbe));
        }

        #endregion

        #region Components

        [SerializeField] private TMP_Text _manipulatorIdText;
        [SerializeField] private Dropdown _handednessDropdown;
        [SerializeField] private Dropdown _linkedProbeDropdown;

        [SerializeField] private GameObject _probePropertiesSection;
        [SerializeField] private InputField _zeroCoordinateXInputField;
        [SerializeField] private InputField _zeroCoordinateYInputField;
        [SerializeField] private InputField _zeroCoordinateZInputField;
        [SerializeField] private InputField _zeroCoordinateDInputField;
        [SerializeField] private InputField _brainOffsetInputField;

        private ManipulatorBehaviorController _manipulatorBehaviorController;

        #endregion

        #region Properties

        private EphysLinkSettings _ephysLinkSettings;
        private string _manipulatorId;

        #endregion
    }
}