using System.Collections.Generic;
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

            _shouldUpdateTargetInsertionOptionsEvent.AddListener(UpdateTargetInsertionOptions);
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
                !_selectedTargetInsertion.Where(pair => pair.Key != ProbeManager.ManipulatorId)
                    .Select(pair => pair.Value).Contains(insertion) &&
                insertion.angles == ProbeManager.GetProbeController().Insertion.angles);


        #region Shared

        public static HashSet<ProbeInsertion> TargetInsertionsReference { private get; set; }
        private static readonly Dictionary<string, ProbeInsertion> _selectedTargetInsertion = new();
        private static readonly UnityEvent<string> _shouldUpdateTargetInsertionOptionsEvent = new();

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
            // Update selection record
            _selectedTargetInsertion[ProbeManager.ManipulatorId] = value > 0
                ? TargetInsertionsReference.First(insertion =>
                    insertion.PositionToString()
                        .Equals(_targetInsertionDropdown.options[_targetInsertionDropdown.value].text))
                : null;

            // Update dropdown options
            _shouldUpdateTargetInsertionOptionsEvent.Invoke(ProbeManager.ManipulatorId);
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
                    .IndexOf(_selectedTargetInsertion.GetValueOrDefault(ProbeManager.ManipulatorId, null)) + 1
            );
        }

        #endregion
    }
}