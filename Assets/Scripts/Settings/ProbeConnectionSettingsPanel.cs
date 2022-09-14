using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Settings
{
    public class ProbeConnectionSettingsPanel : MonoBehaviour
    {
        #region Variables

        #region Components

        #region Serialized

        [SerializeField] private TMP_Text probeIdText;
        [SerializeField] private TMP_Dropdown manipulatorIdDropdown;
        [SerializeField] private TMP_InputField xInputField;
        [SerializeField] private TMP_InputField yInputField;
        [SerializeField] private TMP_InputField zInputField;
        [SerializeField] private TMP_InputField dInputField;
        [SerializeField] private TMP_Dropdown brainSurfaceOffsetDirectionDropdown;
        [SerializeField] private TMP_InputField brainSurfaceOffsetInputField;

        #endregion

        private ProbeManager _probeManager;
        private EphysLinkSettings _ephysLinkSettings;

        #endregion

        #region Properties

        private Vector4 _displayedZeroCoordinateOffset;
        private float _displayedBrainSurfaceOffset;

        #endregion

        #endregion

        #region Unity


        /// <summary>
        ///     Update values as they change
        /// </summary>
        private void FixedUpdate()
        {
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
        
        public ProbeManager GetProbeManager()
        {
            return _probeManager;
        }
        
        public void SetEphysLinkSettings(EphysLinkSettings ephysLinkSettings)
        {
            _ephysLinkSettings = ephysLinkSettings;
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
        }

        /// <summary>
        ///     Check selected option for manipulator and disable connect button if no manipulator is selected.
        /// </summary>
        /// <param name="value">Manipulator option that was selected (0 = no manipulator)</param>
        public void OnManipulatorDropdownValueChanged(int value)
        {
            // Disconnect if already connected
            if (_probeManager.IsConnectedToManipulator())
                _probeManager.SetEphysLinkMovement(false, 0, false);

            // Connect if a manipulator is selected
            if (value != 0)
                _probeManager.SetEphysLinkMovement(true,
                    int.Parse(manipulatorIdDropdown.options[value].text));
            _ephysLinkSettings.UpdateManipulatorPanelAndSelection();
        }

        /// <summary>
        ///     Update x coordinate of zero coordinate offset
        /// </summary>
        /// <param name="x">X coordinate</param>
        public void OnZeroCoordinateXInputFieldEndEdit(string x)
        {
            _probeManager.SetZeroCoordinateOffsetX(float.Parse(x));
        }

        /// <summary>
        ///     Update y coordinate of zero coordinate offset
        /// </summary>
        /// <param name="y">Y coordinate</param>
        public void OnZeroCoordinateYInputFieldEndEdit(string y)
        {
            _probeManager.SetZeroCoordinateOffsetY(float.Parse(y));
        }

        /// <summary>
        ///     Update z coordinate of zero coordinate offset
        /// </summary>
        /// <param name="z">Z coordinate</param>
        public void OnZeroCoordinateZInputFieldEndEdit(string z)
        {
            _probeManager.SetZeroCoordinateOffsetZ(float.Parse(z));
        }

        /// <summary>
        ///     Update depth coordinate of zero coordinate offset
        /// </summary>
        /// <param name="d">Depth coordinate</param>
        public void OnZeroCoordinateDInputFieldEndEdit(string d)
        {
            _probeManager.SetZeroCoordinateOffsetDepth(float.Parse(d));
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
        ///     Update brain surface offset based on input field value
        /// </summary>
        /// <param name="value">Input field value</param>
        public void OnBrainSurfaceOffsetValueUpdated(string value)
        {
            _probeManager.SetBrainSurfaceOffset(float.Parse(value));
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