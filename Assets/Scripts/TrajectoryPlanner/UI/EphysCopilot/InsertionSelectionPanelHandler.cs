using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EphysLink;
using TMPro;
using TrajectoryPlanner.Probes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TrajectoryPlanner.UI.EphysCopilot
{
    public class InsertionSelectionPanelHandler : MonoBehaviour
    {
        #region Unity

        private void Start()
        {
            // Update manipulator ID text
            _manipulatorIDText.text = "Manipulator " + ProbeManager.ManipulatorBehaviorController.ManipulatorID;
            _manipulatorIDText.color = ProbeManager.Color;

            // Attach to dropdown events
            _shouldUpdateTargetInsertionOptionsEvent.AddListener(UpdateTargetInsertionOptions);
            UpdateTargetInsertionOptions("-1");

            // Create line renderer
            InitializeLineRenderers();
        }

        /// <summary>
        ///     Cleanup line renderers on destroy
        /// </summary>
        private void OnDestroy()
        {
            Destroy(_lineGameObjects.ap);
            Destroy(_lineGameObjects.ml);
            Destroy(_lineGameObjects.dv);
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

        /// <summary>
        ///     Update the target insertion dropdown options.
        ///     Try to maintain/restore previous selection
        /// </summary>
        private void UpdateTargetInsertionOptions(string fromManipulatorID)
        {
            // Skip if called from self
            if (fromManipulatorID == ProbeManager.ManipulatorBehaviorController.ManipulatorID) return;

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
                    .IndexOf(SelectedTargetInsertion.GetValueOrDefault(
                        ProbeManager.ManipulatorBehaviorController.ManipulatorID, null)) + 1
            );
        }

        /// <summary>
        ///     Move to target insertion and handle callback when all movements are done
        /// </summary>
        private void MoveToTargetInsertion()
        {
            // Check if a target insertion is selected
            if (!SelectedTargetInsertion.ContainsKey(ProbeManager.ManipulatorBehaviorController.ManipulatorID)) return;

            // Setup and compute movement
            _isMoving = true;
            var apPosition =
                ProbeManager.ManipulatorBehaviorController.ConvertInsertionToManipulatorPosition(_movementAxesInsertions.ap.apmldv);
            var mlPosition =
                ProbeManager.ManipulatorBehaviorController.ConvertInsertionToManipulatorPosition(_movementAxesInsertions.ml.apmldv);
            var dvPosition =
                ProbeManager.ManipulatorBehaviorController.ConvertInsertionToManipulatorPosition(_movementAxesInsertions.dv.apmldv);

            // Move
            CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorBehaviorController.ManipulatorID, true, 1,
                canWrite =>
                {
                    if (canWrite)
                        CommunicationManager.Instance.GotoPos(ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                            dvPosition,
                            ManipulatorBehaviorController.AUTOMATIC_MOVEMENT_SPEED, _ =>
                            {
                                CommunicationManager.Instance.GotoPos(
                                    ProbeManager.ManipulatorBehaviorController.ManipulatorID, apPosition,
                                    ManipulatorBehaviorController.AUTOMATIC_MOVEMENT_SPEED, _ =>
                                    {
                                        CommunicationManager.Instance.GotoPos(
                                            ProbeManager.ManipulatorBehaviorController.ManipulatorID, mlPosition,
                                            ManipulatorBehaviorController.AUTOMATIC_MOVEMENT_SPEED, _ =>
                                            {
                                                CommunicationManager.Instance.SetCanWrite(
                                                    ProbeManager.ManipulatorBehaviorController.ManipulatorID, false,
                                                    1, _ =>
                                                    {
                                                        // Hide lines
                                                        _lineGameObjects.ap.SetActive(false);
                                                        _lineGameObjects.ml.SetActive(false);
                                                        _lineGameObjects.dv.SetActive(false);

                                                        // Complete movement
                                                        _isMoving = false;
                                                        _moveButtonText.text = MOVE_TO_TARGET_INSERTION_STR;
                                                    }, Debug.LogError);
                                            }, Debug.LogError);
                                    }, Debug.LogError);
                            });
                });
        }

        private void UpdateMoveButtonInteractable()
        {
            _moveButton.interactable =
                SelectedTargetInsertion.ContainsKey(ProbeManager.ManipulatorBehaviorController.ManipulatorID);
        }

        #endregion

        #region Constants

        private const float LINE_WIDTH = 0.1f;
        private const int NUM_SEGMENTS = 2;
        private static readonly Vector3 PRE_DEPTH_DRIVE_BREGMA_OFFSET_W = new(0, 0.5f, 0);
        private const string MOVE_TO_TARGET_INSERTION_STR = "Move to Target Insertion";
        private const string STOP_MOVEMENT_STR = "Stop Movement";

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
        [SerializeField] private Button _moveButton;
        [SerializeField] private TMP_Text _moveButtonText;

        public ProbeManager ProbeManager { private get; set; }

        private (GameObject ap, GameObject ml, GameObject dv) _lineGameObjects;
        private (LineRenderer ap, LineRenderer ml, LineRenderer dv) _lineRenderers;

        #endregion

        #region Properties

        private bool _isMoving;

        private IEnumerable<ProbeInsertion> _targetInsertionOptions => _targetableInsertions
            .Where(insertion =>
                !SelectedTargetInsertion
                    .Where(pair => pair.Key != ProbeManager.ManipulatorBehaviorController.ManipulatorID)
                    .Select(pair => pair.Value).Contains(insertion) &&
                insertion.angles == ProbeManager.ProbeController.Insertion.angles);

        private (ProbeInsertion ap, ProbeInsertion ml, ProbeInsertion dv) _movementAxesInsertions;

        private static IEnumerable<ProbeInsertion> _targetableInsertions => ProbeManager.Instances
            .Where(manager => !manager.IsEphysLinkControlled)
            .Select(manager => manager.ProbeController.Insertion).ToHashSet();


        #region Shared

        public static CCFAnnotationDataset AnnotationDataset { private get; set; }
        public static readonly Dictionary<string, ProbeInsertion> SelectedTargetInsertion = new();
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
            // Get selection as insertion
            var insertion = value > 0
                ? _targetableInsertions.First(insertion =>
                    insertion.PositionToString()
                        .Equals(_targetInsertionDropdown.options[_targetInsertionDropdown.value].text))
                : null;

            // Update selection record and text fields
            if (insertion == null)
            {
                // Remove record if no insertion selected
                SelectedTargetInsertion.Remove(ProbeManager.ManipulatorBehaviorController.ManipulatorID);

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
                SelectedTargetInsertion[ProbeManager.ManipulatorBehaviorController.ManipulatorID] = insertion;

                // Update text fields
                _apInputField.text = (insertion.ap * 1000).ToString(CultureInfo.InvariantCulture);
                _mlInputField.text = (insertion.ml * 1000).ToString(CultureInfo.InvariantCulture);
                _dvInputField.text = (insertion.dv * 1000).ToString(CultureInfo.InvariantCulture);
                _depthInputField.text = "0";

                // Calculate movement insertions

                // DV axis
                _movementAxesInsertions.dv = new ProbeInsertion(ProbeManager.ProbeController.Insertion)
                {
                    dv = ProbeManager.ProbeController.Insertion
                        .World2TransformedAxisChange(PRE_DEPTH_DRIVE_BREGMA_OFFSET_W).z
                };

                // Recalculate AP and ML based on pre-depth-drive DV
                var brainSurfaceCoordinate = AnnotationDataset.FindSurfaceCoordinate(
                    AnnotationDataset.CoordinateSpace.World2Space(
                        SelectedTargetInsertion[ProbeManager.ManipulatorBehaviorController.ManipulatorID]
                            .PositionWorldU()),
                    AnnotationDataset.CoordinateSpace.World2SpaceAxisChange(ProbeManager.ProbeController
                        .GetTipWorldU().tipUpWorldU));
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
                _lineRenderers.dv.SetPosition(0, ProbeManager.ProbeController.ProbeTipT.position);
                _lineRenderers.dv.SetPosition(1, _movementAxesInsertions.dv.PositionWorldT());

                _lineRenderers.ap.SetPosition(0, _movementAxesInsertions.dv.PositionWorldT());
                _lineRenderers.ap.SetPosition(1, _movementAxesInsertions.ap.PositionWorldT());

                _lineRenderers.ml.SetPosition(0, _movementAxesInsertions.ap.PositionWorldT());
                _lineRenderers.ml.SetPosition(1, _movementAxesInsertions.ml.PositionWorldT());
            }

            // Update dropdown options
            _shouldUpdateTargetInsertionOptionsEvent.Invoke(ProbeManager.ManipulatorBehaviorController.ManipulatorID);
            UpdateMoveButtonInteractable();
        }

        public void MoveOrStopProbeToInsertionTarget()
        {
            if (_isMoving)
                // Movement in progress -> should stop movement
            {
                CommunicationManager.Instance.Stop(state =>
                {
                    if (!state) return;

                    _isMoving = false;
                    _moveButtonText.text = MOVE_TO_TARGET_INSERTION_STR;
                });
            }
            else
            {
                MoveToTargetInsertion();
                _moveButtonText.text = STOP_MOVEMENT_STR;
            }
        }

        #endregion
    }
}