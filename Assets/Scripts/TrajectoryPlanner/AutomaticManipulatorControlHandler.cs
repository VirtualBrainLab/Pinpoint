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

        private uint _step = 1;

        #endregion

        #region Unity

        private void Awake()
        {
            _communicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();
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

                UpdateManipulator1ZeroCoordinateFields();
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

                UpdateManipulator2ZeroCoordinateFields();
            }
        }

        #endregion

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

            _gotoManipulator2TargetInsertionDropdown.ClearOptions();
            _gotoManipulator2TargetInsertionDropdown.AddOptions(insertionOptions.ToList());
        }

        private void UpdateManipulator1ZeroCoordinateFields()
        {
            _gotoManipulator1APInputField.text =
                Probe1Manager.ZeroCoordinateOffset.x.ToString(CultureInfo.CurrentCulture);
            _gotoManipulator1MLInputField.text =
                Probe1Manager.ZeroCoordinateOffset.y.ToString(CultureInfo.CurrentCulture);
            _gotoManipulator1DVInputField.text =
                Probe1Manager.ZeroCoordinateOffset.z.ToString(CultureInfo.CurrentCulture);
        }

        private void UpdateManipulator2ZeroCoordinateFields()
        {
            _gotoManipulator2APInputField.text =
                Probe2Manager.ZeroCoordinateOffset.x.ToString(CultureInfo.CurrentCulture);
            _gotoManipulator2MLInputField.text =
                Probe2Manager.ZeroCoordinateOffset.y.ToString(CultureInfo.CurrentCulture);
            _gotoManipulator2DVInputField.text =
                Probe2Manager.ZeroCoordinateOffset.z.ToString(CultureInfo.CurrentCulture);
        }

        #endregion

        #endregion
    }
}