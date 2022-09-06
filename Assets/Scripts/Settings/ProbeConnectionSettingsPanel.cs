using System;
using System.Collections.Generic;
using System.Globalization;
using SensapexLink;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Settings
{
    public class ProbeConnectionSettingsPanel : MonoBehaviour
    {
        #region Variables

        #region Components

        #region Serialized

        [SerializeField] private TMP_Text probeIdText;
        [SerializeField] private TMP_Dropdown manipulatorIdDropdown;
        [SerializeField] private Button connectButton;
        [SerializeField] private TMP_Text connectButtonText;
        [SerializeField] private TMP_InputField xInputField;
        [SerializeField] private TMP_InputField yInputField;
        [SerializeField] private TMP_InputField zInputField;
        [SerializeField] private TMP_InputField dInputField;
        [SerializeField] private TMP_Dropdown brainSurfaceOffsetDirectionDropdown;
        [SerializeField] private TMP_InputField brainSurfaceOffsetInputField;

        #endregion

        private CommunicationManager _communicationManager;
        private ProbeManager _probeManager;
        private TP_QuestionDialogue _questionDialogue;

        #endregion

        #region Properties

        private Vector4 _displayedZeroCoordinateOffset;
        private float _displayedBrainSurfaceOffset;

        #endregion

        #endregion

        #region Unity

        /// <summary>
        ///     Initialize components
        /// </summary>
        private void Awake()
        {
            _communicationManager = GameObject.Find("SensapexLink").GetComponent<CommunicationManager>();
            _questionDialogue = GameObject.Find("MainCanvas").transform.Find("QuestionDialoguePanel").gameObject
                .GetComponent<TP_QuestionDialogue>();
        }

        /// <summary>
        ///     Configure input field submissions
        /// </summary>
        private void Start()
        {
            brainSurfaceOffsetInputField.onEndEdit.AddListener(delegate
            {
                _probeManager.SetBrainSurfaceOffset(float.Parse(brainSurfaceOffsetInputField.text));
            });
        }

        /// <summary>
        ///     Update values as they change
        /// </summary>
        private void FixedUpdate()
        {
            connectButton.interactable = _communicationManager.IsConnected() && manipulatorIdDropdown.value > 0;
            connectButtonText.text = _probeManager.IsConnectedToManipulator() ? "Disconnect" : "Connect";

            if (!_probeManager.IsConnectedToManipulator()) return;
            // Update display for zero coordinate offset
            if (_probeManager.GetZeroCoordinateOffset() != _displayedZeroCoordinateOffset)
            {
                _displayedZeroCoordinateOffset = _probeManager.GetZeroCoordinateOffset();
                xInputField.text = _displayedZeroCoordinateOffset.x.ToString(CultureInfo.CurrentCulture);
                yInputField.text = _displayedZeroCoordinateOffset.y.ToString(CultureInfo.CurrentCulture);
                zInputField.text = _displayedZeroCoordinateOffset.z.ToString(CultureInfo.CurrentCulture);
                dInputField.text = _displayedZeroCoordinateOffset.w.ToString(CultureInfo.CurrentCulture);
            }

            // Update brain surface offset drop direction dropdown
            if (_probeManager.IsSetToDropToSurfaceWithDepth() != (brainSurfaceOffsetDirectionDropdown.value == 0))
                brainSurfaceOffsetDirectionDropdown.SetValueWithoutNotify(
                    _probeManager.IsSetToDropToSurfaceWithDepth() ? 0 : 1);

            // Update display for brain surface offset
            if (!(Math.Abs(_probeManager.GetBrainSurfaceOffset() - _displayedBrainSurfaceOffset) > 0.001f)) return;
            _displayedBrainSurfaceOffset = _probeManager.GetBrainSurfaceOffset();
            brainSurfaceOffsetInputField.text =
                _displayedBrainSurfaceOffset.ToString(CultureInfo.CurrentCulture);
        }

        #endregion


        #region Property Getters and Setters

        /// <summary>
        ///     Set probe manager reference attached to this panel.
        /// </summary>
        /// <param name="probeManager">This panel's probe's corresponding probe manager</param>
        public void SetProbeManager(ProbeManager probeManager)
        {
            _probeManager = probeManager;

            probeIdText.text = probeManager.GetID().ToString();
            probeIdText.color = probeManager.GetColor();
        }

        /// <summary>
        ///     Return attached probe manager.
        /// </summary>
        /// <returns>Probe manager for related probe</returns>
        public ProbeManager GetProbeManager()
        {
            return _probeManager;
        }

        /// <summary>
        ///     Set probe zero coordinate offset by using the values in the input fields.
        /// </summary>
        public void SetZeroCoordinateOffset()
        {
            _displayedZeroCoordinateOffset = new Vector4(
                float.Parse(xInputField.text == "" ? "0" : xInputField.text),
                float.Parse(yInputField.text == "" ? "0" : yInputField.text),
                float.Parse(zInputField.text == "" ? "0" : zInputField.text),
                float.Parse(dInputField.text == "" ? "0" : dInputField.text)
            );
            _probeManager.SetZeroCoordinateOffset(_displayedZeroCoordinateOffset);
        }

        #endregion

        #region Component Methods

        /// <summary>
        ///     Set manipulator id dropdown options.
        /// </summary>
        /// <param name="idOptions">Available manipulators to pick from</param>
        public void SetManipulatorIdDropdownOptions(List<string> idOptions)
        {
            manipulatorIdDropdown.ClearOptions();
            manipulatorIdDropdown.AddOptions(idOptions);

            // Select the option corresponding to the current manipulator id
            var indexOfId = _probeManager.IsConnectedToManipulator()
                ? Math.Max(0, idOptions.IndexOf(_probeManager.GetManipulatorId().ToString()))
                : 0;
            manipulatorIdDropdown.SetValueWithoutNotify(indexOfId);
            connectButton.interactable = indexOfId != 0;
        }

        /// <summary>
        ///     Check selected option for manipulator and disable connect button if no manipulator is selected.
        /// </summary>
        /// <param name="value">Manipulator option that was selected (0 = no manipulator)</param>
        public void OnManipulatorDropdownValueChanged(int value)
        {
            connectButton.interactable = value != 0;
        }

        /// <summary>
        ///     Connect and register the selected manipulator.
        /// </summary>
        public void ConnectDisconnectProbeToManipulator()
        {
            // Disconnect if already connected
            if (_probeManager.IsConnectedToManipulator())
            {
                _probeManager.SetSensapexLinkMovement(false, 0, false, () => { connectButtonText.text = "Connect"; });
            }
            // Connect otherwise
            else
            {
                _probeManager.SetSensapexLinkMovement(true,
                    int.Parse(manipulatorIdDropdown.options[manipulatorIdDropdown.value].text), true,
                    () => { connectButtonText.text = "Disconnect"; });
            }
        }

        /// <summary>
        ///     Update drop to surface direction based on dropdown selection
        /// </summary>
        /// <param name="value">Selected direction: 0 = depth, 1 = DV</param>
        public void OnBrainSurfaceOffsetDirectionDropdownValueChanged(int value)
        {
            _probeManager.SetDropToSurfaceWithDepth(value == 0);
        }

        /// <summary>
        ///     Pass an increment amount to the probe manager to update the drop to surface offset
        /// </summary>
        /// <param name="amount">Amount to increment by (negative numbers are valid)</param>
        public void IncrementBrainSurfaceOffset(float amount)
        {
            _probeManager.IncrementBrainSurfaceOffset(amount);
        }

        #endregion
    }
}