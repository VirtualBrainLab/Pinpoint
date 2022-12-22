using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TrajectoryPlanner.AutomaticManipulatorControl
{
    public class InsertionSelectionPanelHandler : MonoBehaviour
    {
        #region Unity

        private void Start()
        {
            _manipulatorIDText.text = "Manipulator " + ProbeManager.ManipulatorId;
            _manipulatorIDText.color = ProbeManager.GetColor();

            ShouldUpdateTargetInsertionOptionsEvent.AddListener(UpdateTargetInsertionOptions);
            UpdateTargetInsertionOptions("-1");
        }

        #endregion

        #region Components

        [SerializeField] private TMP_Text _manipulatorIDText;
        [SerializeField] private TMP_Dropdown _targetInsertionDropdown;
        [SerializeField] private TMP_InputField _apInputField;
        [SerializeField] private TMP_InputField _mlInputField;
        [SerializeField] private TMP_InputField _dvInputField;
        [SerializeField] private TMP_InputField _depthInputField;

        public ProbeManager ProbeManager { private get; set; }

        #endregion

        #region Properties

        private IEnumerable<ProbeInsertion> _targetInsertionOptions => TargetInsertionsReference
            .Where(insertion =>
                !SelectedTargetInsertion.Where(pair => pair.Key != ProbeManager.ManipulatorId)
                    .Select(pair => pair.Value).Contains(insertion) &&
                insertion.angles == ProbeManager.GetProbeController().Insertion.angles);


        #region Shared

        public static HashSet<ProbeInsertion> TargetInsertionsReference { private get; set; }
        public static readonly Dictionary<string, ProbeInsertion> SelectedTargetInsertion = new();
        public static readonly UnityEvent<string> ShouldUpdateTargetInsertionOptionsEvent = new();

        #endregion

        #endregion

        #region UI Functions

        /// <summary>
        ///     Update record of selected target insertion for this panel.
        ///     Triggers all other panels to update their target insertion options.
        /// </summary>
        /// <param name="value">Selected index</param>
        public void OnTargetInsertionDropdownValueChanged(int value)
        {
            // Get selection as insertion
            var insertion = value > 0
                ? TargetInsertionsReference.First(insertion =>
                    insertion.PositionToString()
                        .Equals(_targetInsertionDropdown.options[_targetInsertionDropdown.value].text))
                : null;

            // Update selection record and text fields
            if (insertion == null)
            {
                SelectedTargetInsertion.Remove(ProbeManager.ManipulatorId);
                
                _apInputField.text = "";
                _mlInputField.text = "";
                _dvInputField.text = "";
                _depthInputField.text = "";
            }
            else
            {
                SelectedTargetInsertion[ProbeManager.ManipulatorId] = insertion;
                
                _apInputField.text = (insertion.ap * 1000).ToString(CultureInfo.CurrentCulture);
                _mlInputField.text = (insertion.ml * 1000).ToString(CultureInfo.CurrentCulture);
                _dvInputField.text = (insertion.dv * 1000).ToString(CultureInfo.CurrentCulture);
                _depthInputField.text = "0";
            }

            // Update dropdown options
            ShouldUpdateTargetInsertionOptionsEvent.Invoke(ProbeManager.ManipulatorId);
        }

        /// <summary>
        ///     Update the target insertion dropdown options.
        ///     Try to maintain/restore previous selection
        /// </summary>
        public void UpdateTargetInsertionOptions(string fromManipulatorID)
        {
            // Skip if called from self
            if (fromManipulatorID == ProbeManager.ManipulatorId) return;

            // Clear options
            _targetInsertionDropdown.ClearOptions();

            // Add default option
            _targetInsertionDropdown.options.Add(new TMP_Dropdown.OptionData("Select a target insertion..."));

            // Add other options
            _targetInsertionDropdown.AddOptions(_targetInsertionOptions
                .Select(insertion => insertion.PositionToString()).ToList());

            // Restore selection (if possible)
            _targetInsertionDropdown.SetValueWithoutNotify(
                _targetInsertionOptions.ToList()
                    .IndexOf(SelectedTargetInsertion.GetValueOrDefault(ProbeManager.ManipulatorId, null)) + 1
            );
        }

        #endregion
    }
}