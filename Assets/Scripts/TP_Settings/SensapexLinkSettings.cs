using System;
using System.Collections.Generic;
using System.Linq;
using SensapexLink;
using TMPro;
using TrajectoryPlanner;
using UnityEngine;
using UnityEngine.UI;

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
        private PlayerPrefs _playerPrefs;

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
            _playerPrefs = GameObject.Find("main").GetComponent<PlayerPrefs>();
        }

        private void OnEnable()
        {
            // Get probes in scene
            var probeIds = new HashSet<int>();
            foreach (var probeManager in _trajectoryPlannerManager.GetAllProbes())
            {
                probeIds.Add(probeManager.GetID());
            }

            // Instantiate probe panel records if necessary
            _probeIdToProbeConnectionSettingsPanels ??=
                new Dictionary<int, Tuple<ProbeConnectionSettingsPanel, GameObject>>();

            // Remove probe connections if probe is not in scene anymore
            if (_probeIdToProbeConnectionSettingsPanels.Count > probeIds.Count)
            {
                foreach (var key in _probeIdToProbeConnectionSettingsPanels.Keys.Where(key => !probeIds.Contains(key)))
                {
                    Destroy(_probeIdToProbeConnectionSettingsPanels[key].Item2);
                    _probeIdToProbeConnectionSettingsPanels.Remove(key);
                }
            }
            // Add in new probe settings panels if there are new probes in the scene
            else if (_probeIdToProbeConnectionSettingsPanels.Count < probeIds.Count)
            {
                foreach (var probeId in probeIds.Where(probeId =>
                             !_probeIdToProbeConnectionSettingsPanels.ContainsKey(probeId)))
                {
                    var probeConnectionSettingsPanelGameObject =
                        Instantiate(probeConnectionPanelPrefab, probeList.transform);
                    var probeConnectionSettingsPanel =
                        probeConnectionSettingsPanelGameObject.GetComponent<ProbeConnectionSettingsPanel>();

                    probeConnectionSettingsPanel.SetProbeId(probeId);

                    _probeIdToProbeConnectionSettingsPanels.Add(probeId,
                        new Tuple<ProbeConnectionSettingsPanel, GameObject>(probeConnectionSettingsPanel,
                            probeConnectionSettingsPanelGameObject));
                }
            }
            
            // Update available manipulators
            _communicationManager.GetManipulators(availableIds =>
            {
                var manipulatorDropdownOptions = new List<string> { "-" };
                manipulatorDropdownOptions.AddRange(availableIds.Select(id => id.ToString()));
                
                foreach (var value in _probeIdToProbeConnectionSettingsPanels.Values)
                {
                    value.Item1.SetManipulatorIdDropdownOptions(manipulatorDropdownOptions);
                }
            });
        }


        // Start is called before the first frame update
        private void Start()
        {
            UpdateConnectionUI();
        }

        #endregion

        #region Helper Functions

        private void UpdateConnectionUI()
        {
            ipAddressInputField.text = _communicationManager.GetServerIp();
            portInputField.text = _communicationManager.GetServerPort() == 0
                ? ""
                : _communicationManager.GetServerPort().ToString();
            connectionErrorText.text = "";
            connectButtonText.text = _communicationManager.IsConnected() ? "Disconnect" : "Connect";
        }

        #endregion

        #region UI Functions

        public void OnConnectDisconnectPressed()
        {
            try
            {
                if (_communicationManager.IsConnected())
                {
                    _communicationManager.DisconnectFromServer(UpdateConnectionUI);
                }
                else
                {
                    connectButtonText.text = "Connecting...";
                    _communicationManager.ConnectToServer(ipAddressInputField.text, ushort.Parse(portInputField.text),
                        UpdateConnectionUI, err =>
                        {
                            connectionErrorText.text = err;
                            connectButtonText.text = "Connect";
                        }
                    );
                }
            }
            catch (Exception e)
            {
                connectionErrorText.text = e.Message;
            }
        }

        #endregion
    }
}