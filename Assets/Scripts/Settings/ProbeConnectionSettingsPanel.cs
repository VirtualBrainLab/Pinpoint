using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using TrajectoryPlanner;
using UnityEngine;

namespace Settings
{
    /// <summary>
    ///     Panel representing a probe in the scene and it's binding with a manipulator.
    /// </summary>
    public class ProbeConnectionSettingsPanel : MonoBehaviour
    {
        #region Property Getters and Setters

        /// <summary>
        ///     Set probe manager reference attached to this panel.
        /// </summary>
        /// <param name="probeManager">This panel's probe's corresponding probe manager</param>
        public void SetProbeManager(ProbeManager probeManager)
        {
            ProbeManager = probeManager;

            probeIdText.text = probeManager.ProbeID.ToString();
            probeIdText.color = probeManager.GetColor();
        }

        #endregion

        #region Unity

        private void Awake()
        {
            _trajectoryPlannerManager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
        }

        /// <summary>
        ///     Update values as they change.
        /// </summary>
        private void FixedUpdate()
        {
            if (!ProbeManager.IsEphysLinkControlled) return;
            // Update display for zero coordinate offset
            if (ProbeManager.ZeroCoordinateOffset != _displayedZeroCoordinateOffset)
            {
                _displayedZeroCoordinateOffset = ProbeManager.ZeroCoordinateOffset;
                xInputField.text = _displayedZeroCoordinateOffset.x.ToString(CultureInfo.CurrentCulture);
                yInputField.text = _displayedZeroCoordinateOffset.y.ToString(CultureInfo.CurrentCulture);
                zInputField.text = _displayedZeroCoordinateOffset.z.ToString(CultureInfo.CurrentCulture);
                dInputField.text = _displayedZeroCoordinateOffset.w.ToString(CultureInfo.CurrentCulture);
            }

            // Update brain surface offset drop direction dropdown
            if (ProbeManager.IsSetToDropToSurfaceWithDepth != (brainSurfaceOffsetDirectionDropdown.value == 0))
                brainSurfaceOffsetDirectionDropdown.SetValueWithoutNotify(
                    ProbeManager.IsSetToDropToSurfaceWithDepth ? 0 : 1);

            // Enable/disable interactivity of brain surface offset axis
            if (ProbeManager.CanChangeBrainSurfaceOffsetAxis != brainSurfaceOffsetDirectionDropdown.interactable)
                brainSurfaceOffsetDirectionDropdown.interactable = ProbeManager.CanChangeBrainSurfaceOffsetAxis;

            // Update display for brain surface offset
            if (!(Math.Abs(ProbeManager.BrainSurfaceOffset - _displayedBrainSurfaceOffset) > 0.001f)) return;
            _displayedBrainSurfaceOffset = ProbeManager.BrainSurfaceOffset;
            brainSurfaceOffsetInputField.text =
                _displayedBrainSurfaceOffset.ToString(CultureInfo.CurrentCulture);
        }

        #endregion

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

        public ProbeManager ProbeManager { get; private set; }
        public EphysLinkSettings EphysLinkSettings { private get; set; }
        private TrajectoryPlannerManager _trajectoryPlannerManager;

        #endregion

        #region Properties

        private Vector4 _displayedZeroCoordinateOffset;
        private float _displayedBrainSurfaceOffset;

        #endregion

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
            var indexOfId = ProbeManager.IsEphysLinkControlled
                ? Math.Max(0, idOptions.IndexOf(ProbeManager.ManipulatorId))
                : 0;
            manipulatorIdDropdown.SetValueWithoutNotify(indexOfId);
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
                        _trajectoryPlannerManager.DestroyProbe(ProbeManager.GhostProbeManager);
                        ProbeManager.GhostProbeManager = null;
                    }
                });
            else
                AttachToManipulatorAndUpdateUI();

            void AttachToManipulatorAndUpdateUI()
            {
                ProbeManager.SetIsEphysLinkControlled(true,
                    manipulatorIdDropdown.options[index].text,
                    onSuccess: () => { EphysLinkSettings.UpdateManipulatorPanelAndSelection(); });
            }
        }

        /// <summary>
        ///     Update x coordinate of zero coordinate offset.
        /// </summary>
        /// <param name="x">X coordinate</param>
        public void OnZeroCoordinateXInputFieldEndEdit(string x)
        {
            ProbeManager.SetZeroCoordinateOffsetX(float.Parse(x));
        }

        /// <summary>
        ///     Update y coordinate of zero coordinate offset.
        /// </summary>
        /// <param name="y">Y coordinate</param>
        public void OnZeroCoordinateYInputFieldEndEdit(string y)
        {
            ProbeManager.SetZeroCoordinateOffsetY(float.Parse(y));
        }

        /// <summary>
        ///     Update z coordinate of zero coordinate offset.
        /// </summary>
        /// <param name="z">Z coordinate</param>
        public void OnZeroCoordinateZInputFieldEndEdit(string z)
        {
            ProbeManager.SetZeroCoordinateOffsetZ(float.Parse(z));
        }

        /// <summary>
        ///     Update depth coordinate of zero coordinate offset.
        /// </summary>
        /// <param name="d">Depth coordinate</param>
        public void OnZeroCoordinateDInputFieldEndEdit(string d)
        {
            ProbeManager.SetZeroCoordinateOffsetDepth(float.Parse(d));
        }

        /// <summary>
        ///     Update drop to surface direction based on dropdown selection.
        /// </summary>
        /// <param name="value">Selected direction: 0 = depth, 1 = DV</param>
        public void OnBrainSurfaceOffsetDirectionDropdownValueChanged(int value)
        {
            ProbeManager.SetDropToSurfaceWithDepth(value == 0);
        }

        /// <summary>
        ///     Update brain surface offset based on input field value.
        /// </summary>
        /// <param name="value">Input field value</param>
        public void OnBrainSurfaceOffsetValueUpdated(string value)
        {
            ProbeManager.BrainSurfaceOffset = float.Parse(value);
        }

        /// <summary>
        ///     Pass an increment amount to the probe manager to update the drop to surface offset.
        /// </summary>
        /// <param name="amount">Amount to increment by (negative numbers are valid)</param>
        public void IncrementBrainSurfaceOffset(float amount)
        {
            ProbeManager.IncrementBrainSurfaceOffset(amount);
        }

        #endregion
    }
}