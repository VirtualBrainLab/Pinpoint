using System.Globalization;
using EphysLink;
using TMPro;
using UnityEngine;

namespace TrajectoryPlanner
{
    public class AutomaticManipulatorControlHandler : MonoBehaviour
    {
        #region Components

        #region Step 1

        [SerializeField] private CanvasGroup zeroCoordinatePanelCanvasGroup;
        [SerializeField] private TMP_Text zeroCoordinatePanelText;
        [SerializeField] private TMP_Text zeroCoordinateManipulator1ProbeText;
        [SerializeField] private TMP_Text zeroCoordinateManipulator2ProbeText;

        #endregion

        #region Step 2

        [SerializeField] private CanvasGroup gotoPanelCanvasGroup;
        [SerializeField] private TMP_Text gotoPanelText;
        [SerializeField] private TMP_Text gotoManipulator1ProbeText;
        [SerializeField] private TMP_InputField gotoManipulator1XInputField;
        [SerializeField] private TMP_InputField gotoManipulator1YInputField;
        [SerializeField] private TMP_InputField gotoManipulator1ZInputField;
        [SerializeField] private TMP_InputField gotoManipulator1DInputField;
        [SerializeField] private TMP_Text gotoManipulator2ProbeText;
        [SerializeField] private TMP_InputField gotoManipulator2XInputField;
        [SerializeField] private TMP_InputField gotoManipulator2YInputField;
        [SerializeField] private TMP_InputField gotoManipulator2ZInputField;
        [SerializeField] private TMP_InputField gotoManipulator2DInputField;
        [SerializeField] private TMP_Text gotoMoveButtonText;

        #endregion

        #region Step 3

        [SerializeField] private CanvasGroup duraPanelCanvasGroup;
        [SerializeField] private TMP_Text duraPanelText;
        [SerializeField] private TMP_Text duraManipulator1ProbeText;
        [SerializeField] private TMP_Text duraManipulator2ProbeText;

        #endregion

        #region Step 4

        [SerializeField] private CanvasGroup drivePanelCanvasGroup;
        [SerializeField] private TMP_Text drivePanelText;
        [SerializeField] private TMP_Text driveStatusText;
        [SerializeField] private TMP_Text driveButtonText;

        #endregion

        private TrajectoryPlannerManager _trajectoryPlannerManager;
        private CommunicationManager _communicationManager;

        #endregion

        #region Properties

        private ProbeManager _probe1Manager;
        private ProbeManager _probe2Manager;

        private uint _step = 1;

        #endregion

        #region Unity

        private void Awake()
        {
            _trajectoryPlannerManager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
            _communicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();
        }

        private void OnEnable()
        {
            // Get probes connected to manipulators 1 and 2
            _probe1Manager = _trajectoryPlannerManager.GetAllProbes().Find(manager => manager.ManipulatorId == "1");
            _probe2Manager = _trajectoryPlannerManager.GetAllProbes().Find(manager => manager.ManipulatorId == "2");

            // Update UI elements

            if (_probe1Manager)
            {
                var probeText = "Probe #" + _probe1Manager.GetID();
                zeroCoordinateManipulator1ProbeText.text = probeText;
                gotoManipulator1ProbeText.text = probeText;
                duraManipulator1ProbeText.text = probeText;

                zeroCoordinateManipulator1ProbeText.color = _probe1Manager.GetColor();
                gotoManipulator1ProbeText.color = _probe1Manager.GetColor();
                duraManipulator1ProbeText.color = _probe1Manager.GetColor();

                UpdateManipulator1ZeroCoordinateFields();
            }

            if (!_probe2Manager) return;
            {
                var probeText = "Probe #" + _probe2Manager.GetID();
                zeroCoordinateManipulator2ProbeText.text = probeText;
                gotoManipulator2ProbeText.text = probeText;
                duraManipulator2ProbeText.text = probeText;

                zeroCoordinateManipulator2ProbeText.color = _probe2Manager.GetColor();
                gotoManipulator2ProbeText.color = _probe2Manager.GetColor();
                duraManipulator2ProbeText.color = _probe2Manager.GetColor();

                UpdateManipulator2ZeroCoordinateFields();
            }
        }

        #endregion

        #region Internal UI Functions

        private void UpdateManipulator1ZeroCoordinateFields()
        {
            gotoManipulator1XInputField.text =
                _probe1Manager.ZeroCoordinateOffset.x.ToString(CultureInfo.CurrentCulture);
            gotoManipulator1YInputField.text =
                _probe1Manager.ZeroCoordinateOffset.y.ToString(CultureInfo.CurrentCulture);
            gotoManipulator1ZInputField.text =
                _probe1Manager.ZeroCoordinateOffset.z.ToString(CultureInfo.CurrentCulture);
            gotoManipulator1DInputField.text =
                _probe1Manager.ZeroCoordinateOffset.w.ToString(CultureInfo.CurrentCulture);
        }

        private void UpdateManipulator2ZeroCoordinateFields()
        {
            gotoManipulator2XInputField.text =
                _probe2Manager.ZeroCoordinateOffset.x.ToString(CultureInfo.CurrentCulture);
            gotoManipulator2YInputField.text =
                _probe2Manager.ZeroCoordinateOffset.y.ToString(CultureInfo.CurrentCulture);
            gotoManipulator2ZInputField.text =
                _probe2Manager.ZeroCoordinateOffset.z.ToString(CultureInfo.CurrentCulture);
            gotoManipulator2DInputField.text =
                _probe2Manager.ZeroCoordinateOffset.w.ToString(CultureInfo.CurrentCulture);
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
            ResetManipulatorZeroCoordinate(_probe1Manager);
        }

        public void ResetManipulator2ZeroCoordinate()
        {
            ResetManipulatorZeroCoordinate(_probe2Manager);
        }

        #endregion
    }
}