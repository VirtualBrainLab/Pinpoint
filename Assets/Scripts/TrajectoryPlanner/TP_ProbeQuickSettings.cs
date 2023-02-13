using System.Linq;
using EphysLink;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TrajectoryPlanner
{
    public class TP_ProbeQuickSettings : MonoBehaviour
    {
        #region Components

        [FormerlySerializedAs("probeIdText")] [SerializeField] private TMP_Text _probeIdText;
        [FormerlySerializedAs("coordinatePanel")] [SerializeField] private CoordinateEntryPanel _coordinatePanel;
        [FormerlySerializedAs("positionFields")] [SerializeField] private CanvasGroup _positionFields;
        [FormerlySerializedAs("angleFields")] [SerializeField] private CanvasGroup _angleFields;
        [FormerlySerializedAs("buttons")] [SerializeField] private CanvasGroup _buttons;
        [FormerlySerializedAs("automaticMovementToggle")] [SerializeField] private Toggle _automaticMovementToggle;
        [FormerlySerializedAs("automaticMovementSpeedInputField")] [SerializeField] private TMP_InputField _automaticMovementSpeedInputField;
        [FormerlySerializedAs("automaticMovementControlPanelGameObject")] [SerializeField] private GameObject _automaticMovementControlPanelGameObject;
        [FormerlySerializedAs("automaticMovementGoButton")] [SerializeField] private Button _automaticMovementGoButton;
        [SerializeField] private QuickSettingsLockBehavior _lockBehavior;
        
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

        #endregion

        #region Public Methods

        /// <summary>
        ///     Set active probe (called by TrajectoryPlannerManager)
        /// </summary>
        /// <param name="probeManager">Probe Manager of active probe</param>
        public void SetActiveProbeManager()
        {
            if (ProbeManager.ActiveProbeManager == null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);

                ProbeManager.ActiveProbeManager.ProbeUIUpdateEvent.AddListener(UpdateProbeIdText);

                UpdateProbeIdText();

                _coordinatePanel.NewProbe();

                // Handle picking up events
                ProbeManager.ActiveProbeManager.EphysLinkControlChangeEvent.AddListener(() =>
                {
                    UpdateInteractable();
                    UpdateAutomaticControlPanel();
                });

                _lockBehavior.SetLockState(ProbeManager.ActiveProbeManager.GetProbeController().Locked);

                UpdateCoordinates();
                UpdateInteractable();
                UpdateAutomaticControlPanel();
            }
        }

        public void UpdateInteractable(bool disableAll=false)
        {
            if (disableAll)
            {
                _positionFields.interactable = false;
                _angleFields.interactable = false;
                _buttons.interactable = false;
            }
            else
            {
                _positionFields.interactable = ProbeManager.ActiveProbeManager != null ? !ProbeManager.ActiveProbeManager.IsEphysLinkControlled : true;
                _angleFields.interactable = ProbeManager.ActiveProbeManager == null || !ProbeManager.ActiveProbeManager.IsGhost;
                _buttons.interactable = true;
            }
        }

        public void UpdateCoordinates()
        {
            _coordinatePanel.UpdateText();
        }

        public void UpdateProbeIdText()
        {
            _probeIdText.text = ProbeManager.ActiveProbeManager.name;
            _probeIdText.color = ProbeManager.ActiveProbeManager.GetColor();
        }

        private void UpdateAutomaticControlPanel()
        {
            // Check if this probe can be controlled by EphysLink
            if (ProbeManager.ActiveProbeManager.IsEphysLinkControlled)
            {
                // Show the panel
                _automaticMovementControlPanelGameObject.SetActive(true);
                
                // Set enable status (based on if there is a ghost attached or not)
                _automaticMovementToggle.SetIsOnWithoutNotify(ProbeManager.ActiveProbeManager.HasGhost);
                
                // Enable/disable interaction based on if there is a ghost attached or not
                EnableAutomaticControlUI(ProbeManager.ActiveProbeManager.HasGhost);
                
                // Set value in speed input field
                _automaticMovementSpeedInputField.text = ProbeManager.ActiveProbeManager.AutomaticMovementSpeed.ToString();
            }
            else
            {
                // Hide the panel
                _automaticMovementControlPanelGameObject.SetActive(false);
            }
        }

        public void EnableAutomaticControlUI(bool enable)
        {
            _automaticMovementSpeedInputField.interactable = enable;
            _automaticMovementGoButton.interactable = enable;
        }

        /// <summary>
        ///     Move probe to brain surface and zero out depth
        /// </summary>
        public void ZeroDepth()
        {
            ProbeManager.ActiveProbeManager.SetBrainSurfaceOffset();

            UpdateCoordinates();
        }

        /// <summary>
        ///     Set current manipulator position to be Bregma and move probe to Bregma
        /// </summary>
        public void ResetZeroCoordinate()
        {
            if (ProbeManager.ActiveProbeManager.IsEphysLinkControlled)
            {
                _communicationManager.GetPos(ProbeManager.ActiveProbeManager.ManipulatorId,
                    zeroCoordinate => ProbeManager.ActiveProbeManager.ZeroCoordinateOffset = zeroCoordinate);
                ProbeManager.ActiveProbeManager.BrainSurfaceOffset = 0;
            }
            else
            {
                ProbeManager.ActiveProbeManager.GetProbeController().ResetPosition();
                ProbeManager.ActiveProbeManager.GetProbeController().SetProbePosition();
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
            EnableAutomaticControlUI(isOn);

            if (isOn)
            {
                // Spawn ghost
                var originalProbeManager = ProbeManager.ActiveProbeManager;
                var ghostProbeManager = _trajectoryPlannerManager.AddNewProbe(
                    ProbeManager.ActiveProbeManager.ProbeType, ProbeManager.ActiveProbeManager.GetProbeController().Insertion, "",
                    ProbeManager.ActiveProbeManager.ZeroCoordinateOffset, ProbeManager.ActiveProbeManager.BrainSurfaceOffset,
                    ProbeManager.ActiveProbeManager.IsSetToDropToSurfaceWithDepth, null, true);
    
                // Configure ghost
                var originalProbeInsertion = originalProbeManager.GetProbeController().Insertion;
    
                ghostProbeManager.SetMaterialsTransparent();
                ghostProbeManager.DisableAllColliders();
    
                // Deep copy overwrite the positions and angles of the insertion
                ghostProbeManager.GetProbeController().SetProbePosition(new ProbeInsertion(originalProbeInsertion.ap,
                    originalProbeInsertion.ml, originalProbeInsertion.dv, originalProbeInsertion.phi, originalProbeInsertion.theta,
                    originalProbeInsertion.spin, originalProbeInsertion.CoordinateSpace, originalProbeInsertion.CoordinateTransform));
                
                // Set references
                originalProbeManager.GhostProbeManager = ghostProbeManager;
                ghostProbeManager.name = "GHOST_" + originalProbeManager.name;
            }
            else
            {
                // Disable UI
                EnableAutomaticControlUI(false);
                // Remove ghost
                _trajectoryPlannerManager.DestroyProbe(ProbeManager.ActiveProbeManager.GhostProbeManager);
                // Remove ghost probe manager reference
                ProbeManager.ActiveProbeManager.GhostProbeManager = null;
            }
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
                case <= 0:
                    _automaticMovementSpeedInputField.SetTextWithoutNotify("1");
                    break;
                case > 8000:
                    _automaticMovementSpeedInputField.SetTextWithoutNotify("8000");
                    break;
            }
            
            // Set speed to probe
            ProbeManager.ActiveProbeManager.SetAutomaticMovementSpeed(int.Parse(_automaticMovementSpeedInputField.text));
        }

        /// <summary>
        /// Send position to start automatic driving
        /// </summary>
        public void AutomaticallyDriveManipulator()
        {
            // Gather info
            var apmldv = ProbeManager.ActiveProbeManager.GhostProbeManager.GetProbeController().Insertion.apmldv;
            var depth = ProbeManager.ActiveProbeManager.GhostProbeManager.GetProbeController().GetProbeDepth();

            // Convert apmldv to world coordinate
            var convertToWorld = ProbeManager.ActiveProbeManager.GhostProbeManager.GetProbeController().Insertion
                .Transformed2WorldAxisChange(apmldv);

            // Flip axes to match manipulator
            var posWithDepthAndCorrectAxes = new Vector4(
                -convertToWorld.z,
                convertToWorld.x,
                convertToWorld.y,
                depth);

            // Apply brain surface offset
            var brainSurfaceAdjustment = float.IsNaN(ProbeManager.ActiveProbeManager.BrainSurfaceOffset)
                ? 0
                : ProbeManager.ActiveProbeManager.BrainSurfaceOffset;
            if (ProbeManager.ActiveProbeManager.IsSetToDropToSurfaceWithDepth)
                posWithDepthAndCorrectAxes.w -= brainSurfaceAdjustment;
            else
                posWithDepthAndCorrectAxes.z -= brainSurfaceAdjustment;

            // Adjust for phi
            var probePhi = ProbeManager.ActiveProbeManager.GetProbeController().Insertion.phi * Mathf.Deg2Rad;
            var phiCos = Mathf.Cos(probePhi);
            var phiSin = Mathf.Sin(probePhi);
            var phiAdjustedX = posWithDepthAndCorrectAxes.x * phiCos -
                               posWithDepthAndCorrectAxes.y * phiSin;
            var phiAdjustedY = posWithDepthAndCorrectAxes.x * phiSin +
                               posWithDepthAndCorrectAxes.y * phiCos;
            posWithDepthAndCorrectAxes.x = phiAdjustedX;
            posWithDepthAndCorrectAxes.y = phiAdjustedY;
            
            // Apply axis negations
            posWithDepthAndCorrectAxes.z *= -1;
            posWithDepthAndCorrectAxes.y *=
                ProbeManager.RightHandedManipulatorIDs.Contains(ProbeManager.ActiveProbeManager.ManipulatorId) ? 1 : -1;

            // Apply coordinate offsets
            var zeroCoordinateOffsetPos = posWithDepthAndCorrectAxes + ProbeManager.ActiveProbeManager.ZeroCoordinateOffset;

            // Draw pathway
            var lineObject = new GameObject("AutoControlPath")
            {
                layer = 5
            };
            var lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.green;
            lineRenderer.endColor = Color.red;
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.positionCount = 2;
            // Set start position (current position)
            lineRenderer.SetPosition(0, ProbeManager.ActiveProbeManager.GetProbeController().ProbeTipT.position);
            // Set end position (ghost position)
            lineRenderer.SetPosition(1,
                ProbeManager.ActiveProbeManager.GhostProbeManager.GetProbeController().ProbeTipT.position);

            // Send position to manipulator
            _communicationManager.SetCanWrite(ProbeManager.ActiveProbeManager.ManipulatorId, true, 1, canWrite =>
            {
                if (canWrite)
                    _communicationManager.GotoPos(ProbeManager.ActiveProbeManager.ManipulatorId,
                        zeroCoordinateOffsetPos, ProbeManager.ActiveProbeManager.AutomaticMovementSpeed, _ => Destroy(lineObject),
                        Debug.LogError);
            }, Debug.LogError);
        }

        #endregion
    }
}