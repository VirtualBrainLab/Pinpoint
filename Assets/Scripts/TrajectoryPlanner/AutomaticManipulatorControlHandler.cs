using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EphysLink;
using TMPro;
using UnityEngine;

namespace TrajectoryPlanner
{
    public class AutomaticManipulatorControlHandler : MonoBehaviour
    {
        #region Internal UI Functions

        #region Step 2

        private void EnableStep2()
        {
            // Enable UI
            _gotoPanelCanvasGroup.alpha = 1;
            _gotoPanelCanvasGroup.interactable = true;
            _gotoPanelText.color = Color.green;
            _zeroCoordinatePanelText.color = Color.white;

            // Update insertion options
            var insertionOptions = new List<string> { "Choose an insertion..." };
            insertionOptions.AddRange(TargetProbeInsertionsReference.Select(insertion => insertion.PositionToString()));

            _gotoManipulator1TargetInsertionDropdown.ClearOptions();
            _gotoManipulator1TargetInsertionDropdown.AddOptions(insertionOptions.ToList());
            _manipulator1TargetProbeInsertionOptions = TargetProbeInsertionsReference.ToList();

            _gotoManipulator2TargetInsertionDropdown.ClearOptions();
            _gotoManipulator2TargetInsertionDropdown.AddOptions(insertionOptions.ToList());
            _manipulator2TargetProbeInsertionOptions = TargetProbeInsertionsReference.ToList();
        }

        private void UpdateManipulatorZeroCoordinateFields(int dropdownValue, int manipulatorID)
        {
            var insertionInputFields = manipulatorID == 1
                ? _gotoManipulator1InsertionInputFields
                : _gotoManipulator2InsertionInputFields;
            var insertionOptions = manipulatorID == 1
                ? _manipulator1TargetProbeInsertionOptions
                : _manipulator2TargetProbeInsertionOptions;

            if (dropdownValue == 0)
            {
                foreach (var insertionInputField in insertionInputFields) insertionInputField.text = "";
            }
            else
            {
                var selectedInsertion = insertionOptions[dropdownValue - 1];
                insertionInputFields[0].text = selectedInsertion.ap.ToString(CultureInfo.CurrentCulture);
                insertionInputFields[1].text = selectedInsertion.ml.ToString(CultureInfo.CurrentCulture);
                insertionInputFields[2].text = selectedInsertion.dv.ToString(CultureInfo.CurrentCulture);
            }
        }

        #endregion

        #endregion

        #region UI Functions

        #region Step 1

        public void ResetManipulatorZeroCoordinate(int manipulatorID)
        {
            var probeManager = manipulatorID == 1 ? Probe1Manager : Probe2Manager;

            // Check if manipulator is connected
            if (!probeManager || !probeManager.IsEphysLinkControlled) return;

            // Reset zero coordinate
            _communicationManager.GetPos(probeManager.ManipulatorId,
                zeroCoordinate =>
                {
                    probeManager.ZeroCoordinateOffset = zeroCoordinate;

                    probeManager.BrainSurfaceOffset = 0;

                    // Enable step 2 (if needed)
                    if (_step != 1) return;
                    _step = 2;
                    EnableStep2();
                }, Debug.LogError
            );
        }

        #endregion

        #region Step 2

        public void UpdateManipulator1ZeroCoordinateFields(int dropdownValue)
        {
            UpdateManipulatorZeroCoordinateFields(dropdownValue, 1);
        }

        public void UpdateManipulator2ZeroCoordinateFields(int dropdownValue)
        {
            UpdateManipulatorZeroCoordinateFields(dropdownValue, 2);
        }

        #endregion

        #endregion

        #region Components

        #region Step 1

        [SerializeField] private CanvasGroup _zeroCoordinatePanelCanvasGroup;
        [SerializeField] private TMP_Text _zeroCoordinatePanelText;
        [SerializeField] private TMP_Text _zeroCoordinateManipulator1ProbeText;
        [SerializeField] private TMP_Text _zeroCoordinateManipulator2ProbeText;

        #endregion

        #region Step 2

        [SerializeField] private CanvasGroup _gotoPanelCanvasGroup;
        [SerializeField] private TMP_Text _gotoPanelText;
        [SerializeField] private TMP_Text _gotoManipulator1ProbeText;
        [SerializeField] private TMP_Dropdown _gotoManipulator1TargetInsertionDropdown;
        [SerializeField] private TMP_InputField _gotoManipulator1APInputField;
        [SerializeField] private TMP_InputField _gotoManipulator1MLInputField;
        [SerializeField] private TMP_InputField _gotoManipulator1DVInputField;
        [SerializeField] private TMP_Text _gotoManipulator2ProbeText;
        [SerializeField] private TMP_Dropdown _gotoManipulator2TargetInsertionDropdown;
        [SerializeField] private TMP_InputField _gotoManipulator2APInputField;
        [SerializeField] private TMP_InputField _gotoManipulator2MLInputField;
        [SerializeField] private TMP_InputField _gotoManipulator2DVInputField;
        [SerializeField] private TMP_Text _gotoMoveButtonText;

        private List<TMP_InputField> _gotoManipulator1InsertionInputFields;
        private List<TMP_InputField> _gotoManipulator2InsertionInputFields;

        #endregion

        #region Step 3

        [SerializeField] private CanvasGroup _duraPanelCanvasGroup;
        [SerializeField] private TMP_Text _duraPanelText;
        [SerializeField] private TMP_Text _duraManipulator1ProbeText;
        [SerializeField] private TMP_Text _duraManipulator2ProbeText;

        #endregion

        #region Step 4

        [SerializeField] private CanvasGroup _drivePanelCanvasGroup;
        [SerializeField] private TMP_Text _drivePanelText;
        [SerializeField] private TMP_Text _driveStatusText;
        [SerializeField] private TMP_Text _driveButtonText;

        #endregion

        private CommunicationManager _communicationManager;

        #endregion

        #region Properties

        public ProbeManager Probe1Manager { private get; set; }
        public ProbeManager Probe2Manager { private get; set; }

        public HashSet<ProbeInsertion> TargetProbeInsertionsReference { private get; set; }
        private List<ProbeInsertion> _manipulator1TargetProbeInsertionOptions;
        private List<ProbeInsertion> _manipulator2TargetProbeInsertionOptions;

        private uint _step = 1;

        #endregion

        #region Unity

        private void Awake()
        {
            _communicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();

            _gotoManipulator1InsertionInputFields = new List<TMP_InputField>
                { _gotoManipulator1APInputField, _gotoManipulator1MLInputField, _gotoManipulator1DVInputField };
            _gotoManipulator2InsertionInputFields = new List<TMP_InputField>
                { _gotoManipulator2APInputField, _gotoManipulator2MLInputField, _gotoManipulator2DVInputField };
        }

        private void OnEnable()
        {
            if (Probe1Manager)
            {
                var probeText = "Probe #" + Probe1Manager.GetID();
                _zeroCoordinateManipulator1ProbeText.text = probeText;
                _gotoManipulator1ProbeText.text = probeText;
                _duraManipulator1ProbeText.text = probeText;

                _zeroCoordinateManipulator1ProbeText.color = Probe1Manager.GetColor();
                _gotoManipulator1ProbeText.color = Probe1Manager.GetColor();
                _duraManipulator1ProbeText.color = Probe1Manager.GetColor();

                UpdateManipulatorZeroCoordinateFields(_gotoManipulator1TargetInsertionDropdown.value, 1);
            }

            if (!Probe2Manager) return;
            {
                var probeText = "Probe #" + Probe2Manager.GetID();
                _zeroCoordinateManipulator2ProbeText.text = probeText;
                _gotoManipulator2ProbeText.text = probeText;
                _duraManipulator2ProbeText.text = probeText;

                _zeroCoordinateManipulator2ProbeText.color = Probe2Manager.GetColor();
                _gotoManipulator2ProbeText.color = Probe2Manager.GetColor();
                _duraManipulator2ProbeText.color = Probe2Manager.GetColor();

                UpdateManipulatorZeroCoordinateFields(_gotoManipulator2TargetInsertionDropdown.value, 2);
            }
        }

        #endregion
    }
}