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
        [SerializeField] private TMP_Text zeroCoordinateProbe1Text;
        [SerializeField] private TMP_Text zeroCoordinateProbe2Text;

        #endregion

        #region Step 2

        [SerializeField] private CanvasGroup gotoPanelCanvasGroup;
        [SerializeField] private TMP_Text gotoPanelText;
        [SerializeField] private TMP_Text gotoProbe1Text;
        [SerializeField] private TMP_InputField gotoProbe1XInputField;
        [SerializeField] private TMP_InputField gotoProbe1YInputField;
        [SerializeField] private TMP_InputField gotoProbe1ZInputField;
        [SerializeField] private TMP_InputField gotoProbe1DInputField;
        [SerializeField] private TMP_Text gotoProbe2Text;
        [SerializeField] private TMP_InputField gotoProbe2XInputField;
        [SerializeField] private TMP_InputField gotoProbe2YInputField;
        [SerializeField] private TMP_InputField gotoProbe2ZInputField;
        [SerializeField] private TMP_InputField gotoProbe2DInputField;
        [SerializeField] private TMP_Text gotoMoveButtonText;

        #endregion

        #region Step 3

        [SerializeField] private CanvasGroup duraPanelCanvasGroup;
        [SerializeField] private TMP_Text duraPanelText;
        [SerializeField] private TMP_Text duraProbe1Text;
        [SerializeField] private TMP_Text duraProbe2Text;

        #endregion

        #region Step 4

        [SerializeField] private CanvasGroup drivePanelCanvasGroup;
        [SerializeField] private TMP_Text drivePanelText;
        [SerializeField] private TMP_Text driveStatusText;
        [SerializeField] private TMP_Text driveButtonText;

        #endregion

        #endregion

        #region Properties

        public ProbeManager Probe1Manager { private get; set; }
        public ProbeManager Probe2Manager { private get; set; }

        #endregion
    }
}