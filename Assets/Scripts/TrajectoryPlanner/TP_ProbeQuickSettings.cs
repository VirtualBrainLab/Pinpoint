using System.Collections.Generic;
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
        [FormerlySerializedAs("coordinatePanel")] [SerializeField] private TP_CoordinateEntryPanel _coordinatePanel;
        [FormerlySerializedAs("positionFields")] [SerializeField] private CanvasGroup _positionFields;
        [FormerlySerializedAs("angleFields")] [SerializeField] private CanvasGroup _angleFields;
        [FormerlySerializedAs("buttons")] [SerializeField] private CanvasGroup _buttons;
        [FormerlySerializedAs("automaticMovementToggle")] [SerializeField] private Toggle _automaticMovementToggle;
        [FormerlySerializedAs("automaticMovementSpeedInputField")] [SerializeField] private TMP_InputField _automaticMovementSpeedInputField;
        [FormerlySerializedAs("automaticMovementControlPanelGameObject")] [SerializeField] private GameObject _automaticMovementControlPanelGameObject;
        [FormerlySerializedAs("automaticMovementGoButton")] [SerializeField] private Button _automaticMovementGoButton;
        
        private ProbeManager _probeManager;
        private CommunicationManager _communicationManager;
        private TrajectoryPlannerManager _trajectoryPlannerManager;
        private TMP_InputField[] _inputFields;

        [SerializeField] private TMP_Dropdown _linkedExperimentDropdown;

        private TMP_InputField[] inputFields;

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
            if (_probeManager.IsEphysLinkControlled == _automaticMovementControlPanelGameObject.activeSelf) return;
            UpdateAutomaticControlPanel();
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

                UpdateProbeIdText();

                _coordinatePanel.LinkProbe(probeManager);
                
                UpdateInteractable();
                UpdateCoordinates();
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
                _positionFields.interactable = false; // !_probeManager.GetEphysLinkMovement();
                _angleFields.interactable = _probeManager == null || !_probeManager.IsGhost;
                _buttons.interactable = true;
            }
        }

        public void UpdateCoordinates()
        {
            _coordinatePanel.UpdateText();
        }

        public void UpdateProbeIdText()
        {
            _probeIdText.text = _probeManager.UUID.Substring(0, 8);
            _probeIdText.color = _probeManager.GetColor();
        }

        private void UpdateAutomaticControlPanel()
        {
            // Check if this probe can be controlled by EphysLink
            if (_probeManager.IsEphysLinkControlled)
            {
                // Show the panel
                _automaticMovementControlPanelGameObject.SetActive(true);
                
                // Set enable status (based on if there is a ghost attached or not)
                _automaticMovementToggle.SetIsOnWithoutNotify(_probeManager.HasGhost);
                
                // Enable/disable interaction based on if there is a ghost attached or not
                EnableAutomaticControlUI(_probeManager.HasGhost);
                
                // Set value in speed input field
                _automaticMovementSpeedInputField.text = _probeManager.AutomaticMovementSpeed.ToString();
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
            _probeManager.SetBrainSurfaceOffset();

            UpdateCoordinates();
        }

        /// <summary>
        ///     Set current manipulator position to be Bregma and move probe to Bregma
        /// </summary>
        public void ResetZeroCoordinate()
        {
            if (_probeManager.IsEphysLinkControlled)
            {
                _communicationManager.GetPos(_probeManager.ManipulatorId,
                    zeroCoordinate => _probeManager.ZeroCoordinateOffset = zeroCoordinate);
                _probeManager.BrainSurfaceOffset = 0;
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
            EnableAutomaticControlUI(isOn);

            if (isOn)
            {
                // Spawn ghost
                var originalProbeManager = _probeManager;
                var ghostProbeManager = _trajectoryPlannerManager.AddNewProbe(
                    _probeManager.ProbeType, _probeManager.GetProbeController().Insertion, "",
                    _probeManager.ZeroCoordinateOffset, _probeManager.BrainSurfaceOffset,
                    _probeManager.IsSetToDropToSurfaceWithDepth, null, true);
    
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
                _trajectoryPlannerManager.DestroyProbe(_probeManager.GhostProbeManager);
                // Remove ghost probe manager reference
                _probeManager.GhostProbeManager = null;
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
            _probeManager.SetAutomaticMovementSpeed(int.Parse(_automaticMovementSpeedInputField.text));
        }

        /// <summary>
        /// Send position to start automatic driving
        /// </summary>
        public void AutomaticallyDriveManipulator()
        {
            // Gather info
            var apmldv = _probeManager.GhostProbeManager.GetProbeController().Insertion.apmldv;
            var depth = _probeManager.GhostProbeManager.GetProbeController().GetProbeDepth();

            // Convert apmldv to world coordinate
            var convertToWorld = _probeManager.GhostProbeManager.GetProbeController().Insertion
                .Transformed2WorldAxisChange(apmldv);

            // Flip axes to match manipulator
            var posWithDepthAndCorrectAxes = new Vector4(
                -convertToWorld.z,
                convertToWorld.x,
                convertToWorld.y,
                depth);

            // Apply brain surface offset
            var brainSurfaceAdjustment = float.IsNaN(_probeManager.BrainSurfaceOffset)
                ? 0
                : _probeManager.BrainSurfaceOffset;
            if (_probeManager.IsSetToDropToSurfaceWithDepth)
                posWithDepthAndCorrectAxes.w -= brainSurfaceAdjustment;
            else
                posWithDepthAndCorrectAxes.z -= brainSurfaceAdjustment;

            // Adjust for phi
            var probePhi = _probeManager.GetProbeController().Insertion.phi * Mathf.Deg2Rad;
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
                ProbeManager.RightHandedManipulatorIDs.Contains(_probeManager.ManipulatorId) ? 1 : -1;

            // Apply coordinate offsets
            var zeroCoordinateOffsetPos = posWithDepthAndCorrectAxes + _probeManager.ZeroCoordinateOffset;

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
            lineRenderer.SetPosition(0, _probeManager.GetProbeController().ProbeTipT.position);
            // Set end position (ghost position)
            lineRenderer.SetPosition(1,
                _probeManager.GhostProbeManager.GetProbeController().ProbeTipT.position);

            // Send position to manipulator
            _communicationManager.SetCanWrite(_probeManager.ManipulatorId, true, 1, canWrite =>
            {
                if (canWrite)
                    _communicationManager.GotoPos(_probeManager.ManipulatorId,
                        zeroCoordinateOffsetPos, _probeManager.AutomaticMovementSpeed, _ => Destroy(lineObject),
                        Debug.LogError);
            }, Debug.LogError);
        }

        #endregion
    }
}