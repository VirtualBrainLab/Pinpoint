using System.Linq;
using EphysLink;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrajectoryPlanner
{
    public class TP_ProbeQuickSettings : MonoBehaviour
    {
        #region Components

        [SerializeField] private TMP_Text panelTitle;
        [SerializeField] private TP_CoordinateEntryPanel coordinatePanel;
        [SerializeField] private CanvasGroup positionFields;
        [SerializeField] private CanvasGroup angleFields;
        [SerializeField] private CanvasGroup buttons;
        [SerializeField] private TMP_InputField automaticMovementSpeedInputField;
        [SerializeField] private GameObject automaticMovementControlPanelGameObject;
        [SerializeField] private Button automaticMovementGoButton;
        
        private ProbeManager _probeManager;
        private CommunicationManager _communicationManager;
        private TrajectoryPlannerManager _trajectoryPlannerManager;
        private TMP_InputField[] _inputFields;

        #endregion

        #region Unity

        /// <summary>
        ///     Initialize components
        /// </summary>
        private void Awake()
        {
            _communicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();
            _trajectoryPlannerManager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();

            _inputFields = gameObject.GetComponentsInChildren<TMP_InputField>(true);

            UpdateInteractable(true);
        }

        /// <summary>
        ///     Update UI components based on external updates
        /// </summary>
        private void FixedUpdate()
        {
            if (!_probeManager) return;
            if (_probeManager.IsConnectedToManipulator() != automaticMovementControlPanelGameObject.activeSelf)
                automaticMovementControlPanelGameObject.SetActive(_probeManager.IsConnectedToManipulator());
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Set active probe (called by TrajectoryPlannerManager)
        /// </summary>
        /// <param name="probeManager">Probe Manager of active probe</param>
        public void SetProbeManager(ProbeManager probeManager)
        {
            if (probeManager == null)
                gameObject.SetActive(false);
            else
            {
                gameObject.SetActive(true);
                _probeManager = probeManager;

                panelTitle.text = probeManager.GetID().ToString();
                panelTitle.color = probeManager.GetColor();

                coordinatePanel.LinkProbe(probeManager);
                
                automaticMovementControlPanelGameObject.SetActive(_probeManager.IsConnectedToManipulator());

                UpdateInteractable();
                UpdateCoordinates();
            }
        }

        public void UpdateInteractable(bool disableAll=false)
        {
            if (disableAll)
            {
                positionFields.interactable = false;
                angleFields.interactable = false;
                buttons.interactable = false;
            }
            else
            {
                positionFields.interactable = false; // !_probeManager.GetEphysLinkMovement();
                angleFields.interactable = true;
                buttons.interactable = true;
            }
        }

        public void UpdateCoordinates()
        {
            coordinatePanel.UpdateText();
        }

        /// <summary>
        ///     Move probe to brain surface and zero out depth
        /// </summary>
        public void ZeroDepth()
        {
            _probeManager.SetBrainSurfaceOffset();

            UpdateCoordinates();
        }

        /// <summary>
        ///     Set current manipulator position to be Bregma and move probe to Bregma
        /// </summary>
        public void ResetZeroCoordinate()
        {
            if (_probeManager.IsConnectedToManipulator())
            {
                _communicationManager.GetPos(_probeManager.GetManipulatorId(), _probeManager.SetZeroCoordinateOffset);
                _probeManager.SetBrainSurfaceOffset(0);
            }
            else
            {
                _probeManager.GetProbeController().ResetPosition();
                _probeManager.GetProbeController().SetProbePosition();
            }

            UpdateCoordinates();
        }

        public bool IsFocused()
        {
            return isActiveAndEnabled && _inputFields.Any(inputField => inputField != null && inputField.isFocused);
        }

        /// <summary>
        /// Toggle on or off automatic manipulator control
        /// </summary>
        /// <param name="isOn">Toggle state</param>
        public void ToggleAutomaticControl(bool isOn)
        {
            automaticMovementSpeedInputField.interactable = isOn;
            automaticMovementGoButton.interactable = isOn;
            
            // Spawn ghost
            var originalProbeManager = _probeManager;
            var ghostProbeManager = _trajectoryPlannerManager.AddNewProbeTransformed(
                _probeManager.GetProbeType(), _probeManager.GetProbeController().Insertion, 0,
                _probeManager.GetZeroCoordinateOffset(), _probeManager.GetBrainSurfaceOffset(),
                _probeManager.IsSetToDropToSurfaceWithDepth());

            // Configure ghost
            var originalProbeInsertion = originalProbeManager.GetProbeController().Insertion;

            ghostProbeManager.SetMaterialsTransparent();
            ghostProbeManager.DisableAllColliders();

            // Deep copy overwrite the positions and angles of the insertion
            ghostProbeManager.GetProbeController().SetProbePosition(new ProbeInsertion(originalProbeInsertion.ap,
                originalProbeInsertion.ml, originalProbeInsertion.dv, originalProbeInsertion.phi, originalProbeInsertion.theta,
                originalProbeInsertion.spin, originalProbeInsertion.CoordinateSpace, originalProbeInsertion.CoordinateTransform));
            
            // Set references
            ghostProbeManager.SetOriginalProbeManager(originalProbeManager);
            originalProbeManager.SetGhostProbeManager(ghostProbeManager);
        }

        /// <summary>
        /// Automatically limit speed input from text input
        /// </summary>
        /// <param name="input">Value from text input</param>
        public void LimitSpeedInput(string input)
        {
            if (input.Length == 0)
            {
                return;
            }

            switch (int.Parse(input))
            {
                case < 0:
                    automaticMovementSpeedInputField.SetTextWithoutNotify("0");
                    break;
                case > 8000:
                    automaticMovementSpeedInputField.SetTextWithoutNotify("8000");
                    break;
            }
        }

        /// <summary>
        /// Send position to start automatic driving
        /// </summary>
        public void AutomaticallyDriveManipulator()
        {
            // Set default speed if none was provided
            if (automaticMovementSpeedInputField.text.Length == 0)
            {
                automaticMovementSpeedInputField.SetTextWithoutNotify("500");
            }

            // Gather info
            var speed = int.Parse(automaticMovementSpeedInputField.text);
            var apmldv = _probeManager.GetGhostProbeManager().GetProbeController().Insertion.apmldv;
            var depth = _probeManager.GetGhostProbeManager().GetProbeController().GetProbeDepth();

            // Convert apmldv to world coordinate
            var convertToWorld = _probeManager.GetGhostProbeManager().GetProbeController().Insertion
                .Transformed2WorldRot(apmldv);

            // Flip axes to match manipulator
            var posWithDepthAndCorrectAxes = new Vector4(
                -convertToWorld.z,
                convertToWorld.x * (
                    _trajectoryPlannerManager.IsManipulatorRightHanded(_probeManager.GetManipulatorId())
                        ? 1
                        : -1),
                -convertToWorld.y,
                depth);

            var brainSurfaceAdjustment = float.IsNaN(_probeManager.GetBrainSurfaceOffset())
                ? 0
                : _probeManager.GetBrainSurfaceOffset();
            if (_probeManager.IsSetToDropToSurfaceWithDepth())
                posWithDepthAndCorrectAxes.w -= brainSurfaceAdjustment;
            else
                posWithDepthAndCorrectAxes.z -= brainSurfaceAdjustment;

            var zeroCoordinateOffsetPos = posWithDepthAndCorrectAxes + _probeManager.GetZeroCoordinateOffset();

            print("Speed: " + speed);
            print("Pos: " + zeroCoordinateOffsetPos);
            zeroCoordinateOffsetPos.w -= _probeManager.GetBrainSurfaceOffset();
            print("Pos after depth update: " + zeroCoordinateOffsetPos);

            
            _communicationManager.SetCanWrite(_probeManager.GetManipulatorId(), true, 1, canWrite =>
            {
                if (canWrite)
                    _communicationManager.GotoPos(_probeManager.GetManipulatorId(),
                        zeroCoordinateOffsetPos, speed, endPos => Debug.Log("End Position: " + endPos),
                        Debug.LogError);
            }, Debug.LogError);
            
        }
        #endregion
    }
}