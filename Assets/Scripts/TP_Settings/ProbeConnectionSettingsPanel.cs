using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TP_Settings
{
    public class ProbeConnectionSettingsPanel : MonoBehaviour
    {
        #region Variables

        #region Properties

        private int _manipulatorId;
        private ProbeManager _probeManager;

        #endregion

        #region Components

        [SerializeField] private TMP_Text probeIdText;
        [SerializeField] private TMP_Dropdown manipulatorIdDropdown;
        [SerializeField] private Button connectButton;
        [SerializeField] private TMP_Text connectButtonText;
        [SerializeField] private TMP_InputField phiInputField;
        [SerializeField] private TMP_InputField thetaInputField;
        [SerializeField] private TMP_InputField spinInputField;
        [SerializeField] private TMP_InputField xInputField;
        [SerializeField] private TMP_InputField yInputField;
        [SerializeField] private TMP_InputField zInputField;
        [SerializeField] private TMP_InputField dInputField;

        #endregion

        #endregion

        #region Property Getters and Setters

        /// <summary>
        /// Set probe manager reference attached to this panel.
        /// </summary>
        /// <param name="probeManager">This panel's probe's corresponding probe manager</param>
        public void SetProbeManager(ProbeManager probeManager)
        {
            _probeManager = probeManager;

            probeIdText.text = probeManager.GetID().ToString();
            probeIdText.color = probeManager.GetColor();
        }

        /// <summary>
        /// Get probe's manipulator registration state
        /// </summary>
        /// <returns>True if the manipulator is registered, false otherwise</returns>
        public bool IsRegistered()
        {
            return _manipulatorId != 0;
        }

        /// <summary>
        /// Get probe's manipulator ID
        /// </summary>
        /// <returns>ID of the manipulator this probe is registered with</returns>
        public int GetManipulatorId()
        {
            return _manipulatorId;
        }

        /// <summary>
        /// Set probe angles by using the values in the input fields.
        /// </summary>
        public void SetAngles()
        {
            _probeManager.SetProbeAngles(new Vector3(
                float.Parse(phiInputField.text == "" ? "0" : phiInputField.text),
                float.Parse(thetaInputField.text == "" ? "0" : thetaInputField.text),
                float.Parse(spinInputField.text == "" ? "0" : spinInputField.text)
            ));
        }

        /// <summary>
        /// Set probe bregma offset by using the values in the input fields.
        /// </summary>
        public void SetBregmaOffset()
        {
            _probeManager.SetBregmaOffset(new Vector4(
                float.Parse(xInputField.text == "" ? "0" : xInputField.text),
                float.Parse(yInputField.text == "" ? "0" : yInputField.text),
                float.Parse(zInputField.text == "" ? "0" : zInputField.text),
                float.Parse(dInputField.text == "" ? "0" : dInputField.text)
            ));
        }

        #endregion

        #region Component Methods

        /// <summary>
        /// Set manipulator id dropdown options.
        /// </summary>
        /// <param name="idOptions">Available manipulators to pick from</param>
        public void SetManipulatorIdDropdownOptions(List<string> idOptions)
        {
            manipulatorIdDropdown.ClearOptions();
            manipulatorIdDropdown.AddOptions(idOptions);

            // Select the option corresponding to the current manipulator id
            var indexOfId = _probeManager.GetManipulatorId() == 0
                ? 0
                : Math.Max(0, idOptions.IndexOf(_probeManager.GetManipulatorId().ToString()));
            manipulatorIdDropdown.SetValueWithoutNotify(indexOfId);
            connectButton.interactable = indexOfId != 0;
        }

        public void OnManipulatorDropdownValueChanged(int value)
        {
            connectButton.interactable = value != 0;
        }

        /// <summary>
        /// Connect and register the selected manipulator.
        /// </summary>
        public void ConnectDisconnectProbeToManipulator()
        {
            // Connect if currently not connected
            if (_manipulatorId == 0)
            {
                _manipulatorId = int.Parse(manipulatorIdDropdown.options[manipulatorIdDropdown.value].text);
                // TODO: Put alert here to make sure manipulators are set to Bregma
                _probeManager.SetSensapexLinkMovement(true, _manipulatorId, true,
                    () => { connectButtonText.text = "Disconnect"; });
            }
            // Disconnect otherwise
            else
            {
                _probeManager.SetSensapexLinkMovement(false, _manipulatorId, false, () =>
                {
                    connectButtonText.text = "Connect";
                    _manipulatorId = 0;
                });
            }
        }

        #endregion
    }
}