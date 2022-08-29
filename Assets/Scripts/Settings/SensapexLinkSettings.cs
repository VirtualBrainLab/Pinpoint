using System;
using System.Collections.Generic;
using System.Linq;
using SensapexLink;
using TMPro;
using TrajectoryPlanner;
using UnityEngine;

namespace TP_Settings
{
    public class SensapexLinkSettings : MonoBehaviour
    {
        #region Variables

        #region Serialized Fields

        // Server connection
        [SerializeField] private TMP_InputField ipAddressInputField;
        [SerializeField] private TMP_InputField portInputField;
        [SerializeField] private TMP_Text connectionErrorText;
        [SerializeField] private TMP_Text connectButtonText;

        // Probes in scene
        [SerializeField] private GameObject probeList;
        [SerializeField] private GameObject probeConnectionPanelPrefab;

        #endregion

        #region Components

        private CommunicationManager _communicationManager;
        private TrajectoryPlannerManager _trajectoryPlannerManager;
        private TP_QuestionDialogue _questionDialogue;

        #endregion

        #region Session variables

        private Dictionary<int, Tuple<ProbeConnectionSettingsPanel, GameObject>>
            _probeIdToProbeConnectionSettingsPanels;

        #endregion

        #endregion

        #region Setup

        private void Awake()
        {
            // Get Components
            _communicationManager = GameObject.Find("SensapexLink").GetComponent<CommunicationManager>();
            _trajectoryPlannerManager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
            _questionDialogue = GameObject.Find("MainCanvas").transform.Find("QuestionDialoguePanel").gameObject
                .GetComponent<TP_QuestionDialogue>();

            // Initialize session variables
            _probeIdToProbeConnectionSettingsPanels =
                new Dictionary<int, Tuple<ProbeConnectionSettingsPanel, GameObject>>();
        }

        private void OnEnable()
        {
            var handledProbeIds = new HashSet<int>();

            // Add any new probes in scene to list
            foreach (var probeManager in _trajectoryPlannerManager.GetAllProbes())
            {
                var probeId = probeManager.GetID();

                // Create probe connection settings panel if the probe is new
                if (!_probeIdToProbeConnectionSettingsPanels.ContainsKey(probeId))
                {
                    var probeConnectionSettingsPanelGameObject =
                        Instantiate(probeConnectionPanelPrefab, probeList.transform);
                    var probeConnectionSettingsPanel =
                        probeConnectionSettingsPanelGameObject.GetComponent<ProbeConnectionSettingsPanel>();

                    probeConnectionSettingsPanel.SetProbeManager(probeManager);

                    _probeIdToProbeConnectionSettingsPanels.Add(probeId,
                        new Tuple<ProbeConnectionSettingsPanel, GameObject>(probeConnectionSettingsPanel,
                            probeConnectionSettingsPanelGameObject));
                }
                else
                {
                    // Update probeManager in probe connection settings panel
                    _probeIdToProbeConnectionSettingsPanels[probeId].Item1.SetProbeManager(probeManager);
                }

                handledProbeIds.Add(probeId);
            }

            // Remove any probe that is not in the scene anymore
            foreach (var removedProbeId in _probeIdToProbeConnectionSettingsPanels.Keys.Where(key =>
                         !handledProbeIds.Contains(key)))
            {
                // Remove
                Destroy(_probeIdToProbeConnectionSettingsPanels[removedProbeId].Item2);
                _probeIdToProbeConnectionSettingsPanels.Remove(removedProbeId);
            }

            // Update available manipulators
            _communicationManager.GetManipulators(availableIds =>
            {
                var manipulatorDropdownOptions = new List<string> { "-" };
                manipulatorDropdownOptions.AddRange(availableIds.Select(id => id.ToString()));

                foreach (var value in _probeIdToProbeConnectionSettingsPanels.Values)
                    value.Item1.SetManipulatorIdDropdownOptions(manipulatorDropdownOptions);
            });
        }


        // Start is called before the first frame update
        private void Start()
        {
            UpdateConnectionUI();
        }

        #endregion

        #region UI Functions

        /// <summary>
        ///     Populate UI elements with current connection settings
        /// </summary>
        private void UpdateConnectionUI()
        {
            connectionErrorText.text = "";
            connectButtonText.text = _communicationManager.IsConnected() ? "Disconnect" : "Connect";
        }

        /// <summary>
        ///     Handle when connect/disconnect button is pressed
        /// </summary>
        public void OnConnectDisconnectPressed()
        {
            if (!_communicationManager.IsConnected())
            {
                try
                {
                    connectButtonText.text = "Connecting...";
                    _communicationManager.ConnectToServer(ipAddressInputField.text, int.Parse(portInputField.text),
                        UpdateConnectionUI, err =>
                        {
                            connectionErrorText.text = err;
                            connectButtonText.text = "Connect";
                        }
                    );
                }
                catch (Exception e)
                {
                    connectionErrorText.text = e.Message;
                }
            }
            else
            {
                _questionDialogue.SetYesCallback(() =>
                {
                    foreach (var probeManager in _trajectoryPlannerManager.GetAllProbes()
                                 .Where(probeManager => probeManager.IsConnectedToManipulator()))
                        probeManager.ResetManipulatorProperties();

                    _communicationManager.DisconnectFromServer(UpdateConnectionUI);
                });

                _questionDialogue.NewQuestion(
                    "Are you sure you want to disconnect?\nAll incomplete movements will be canceled.");
            }
        }

        #endregion
    }
}