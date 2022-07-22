using System;
using SensapexLink;
using TMPro;
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

        #endregion

        #region Components

        private CommunicationManager _communicationManager;

        #endregion

        #endregion

        #region Setup

        private void Awake()
        {
            _communicationManager = GameObject.Find("SensapexLink").GetComponent<CommunicationManager>();
        }


        // Start is called before the first frame update
        private void Start()
        {
            UpdateUI();
        }
        #endregion

        #region Helper Functions

        private void UpdateUI()
        {
            if (_communicationManager.IsConnected())
            {
                ipAddressInputField.text = _communicationManager.GetServerIp();
                portInputField.text = _communicationManager.GetServerPort().ToString();
                connectionErrorText.text = "";
                connectButtonText.text = "Disconnect";
            }
            else
            {
                connectButtonText.text = "Connect";
            }
        }

        #endregion

        #region UI Functions

        public void OnConnectDisconnectPressed()
        {
            if (connectButtonText.text == "Connect")
            {
                _communicationManager.ConnectToServer(ipAddressInputField.text, ushort.Parse(portInputField.text));
            }
            else
            {
                // TODO: Call disconnect
            }
        }

        #endregion
    }
}