using System;
using System.Collections.Generic;
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
        private TP_PlayerPrefs _playerPrefs;

        #endregion

        #region Session variables

        private Dictionary<int, int> _proveIdToManipulatorId;
        private int[] _availableManipulatorIds;

        #endregion

        #endregion

        #region Setup

        private void Awake()
        {
            // Get Components
            _communicationManager = GameObject.Find("SensapexLink").GetComponent<CommunicationManager>();
            _trajectoryPlannerManager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
            _playerPrefs = GameObject.Find("main").GetComponent<TP_PlayerPrefs>();
        }

        private void OnEnable()
        {
            // Get available probes
            if (probeList.transform.childCount == _trajectoryPlannerManager.GetAllProbes().Count) return;
            foreach (var tpProbeController in _trajectoryPlannerManager.GetAllProbes())
            {
                Instantiate(probeConnectionPanelPrefab, probeList.transform);
            }
        }


        // Start is called before the first frame update
        private void Start()
        {
            UpdateConnectionUI();

            // Get available manipulator ids
            _communicationManager.GetManipulators(ids => _availableManipulatorIds = ids);
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