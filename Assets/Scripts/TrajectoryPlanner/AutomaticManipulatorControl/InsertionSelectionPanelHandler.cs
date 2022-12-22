using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EphysLink;
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
            // Update manipulator ID text
            _manipulatorIDText.text = "Manipulator " + ProbeManager.ManipulatorId;
            _manipulatorIDText.color = ProbeManager.GetColor();

            // Attach to dropdown events
            ShouldUpdateTargetInsertionOptionsEvent.AddListener(UpdateTargetInsertionOptions);
            UpdateTargetInsertionOptions("-1");

            // Create line renderer
            InitializeLineRenderers();
        }

        #endregion

        #region Internal Functions

        private void InitializeLineRenderers()
        {
            // Create hosting game objects
            _lineGameObjects = (new GameObject("APLine") { layer = 5 }, new GameObject("MLLine") { layer = 5 },
                new GameObject("DVLine") { layer = 5 });

            // Default them to hidden
            _lineGameObjects.ap.SetActive(false);
            _lineGameObjects.ml.SetActive(false);
            _lineGameObjects.dv.SetActive(false);

            // Create line renderer components
            _lineRenderers = (_lineGameObjects.ap.AddComponent<LineRenderer>(),
                _lineGameObjects.ml.AddComponent<LineRenderer>(),
                _lineGameObjects.dv.AddComponent<LineRenderer>());

            // Set materials
            var defaultSpriteShader = Shader.Find("Sprites/Default");
            _lineRenderers.ap.material = new Material(defaultSpriteShader) { color = _apColor };
            _lineRenderers.ml.material = new Material(defaultSpriteShader) { color = _mlColor };
            _lineRenderers.dv.material = new Material(defaultSpriteShader) { color = _dvColor };

            // Set line width
            _lineRenderers.ap.startWidth = _lineRenderers.ap.endWidth = LINE_WIDTH;
            _lineRenderers.ml.startWidth = _lineRenderers.ml.endWidth = LINE_WIDTH;
            _lineRenderers.dv.startWidth = _lineRenderers.dv.endWidth = LINE_WIDTH;

            // Set Segment count
            _lineRenderers.ap.positionCount =
                _lineRenderers.ml.positionCount = _lineRenderers.dv.positionCount = NUM_SEGMENTS;
        }

        private Vector4 ConvertInsertionToManipulatorPosition(ProbeInsertion insertion)
        {
            // Gather info
            var apmldv = insertion.apmldv;
            const float depth = 0;

            // Convert apmldv to world coordinate
            var convertToWorld = insertion.Transformed2WorldAxisChange(apmldv);
            // var convertToWorld = insertion.PositionWorld();

            // Flip axes to match manipulator
            var posWithDepthAndCorrectAxes = new Vector4(
                -convertToWorld.z,
                convertToWorld.x,
                convertToWorld.y,
                depth);

            // Apply brain surface offset
            var brainSurfaceAdjustment = float.IsNaN(ProbeManager.BrainSurfaceOffset)
                ? 0
                : ProbeManager.BrainSurfaceOffset;
            if (ProbeManager.IsSetToDropToSurfaceWithDepth)
                posWithDepthAndCorrectAxes.w -= brainSurfaceAdjustment;
            else
                posWithDepthAndCorrectAxes.z -= brainSurfaceAdjustment;

            // Adjust for phi
            var probePhi = ProbeManager.GetProbeController().Insertion.phi * Mathf.Deg2Rad;
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
            posWithDepthAndCorrectAxes.y *= RightHandedManipulatorIDs.Contains(ProbeManager.ManipulatorId) ? 1 : -1;

            // Apply coordinate offsets and return result
            return posWithDepthAndCorrectAxes + ProbeManager.ZeroCoordinateOffset;
        }

        #endregion

        #region Constants

        private const float LINE_WIDTH = 0.1f;
        private const int NUM_SEGMENTS = 2;
        private static readonly Vector3 PRE_DEPTH_DRIVE_BREGMA_OFFSET_W = new(0, 0.5f, 0);

        #endregion

        #region Components

        [SerializeField] private Color _apColor;
        [SerializeField] private Color _mlColor;
        [SerializeField] private Color _dvColor;

        [SerializeField] private TMP_Text _manipulatorIDText;
        [SerializeField] private TMP_Dropdown _targetInsertionDropdown;
        [SerializeField] private TMP_InputField _apInputField;
        [SerializeField] private TMP_InputField _mlInputField;
        [SerializeField] private TMP_InputField _dvInputField;
        [SerializeField] private TMP_InputField _depthInputField;

        public ProbeManager ProbeManager { private get; set; }

        private (GameObject ap, GameObject ml, GameObject dv) _lineGameObjects;
        private (LineRenderer ap, LineRenderer ml, LineRenderer dv) _lineRenderers;

        #endregion

        #region Properties

        private IEnumerable<ProbeInsertion> _targetInsertionOptions => TargetInsertionsReference
            .Where(insertion =>
                !SelectedTargetInsertion.Where(pair => pair.Key != ProbeManager.ManipulatorId)
                    .Select(pair => pair.Value).Contains(insertion) &&
                insertion.angles == ProbeManager.GetProbeController().Insertion.angles);

        private (ProbeInsertion ap, ProbeInsertion ml, ProbeInsertion dv) _movementAxesInsertions;


        #region Shared

        public static HashSet<ProbeInsertion> TargetInsertionsReference { private get; set; }
        public static HashSet<string> RightHandedManipulatorIDs { private get; set; }
        public static CommunicationManager CommunicationManager { private get; set; }
        public static CCFAnnotationDataset AnnotationDataset { private get; set; }
        public static readonly Dictionary<string, ProbeInsertion> SelectedTargetInsertion = new();
        private static uint completedMovements;
        public static bool Moving { get; private set; }
        public static readonly UnityEvent<string> ShouldUpdateTargetInsertionOptionsEvent = new();

        #endregion

        #endregion

        #region Public Functions

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
                // Remove record if no insertion selected
                SelectedTargetInsertion.Remove(ProbeManager.ManipulatorId);

                // Reset text fields
                _apInputField.text = "";
                _mlInputField.text = "";
                _dvInputField.text = "";
                _depthInputField.text = "";

                // Hide line
                _lineGameObjects.ap.SetActive(false);
                _lineGameObjects.ml.SetActive(false);
                _lineGameObjects.dv.SetActive(false);
            }
            else
            {
                // Update record if insertion selected
                SelectedTargetInsertion[ProbeManager.ManipulatorId] = insertion;

                // Update text fields
                _apInputField.text = (insertion.ap * 1000).ToString(CultureInfo.CurrentCulture);
                _mlInputField.text = (insertion.ml * 1000).ToString(CultureInfo.CurrentCulture);
                _dvInputField.text = (insertion.dv * 1000).ToString(CultureInfo.CurrentCulture);
                _depthInputField.text = "0";

                // Calculate movement insertions

                // DV axis
                _movementAxesInsertions.dv = new ProbeInsertion(ProbeManager.GetProbeController().Insertion)
                {
                    dv = ProbeManager.GetProbeController().Insertion
                        .World2TransformedAxisChange(PRE_DEPTH_DRIVE_BREGMA_OFFSET_W).z
                };

                // Recalculate AP and ML based on pre-depth-drive DV
                var brainSurfaceCoordinate = AnnotationDataset.FindSurfaceCoordinate(
                    AnnotationDataset.CoordinateSpace.World2Space(SelectedTargetInsertion[ProbeManager.ManipulatorId]
                        .PositionWorldU()),
                    AnnotationDataset.CoordinateSpace.World2SpaceAxisChange(ProbeManager.GetProbeController()
                        .GetTipWorldU().tipUpWorld));
                var brainSurfaceWorld = AnnotationDataset.CoordinateSpace.Space2World(brainSurfaceCoordinate);
                var brainSurfaceTransformed = _movementAxesInsertions.dv.World2Transformed(brainSurfaceWorld);

                // AP Axis
                _movementAxesInsertions.ap = new ProbeInsertion(_movementAxesInsertions.dv)
                {
                    ap = brainSurfaceTransformed.x
                };

                // ML Axis
                _movementAxesInsertions.ml = new ProbeInsertion(_movementAxesInsertions.ap)
                {
                    ml = brainSurfaceTransformed.y
                };

                // Update line renderer

                // Show line
                _lineGameObjects.ap.SetActive(true);
                _lineGameObjects.ml.SetActive(true);
                _lineGameObjects.dv.SetActive(true);

                // Set line positions
                _lineRenderers.dv.SetPosition(0, ProbeManager.GetProbeController().ProbeTipT.position);
                _lineRenderers.dv.SetPosition(1, _movementAxesInsertions.dv.PositionWorldT());

                _lineRenderers.ap.SetPosition(0, _movementAxesInsertions.dv.PositionWorldT());
                _lineRenderers.ap.SetPosition(1, _movementAxesInsertions.ap.PositionWorldT());

                _lineRenderers.ml.SetPosition(0, _movementAxesInsertions.ap.PositionWorldT());
                _lineRenderers.ml.SetPosition(1, _movementAxesInsertions.ml.PositionWorldT());
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

        /// <summary>
        ///     Move to target insertion and handle callback when all movements are done
        /// </summary>
        /// <param name="totallyCompleteCallback">Callback function to report back when all movements have completed</param>
        public void MoveToTargetInsertion(Action totallyCompleteCallback)
        {
            // Check if a target insertion is selected
            if (!SelectedTargetInsertion.ContainsKey(ProbeManager.ManipulatorId)) return;

            // Setup and compute movement
            Moving = true;
            var manipulatorID = ProbeManager.ManipulatorId;
            var automaticMovementSpeed = ProbeManager.AutomaticMovementSpeed;
            var apPosition =
                ConvertInsertionToManipulatorPosition(_movementAxesInsertions.ap);
            var mlPosition =
                ConvertInsertionToManipulatorPosition(_movementAxesInsertions.ml);
            var dvPosition =
                ConvertInsertionToManipulatorPosition(_movementAxesInsertions.dv);

            // Move
            CommunicationManager.SetCanWrite(manipulatorID, true, 1, canWrite =>
            {
                if (canWrite)
                    CommunicationManager.GotoPos(manipulatorID, dvPosition,
                        automaticMovementSpeed, _ =>
                        {
                            CommunicationManager.GotoPos(manipulatorID, apPosition,
                                automaticMovementSpeed, _ =>
                                {
                                    CommunicationManager.GotoPos(manipulatorID, mlPosition,
                                        automaticMovementSpeed, _ =>
                                        {
                                            CommunicationManager.SetCanWrite(manipulatorID, false, 1, _ =>
                                            {
                                                // Hide lines
                                                _lineGameObjects.ap.SetActive(false);
                                                _lineGameObjects.ml.SetActive(false);
                                                _lineGameObjects.dv.SetActive(false);

                                                // Increment movement counter
                                                completedMovements++;

                                                // Check and invoke totally complete callback
                                                if (completedMovements != SelectedTargetInsertion.Count) return;
                                                Moving = false;
                                                totallyCompleteCallback.Invoke();
                                            }, Debug.LogError);
                                        }, Debug.LogError);
                                }, Debug.LogError);
                        });
            });
        }

        /// <summary>
        ///     Reset state function for when a stop is requested
        /// </summary>
        public static void MovementStopped()
        {
            completedMovements = 0;
            Moving = false;
        }

        #endregion
    }
}