using System.Globalization;
using EphysLink;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrajectoryPlanner
{
    public class AutomaticManipulatorControlHandler : MonoBehaviour
    {
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
        [SerializeField] private TMP_InputField _gotoManipulator1XInputField;
        [SerializeField] private TMP_InputField _gotoManipulator1YInputField;
        [SerializeField] private TMP_InputField _gotoManipulator1ZInputField;
        [SerializeField] private TMP_InputField _gotoManipulator1DInputField;
        [SerializeField] private TMP_Text _gotoManipulator2ProbeText;
        [SerializeField] private TMP_InputField _gotoManipulator2XInputField;
        [SerializeField] private TMP_InputField _gotoManipulator2YInputField;
        [SerializeField] private TMP_InputField _gotoManipulator2ZInputField;
        [SerializeField] private TMP_InputField _gotoManipulator2DInputField;
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
        public ProbeManager Probe2Manager {private get; set; }

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

        private void UpdateManipulator1ZeroCoordinateFields()
        {
            _gotoManipulator1XInputField.text =
                Probe1Manager.ZeroCoordinateOffset.x.ToString(CultureInfo.CurrentCulture);
            _gotoManipulator1YInputField.text =
                Probe1Manager.ZeroCoordinateOffset.y.ToString(CultureInfo.CurrentCulture);
            _gotoManipulator1ZInputField.text =
                Probe1Manager.ZeroCoordinateOffset.z.ToString(CultureInfo.CurrentCulture);
            _gotoManipulator1DInputField.text =
                Probe1Manager.ZeroCoordinateOffset.w.ToString(CultureInfo.CurrentCulture);
        }

        private void UpdateManipulator2ZeroCoordinateFields()
        {
            _gotoManipulator2XInputField.text =
                Probe2Manager.ZeroCoordinateOffset.x.ToString(CultureInfo.CurrentCulture);
            _gotoManipulator2YInputField.text =
                Probe2Manager.ZeroCoordinateOffset.y.ToString(CultureInfo.CurrentCulture);
            _gotoManipulator2ZInputField.text =
                Probe2Manager.ZeroCoordinateOffset.z.ToString(CultureInfo.CurrentCulture);
            _gotoManipulator2DInputField.text =
                Probe2Manager.ZeroCoordinateOffset.w.ToString(CultureInfo.CurrentCulture);
        }

        private void ResetManipulatorZeroCoordinate(ProbeManager probeManager)
        {
            // Check if manipulator is connected
            if (!probeManager || !probeManager.IsEphysLinkControlled) return;

            // Reset zero coordinate
            _communicationManager.GetPos(probeManager.ManipulatorId,
                zeroCoordinate => probeManager.ZeroCoordinateOffset = zeroCoordinate);
            probeManager.BrainSurfaceOffset = 0;

            // Enable step 2 (if needed)
            _step = _step == 1 ? 2 : _step;
        }

        #endregion

        #region UI Functions

        public void ResetManipulator1ZeroCoordinate()
        {
            ResetManipulatorZeroCoordinate(Probe1Manager);
        }

        public void ResetManipulator2ZeroCoordinate()
        {
            ResetManipulatorZeroCoordinate(Probe2Manager);
        }

        #endregion
    }
}