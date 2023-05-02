using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TrajectoryPlanner.UI.EphysLinkSettings
{
    /// <summary>
    ///     Panel representing a probe in the scene and it's binding with a manipulator.
    /// </summary>
    public class ProbeConnectionSettingsPanel : MonoBehaviour
    {
        #region Unity

        private void Start()
        {
            UpdateZeroCoordinateInputFields(ProbeManager.ManipulatorBehaviorController.ZeroCoordinateOffset);
            UpdateBrainSurfaceOffsetDropDirectionUI(ProbeManager.IsSetToDropToSurfaceWithDepth);
            UpdateBrainSurfaceOffsetValue(ProbeManager.ManipulatorBehaviorController.BrainSurfaceOffset);
        }

        #endregion

        #region Property Getters and Setters

        /// <summary>
        ///     Set probe manager reference attached to this panel.
        /// </summary>
        /// <param name="probeManager">This panel's probe's corresponding probe manager</param>
        public void SetProbeManager(ProbeManager probeManager)
        {
            ProbeManager = probeManager;

            // Set probe name
            _probeIdText.text = probeManager.UUID[..8];
            _probeIdText.color = probeManager.Color;

            // Register event functions
            ProbeManager.ManipulatorBehaviorController.ZeroCoordinateOffsetChangedEvent.AddListener(
                UpdateZeroCoordinateInputFields);
            ProbeManager.IsSetToDropToSurfaceWithDepthChangedEvent.AddListener(UpdateBrainSurfaceOffsetDropDirectionUI);
            ProbeManager.ManipulatorBehaviorController.BrainSurfaceOffsetChangedEvent.AddListener(
                UpdateBrainSurfaceOffsetValue);
        }

        private void UpdateZeroCoordinateInputFields(Vector4 offset)
        {
            _xInputField.text = offset.x.ToString(CultureInfo.CurrentCulture);
            _yInputField.text = offset.y.ToString(CultureInfo.CurrentCulture);
            _zInputField.text = offset.z.ToString(CultureInfo.CurrentCulture);
            _dInputField.text = offset.w.ToString(CultureInfo.CurrentCulture);
        }

        private void UpdateBrainSurfaceOffsetDropDirectionUI(bool withDepth)
        {
            _brainSurfaceOffsetDirectionDropdown.SetValueWithoutNotify(withDepth ? 0 : 1);
            _brainSurfaceOffsetDirectionDropdown.interactable =
                ProbeManager.ManipulatorBehaviorController.BrainSurfaceOffset == 0 ||
                ProbeManager.ManipulatorBehaviorController
                    .BrainSurfaceOffset == 0;
        }

        private void UpdateBrainSurfaceOffsetValue(float offset)
        {
            _brainSurfaceOffsetInputField.text = offset.ToString(CultureInfo.CurrentCulture);
        }

        #endregion

        #region Variables

        #region Components

        #region Serialized

        [SerializeField] private TMP_Text _probeIdText;
        [SerializeField] private TMP_Dropdown _manipulatorIdDropdown;
        [SerializeField] private TMP_InputField _xInputField;
        [SerializeField] private TMP_InputField _yInputField;
        [SerializeField] private TMP_InputField _zInputField;
        [SerializeField] private TMP_InputField _dInputField;
        [SerializeField] private TMP_Dropdown _brainSurfaceOffsetDirectionDropdown;
        [SerializeField] private TMP_InputField _brainSurfaceOffsetInputField;

        #endregion

        public ProbeManager ProbeManager { get; private set; }
        public EphysLinkSettings EphysLinkSettings { private get; set; }

        #endregion

        #region Properties

        private float _displayedBrainSurfaceOffset;

        public static UnityEvent<ProbeManager> DestroyProbeEvent { private get; set; }

        #endregion

        #endregion

        #region Component Methods

        /// <summary>
        ///     Set manipulator id dropdown options.
        /// </summary>
        /// <param name="idOptions">Available manipulators to pick from</param>
        public void SetManipulatorIdDropdownOptions(List<string> idOptions)
        {
            _manipulatorIdDropdown.ClearOptions();
            _manipulatorIdDropdown.AddOptions(idOptions);

            // Select the option corresponding to the current manipulator id
            var indexOfId = ProbeManager.IsEphysLinkControlled
                ? Math.Max(0, idOptions.IndexOf(ProbeManager.ManipulatorBehaviorController.ManipulatorID))
                : 0;
            _manipulatorIdDropdown.SetValueWithoutNotify(indexOfId);
        }

        /// <summary>
        ///     Check selected option for manipulator and disable connect button if no manipulator is selected.
        /// </summary>
        /// <param name="index">Manipulator option index that was selected (0 = no manipulator)</param>
        public void OnManipulatorDropdownValueChanged(int index)
        {
            if (ProbeManager.IsEphysLinkControlled)
                ProbeManager.SetIsEphysLinkControlled(false, "", false, () =>
                {
                    if (index != 0)
                    {
                        AttachToManipulatorAndUpdateUI();
                    }
                    else
                    {
                        EphysLinkSettings.UpdateManipulatorPanelAndSelection();

                        // Cleanup ghost prove stuff if applicable
                        if (!ProbeManager.HasGhost) return;
                        DestroyProbeEvent.Invoke(ProbeManager.GhostProbeManager);
                        ProbeManager.GhostProbeManager = null;
                    }
                });
            else
                AttachToManipulatorAndUpdateUI();

            void AttachToManipulatorAndUpdateUI()
            {
                ProbeManager.SetIsEphysLinkControlled(true,
                    _manipulatorIdDropdown.options[index].text,
                    onSuccess: () => { EphysLinkSettings.UpdateManipulatorPanelAndSelection(); });
            }
        }

        /// <summary>
        ///     Update x coordinate of zero coordinate offset.
        /// </summary>
        /// <param name="x">X coordinate</param>
        public void OnZeroCoordinateXInputFieldEndEdit(string x)
        {
            ProbeManager.ManipulatorBehaviorController.ZeroCoordinateOffset =
                new Vector4(float.Parse(x), float.NaN, float.NaN, float.NaN);
        }

        /// <summary>
        ///     Update y coordinate of zero coordinate offset.
        /// </summary>
        /// <param name="y">Y coordinate</param>
        public void OnZeroCoordinateYInputFieldEndEdit(string y)
        {
            ProbeManager.ManipulatorBehaviorController.ZeroCoordinateOffset =
                new Vector4(float.NaN, float.Parse(y), float.NaN, float.NaN);
        }

        /// <summary>
        ///     Update z coordinate of zero coordinate offset.
        /// </summary>
        /// <param name="z">Z coordinate</param>
        public void OnZeroCoordinateZInputFieldEndEdit(string z)
        {
            ProbeManager.ManipulatorBehaviorController.ZeroCoordinateOffset =
                new Vector4(float.NaN, float.NaN, float.Parse(z), float.NaN);
        }

        /// <summary>
        ///     Update depth coordinate of zero coordinate offset.
        /// </summary>
        /// <param name="d">Depth coordinate</param>
        public void OnZeroCoordinateDInputFieldEndEdit(string d)
        {
            ProbeManager.ManipulatorBehaviorController.ZeroCoordinateOffset =
                new Vector4(float.NaN, float.NaN, float.NaN, float.Parse(d));
        }

        /// <summary>
        ///     Update drop to surface direction based on dropdown selection.
        /// </summary>
        /// <param name="value">Selected direction: 0 = depth, 1 = DV</param>
        public void OnBrainSurfaceOffsetDirectionDropdownValueChanged(int value)
        {
            ProbeManager.IsSetToDropToSurfaceWithDepth = value == 0;
            ProbeManager.ManipulatorBehaviorController.IsSetToDropToSurfaceWithDepth = value == 0;
        }

        /// <summary>
        ///     Update brain surface offset based on input field value.
        /// </summary>
        /// <param name="value">Input field value</param>
        public void OnBrainSurfaceOffsetValueUpdated(string value)
        {
            ProbeManager.ManipulatorBehaviorController.BrainSurfaceOffset = float.Parse(value);
        }

        /// <summary>
        ///     Pass an increment amount to the probe manager to update the drop to surface offset.
        /// </summary>
        /// <param name="amount">Amount to increment by (negative numbers are valid)</param>
        public void IncrementBrainSurfaceOffset(float amount)
        {
            ProbeManager.ManipulatorBehaviorController.IncrementBrainSurfaceOffset(amount);
        }

        #endregion
    }
}