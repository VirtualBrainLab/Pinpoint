using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrajectoryPlanner.UI.EphysLinkSettings
{
    /// <summary>
    ///     Panel representing an available manipulator to connect to and its settings.
    /// </summary>
    public class ManipulatorConnectionPanel : MonoBehaviour
    {
        #region Constructor

        public void Initialize(EphysLinkSettings settingsMenu, string manipulatorID)
        {
            // Set properties
            _ephysLinkSettings = settingsMenu;
            _manipulatorId = manipulatorID;

            // Initialize components
            _manipulatorIdText.text = manipulatorID;
            UpdateLinkableProbeOptions();

            // Get attached probe (could be null)
            _attachedProbe = ProbeManager.Instances.Find(manager => manager.IsEphysLinkControlled &&
                                                                    manager.ManipulatorBehaviorController
                                                                        .ManipulatorID == manipulatorID);

            // Apply handedness from memory or default to right handed
            if (_attachedProbe)
                _handednessDropdown.value = _attachedProbe.ManipulatorBehaviorController.IsRightHanded ? 1 : 0;
            else
                _handednessDropdown.value =
                    Settings.EphysLinkRightHandedManipulators.Split("\n").Contains(manipulatorID) ? 1 : 0;

            // Register event listeners for updating probes list
            settingsMenu.ShouldUpdateProbesListEvent.AddListener(UpdateLinkableProbeOptions);
        }

        #endregion

        #region UI Function

        /// <summary>
        ///     Handle changing manipulator's registered handedness on UI change.
        /// </summary>
        /// <param name="value">Selected index of the handedness options (0 = left handed, 1 = right handed)</param>
        public void OnManipulatorHandednessValueChanged(int value)
        {
            // Set handedness on attached probe if it exists
            if (_attachedProbe) _attachedProbe.ManipulatorBehaviorController.IsRightHanded = value == 1;

            // Update handedness in settings
            var currentRightHandedManipulators = Settings.EphysLinkRightHandedManipulators.Split("\n").ToList();
            if (currentRightHandedManipulators.Contains(_manipulatorId) && value == 0)
                currentRightHandedManipulators.Remove(_manipulatorId);
            else if (!currentRightHandedManipulators.Contains(_manipulatorId) && value == 1)
                currentRightHandedManipulators.Add(_manipulatorId);

            Settings.EphysLinkRightHandedManipulators = string.Join("\n", currentRightHandedManipulators);
        }

        public void OnLinkedProbeSelectionChanged(int value)
        {
            if (value == 0)
            {
                // With values != 0, there definitely was an attached probe before
                _attachedProbe.SetIsEphysLinkControlled(false, _manipulatorId, onSuccess: () =>
                {
                    _ephysLinkSettings.LinkedProbes.Remove(_attachedProbe);
                    _attachedProbe = null;

                    // Inform others a change was made
                    _ephysLinkSettings.InvokeShouldUpdateProbesListEvent();
                });
            }
            else
            {
                // Disconnect currently attached probe
                if (_attachedProbe)
                {
                    _attachedProbe.SetIsEphysLinkControlled(false,
                        onSuccess: () => { _ephysLinkSettings.LinkedProbes.Remove(_attachedProbe); });
                }

                // Find the new probe and attach it
                var selectedProbeUUID = _linkedProbeDropdown.options[value].text;
                var newProbeManager = ProbeManager.Instances.Find(manager => manager.UUID == selectedProbeUUID);
                newProbeManager.SetIsEphysLinkControlled(true, _manipulatorId, onSuccess: () =>
                {
                    _attachedProbe = newProbeManager;
                    _ephysLinkSettings.LinkedProbes.Add(_attachedProbe);

                    // Inform others a change was made
                    _ephysLinkSettings.InvokeShouldUpdateProbesListEvent();
                });
            }
        }

        /// <summary>
        ///     Updates the list of linkable probes and re-selects the previously selected probe if it is still available.
        /// </summary>
        private void UpdateLinkableProbeOptions()
        {
            // Capture current selection (either from the previous selection or from the attached probe)
            var previouslyLinkedProbeUUID = _attachedProbe
                ? _attachedProbe.UUID
                : "";

            // Repopulate dropdown options
            _linkedProbeDropdown.ClearOptions();
            var availableProbes = ProbeManager.Instances.Where(manager =>
                    manager.UUID == previouslyLinkedProbeUUID || !_ephysLinkSettings.LinkedProbes.Contains(manager))
                .Select(manager => manager.UUID).ToList();
            availableProbes.Insert(0, "None");
            _linkedProbeDropdown.AddOptions(availableProbes);

            // Select previously selected probe if it exists
            _linkedProbeDropdown.value = Mathf.Max(0, availableProbes.IndexOf(previouslyLinkedProbeUUID));
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

        private ProbeManager _attachedProbe;

        #endregion

        #region Properties

        private EphysLinkSettings _ephysLinkSettings;
        private string _manipulatorId;

        #endregion
    }
}