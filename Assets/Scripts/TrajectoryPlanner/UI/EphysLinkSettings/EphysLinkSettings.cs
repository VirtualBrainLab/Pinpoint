using System;
using System.Collections.Generic;
using System.Linq;
using EphysLink;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TrajectoryPlanner.UI.EphysLinkSettings
{
    /// <summary>
    ///     Settings menu to connect to the Ephys Link server and manage probe-manipulator bindings.
    /// </summary>
    public class EphysLinkSettings : MonoBehaviour
    {
        #region Components

        // Server connection
        [SerializeField] private TMP_InputField _ipAddressInputField;
        [SerializeField] private InputField _portInputField;
        [SerializeField] private Text _connectButtonText;
        [SerializeField] private TMP_Text _connectionErrorText;

        // Manipulators
        [SerializeField] private GameObject _manipulatorList;
        [SerializeField] private GameObject _manipulatorConnectionPanelPrefab;
        [SerializeField] private Button _copilotButton;
        [SerializeField] private Text _copilotButtonText;

        private UIManager _uiManager;

        #endregion

        #region Properties

        private bool _isEphysLinkCompatible;

        private bool _ephysCopilotIsEnabled => _copilotButtonText.text.Contains("Hide");


        private readonly Dictionary<string, (ManipulatorConnectionPanel manipulatorConnectionSettingsPanel,
                GameObject gameObject)>
            _manipulatorIdToManipulatorConnectionSettingsPanel = new();

        public HashSet<ProbeManager> LinkedProbes { get; } = new();
        public UnityEvent ShouldUpdateProbesListEvent { get; } = new();

        #endregion

        #region Unity

        private void Awake()
        {
            // Get/Set Components
            _uiManager = GameObject.Find("MainCanvas").GetComponent<UIManager>();
        }

        private void OnEnable()
        {
            // Update UI elements every time the settings panel is opened
            UpdateConnectionPanel();
        }

        #endregion

        #region UI Functions

        /// <summary>
        ///     Populate UI elements with current connection settings.
        /// </summary>
        private void UpdateConnectionPanel()
        {
            if (CommunicationManager.Instance.IsConnected && !_isEphysLinkCompatible)
            {
                _connectionErrorText.text =
                    "Ephys Link is outdated. Please update to " + CommunicationManager.EPHYS_LINK_MIN_VERSION_STRING;
                _connectButtonText.text = "Connect";
                CommunicationManager.Instance.DisconnectFromServer();
                return;
            }

            // Connection UI
            _connectionErrorText.text = "";
            _connectButtonText.text = CommunicationManager.Instance.IsConnected ? "Disconnect" : "Connect";

            // Update Manipulator Panels
            UpdateManipulatorPanels();
        }

        private void UpdateManipulatorPanels()
        {
            // Default Copilot to be disabled unless the right manipulator type is found
            _copilotButton.interactable = false;

            if (CommunicationManager.Instance.IsConnected)
            {
                // FIXME: Dependent on Manipulator Type. Should be standardized by Ephys Link.
                CommunicationManager.Instance.GetManipulators((availableIDs, type) =>
                {
                    // Enable Copilot button if using Sensapex or New Scale
                    _copilotButton.interactable = !type.Contains("pathway");

                    // Keep track of handled manipulator panels
                    var handledManipulatorIds = new HashSet<string>();

                    // Add any new manipulators in scene to list
                    foreach (var manipulatorID in availableIDs)
                    {
                        // Create new manipulator connection settings panel if the manipulator is new
                        if (!_manipulatorIdToManipulatorConnectionSettingsPanel.ContainsKey(manipulatorID))
                        {
                            // Instantiate panel
                            var manipulatorConnectionSettingsPanelGameObject =
                                Instantiate(_manipulatorConnectionPanelPrefab, _manipulatorList.transform);
                            var manipulatorConnectionSettingsPanel =
                                manipulatorConnectionSettingsPanelGameObject
                                    .GetComponent<ManipulatorConnectionPanel>();

                            // Set manipulator id
                            manipulatorConnectionSettingsPanel.Initialize(this, manipulatorID, type);

                            // Add to dictionary
                            _manipulatorIdToManipulatorConnectionSettingsPanel.Add(manipulatorID,
                                new ValueTuple<ManipulatorConnectionPanel, GameObject>(
                                    manipulatorConnectionSettingsPanel, manipulatorConnectionSettingsPanelGameObject));
                        }

                        // Mark ID as handled
                        handledManipulatorIds.Add(manipulatorID);
                    }

                    // Remove any manipulators that are not connected anymore
                    foreach (var disconnectedManipulator in _manipulatorIdToManipulatorConnectionSettingsPanel.Keys
                                 .Except(handledManipulatorIds).ToList())
                    {
                        _manipulatorIdToManipulatorConnectionSettingsPanel.Remove(disconnectedManipulator);
                        Destroy(_manipulatorIdToManipulatorConnectionSettingsPanel[disconnectedManipulator].gameObject);
                    }

                    // Reorder panels to match order of availableIds
                    foreach (var manipulatorId in availableIDs)
                        _manipulatorIdToManipulatorConnectionSettingsPanel[manipulatorId].gameObject.transform
                            .SetAsLastSibling();
                });
            }
            else
            {
                // Clear manipulator panels if not connected
                foreach (var manipulatorPanel in
                         _manipulatorIdToManipulatorConnectionSettingsPanel.Values.Select(value => value.gameObject))
                    Destroy(manipulatorPanel);
                _manipulatorIdToManipulatorConnectionSettingsPanel.Clear();
            }
        }

        /// <summary>
        ///     Handle when connect/disconnect button is pressed.
        /// </summary>
        public void OnConnectDisconnectPressed()
        {
            if (!CommunicationManager.Instance.IsConnected)
            {
                // Attempt to connect to server
                try
                {
                    _connectButtonText.text = "Connecting...";
                    CommunicationManager.Instance.ConnectToServer(_ipAddressInputField.text,
                        int.Parse(_portInputField.text),
                        () =>
                        {
                            // Check Ephys Link version
                            CommunicationManager.Instance.GetVersion(version =>
                            {
                                if (!version.Split(".").Where((versionNumber, index) =>
                                            int.Parse(versionNumber) <
                                            CommunicationManager.EPHYS_LINK_MIN_VERSION[index])
                                        .Any())
                                {
                                    // Ephys Link is current enough
                                    _isEphysLinkCompatible = true;
                                    UpdateConnectionPanel();
                                }
                                else
                                    // Ephys Link needs updating
                                {
                                    CommunicationManager.Instance.DisconnectFromServer(() =>
                                    {
                                        _connectionErrorText.text =
                                            "Ephys Link is outdated. Please update to " +
                                            CommunicationManager.EPHYS_LINK_MIN_VERSION_STRING;
                                        _connectButtonText.text = "Connect";
                                    });
                                }
                            }, () =>
                            {
                                // Failed to get version (probably because it's outdated)
                                CommunicationManager.Instance.DisconnectFromServer(() =>
                                {
                                    _connectionErrorText.text =
                                        "Unable to get server version. Please ensure Ephys Link version is " +
                                        CommunicationManager.EPHYS_LINK_MIN_VERSION_STRING;
                                    _connectButtonText.text = "Connect";
                                });
                            });
                        }, err =>
                        {
                            _connectionErrorText.text = err;
                            _connectButtonText.text = "Connect";
                        }
                    );
                }
                catch (Exception e)
                {
                    _connectionErrorText.text = e.Message;
                }
            }
            else
            {
                // Disconnect from server
                QuestionDialogue.SetYesCallback(() =>
                {
                    foreach (var probeManager in ProbeManager.Instances
                                 .Where(probeManager => probeManager.IsEphysLinkControlled))
                        probeManager.SetIsEphysLinkControlled(false);

                    CommunicationManager.Instance.DisconnectFromServer(UpdateConnectionPanel);
                });

                QuestionDialogue.NewQuestion(
                    "Are you sure you want to disconnect?\nAll incomplete movements will be canceled.");
            }
        }

        /// <summary>
        ///     Toggle Ephys Copilot panel
        /// </summary>
        public void ToggleCopilotPanel()
        {
            _copilotButtonText.text = !_ephysCopilotIsEnabled
                ? "Hide Ephys Copilot"
                : "Show Ephys Copilot";
            _uiManager.EnableEphysCopilotPanel(_ephysCopilotIsEnabled);
        }

        public void InvokeShouldUpdateProbesListEvent()
        {
            ShouldUpdateProbesListEvent.Invoke();
        }

        #endregion
    }
}