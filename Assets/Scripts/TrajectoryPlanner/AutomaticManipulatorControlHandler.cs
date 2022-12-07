using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EphysLink;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrajectoryPlanner
{
    public class AutomaticManipulatorControlHandler : MonoBehaviour
    {
        #region Constants

        private const float LINE_WIDTH = 0.1f;
        private const int NUM_SEGMENTS = 2;

        #endregion

        #region Internal UI Functions

        #region Step 2

        private void InitializeLineRenderers()
        {
            // Create hosting game objects
            _probe1LineGameObjects = (
                new GameObject("APLine1") { layer = 5 }, new GameObject("MLLine1") { layer = 5 },
                new GameObject("DVLine1") { layer = 5 });
            _probe2LineGameObjects = (
                new GameObject("APLine2") { layer = 5 }, new GameObject("MLLine2") { layer = 5 },
                new GameObject("DVLine2") { layer = 5 });

            // Default game objects to hidden
            _probe1LineGameObjects.ap.SetActive(false);
            _probe2LineGameObjects.ap.SetActive(false);
            _probe1LineGameObjects.ml.SetActive(false);
            _probe2LineGameObjects.ml.SetActive(false);
            _probe1LineGameObjects.dv.SetActive(false);
            _probe2LineGameObjects.dv.SetActive(false);

            // Create line renderer components
            _probe1LineRenderers = (_probe1LineGameObjects.ap.AddComponent<LineRenderer>(),
                _probe1LineGameObjects.ml.AddComponent<LineRenderer>(),
                _probe1LineGameObjects.dv.AddComponent<LineRenderer>());
            _probe2LineRenderers = (_probe2LineGameObjects.ap.AddComponent<LineRenderer>(),
                _probe2LineGameObjects.ml.AddComponent<LineRenderer>(),
                _probe2LineGameObjects.dv.AddComponent<LineRenderer>());

            // Set materials
            var apMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = Color.green
            };
            var mlMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = Color.magenta
            };
            var dvMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = Color.cyan
            };

            _probe1LineRenderers.ap.material = apMaterial;
            _probe2LineRenderers.ap.material = apMaterial;
            _probe1LineRenderers.ml.material = mlMaterial;
            _probe2LineRenderers.ml.material = mlMaterial;
            _probe1LineRenderers.dv.material = dvMaterial;
            _probe2LineRenderers.dv.material = dvMaterial;

            // Set line width
            _probe1LineRenderers.ap.startWidth = LINE_WIDTH;
            _probe2LineRenderers.ap.startWidth = LINE_WIDTH;
            _probe1LineRenderers.ml.startWidth = LINE_WIDTH;
            _probe2LineRenderers.ml.startWidth = LINE_WIDTH;
            _probe1LineRenderers.dv.startWidth = LINE_WIDTH;
            _probe2LineRenderers.dv.startWidth = LINE_WIDTH;

            _probe1LineRenderers.ap.endWidth = LINE_WIDTH;
            _probe2LineRenderers.ap.endWidth = LINE_WIDTH;
            _probe1LineRenderers.ml.endWidth = LINE_WIDTH;
            _probe2LineRenderers.ml.endWidth = LINE_WIDTH;
            _probe1LineRenderers.dv.endWidth = LINE_WIDTH;
            _probe2LineRenderers.dv.endWidth = LINE_WIDTH;

            // Set segment count
            _probe1LineRenderers.ap.positionCount = NUM_SEGMENTS;
            _probe2LineRenderers.ap.positionCount = NUM_SEGMENTS;
            _probe1LineRenderers.ml.positionCount = NUM_SEGMENTS;
            _probe2LineRenderers.ml.positionCount = NUM_SEGMENTS;
            _probe1LineRenderers.dv.positionCount = NUM_SEGMENTS;
            _probe2LineRenderers.dv.positionCount = NUM_SEGMENTS;
        }

        private void EnableStep2()
        {
            // Enable UI
            _gotoPanelCanvasGroup.alpha = 1;
            _gotoPanelCanvasGroup.interactable = true;
            _gotoPanelText.color = Color.green;
            _zeroCoordinatePanelText.color = Color.white;

            // Update insertion options
            UpdateInsertionDropdownOptions();
        }

        private void UpdateManipulatorInsertionSelection(int dropdownValue, int manipulatorID)
        {
            UpdateManipulatorInsertionInputFields(dropdownValue, manipulatorID);
            UpdateInsertionDropdownOptions();
            DrawPath(manipulatorID);
            UpdateMoveButtonInteractable();
        }

        private void UpdateManipulatorInsertionInputFields(int dropdownValue, int manipulatorID)
        {
            var insertionInputFields = manipulatorID == 1
                ? _gotoManipulator1InsertionInputFields
                : _gotoManipulator2InsertionInputFields;
            var insertionOptions = manipulatorID == 1
                ? Probe1TargetProbeInsertionOptions
                : Probe2TargetProbeInsertionOptions;

            if (dropdownValue == 0)
            {
                insertionInputFields.ap.text = "";
                insertionInputFields.ml.text = "";
                insertionInputFields.dv.text = "";
            }
            else
            {
                var selectedInsertion = insertionOptions[dropdownValue - 1];
                insertionInputFields.ap.text = selectedInsertion.ap.ToString(CultureInfo.CurrentCulture);
                insertionInputFields.ml.text = selectedInsertion.ml.ToString(CultureInfo.CurrentCulture);
                insertionInputFields.dv.text = selectedInsertion.dv.ToString(CultureInfo.CurrentCulture);
            }
        }

        private void UpdateInsertionDropdownOptions()
        {
            // Save currently selected option
            _probe1SelectedTargetProbeInsertion = _gotoManipulator1TargetInsertionDropdown.value > 0
                ? TargetProbeInsertionsReference.First(insertion =>
                    insertion.PositionToString().Equals(_gotoManipulator1TargetInsertionDropdown
                        .options[_gotoManipulator1TargetInsertionDropdown.value].text))
                : null;
            _probe2SelectedTargetProbeInsertion = _gotoManipulator2TargetInsertionDropdown.value > 0
                ? TargetProbeInsertionsReference.First(insertion =>
                    insertion.PositionToString().Equals(_gotoManipulator2TargetInsertionDropdown
                        .options[_gotoManipulator2TargetInsertionDropdown.value].text))
                : null;

            // Clear options
            _gotoManipulator1TargetInsertionDropdown.ClearOptions();
            _gotoManipulator2TargetInsertionDropdown.ClearOptions();

            // Add base option
            var baseOption = new TMP_Dropdown.OptionData("Choose an insertion...");
            _gotoManipulator1TargetInsertionDropdown.options.Add(baseOption);
            _gotoManipulator2TargetInsertionDropdown.options.Add(baseOption);

            // Add other options
            _gotoManipulator1TargetInsertionDropdown.AddOptions(Probe1TargetProbeInsertionOptions
                .Select(insertion => insertion.PositionToString()).ToList());
            _gotoManipulator2TargetInsertionDropdown.AddOptions(Probe2TargetProbeInsertionOptions
                .Select(insertion => insertion.PositionToString()).ToList());

            // Restore selection option (if possible)
            _gotoManipulator1TargetInsertionDropdown.SetValueWithoutNotify(
                Probe1TargetProbeInsertionOptions.IndexOf(_probe1SelectedTargetProbeInsertion) + 1);

            _gotoManipulator2TargetInsertionDropdown.SetValueWithoutNotify(
                Probe2TargetProbeInsertionOptions.IndexOf(_probe2SelectedTargetProbeInsertion) + 1);
        }

        private void DrawPath(int manipulatorID)
        {
            var targetProbe = manipulatorID == 1
                ? Probe1Manager
                : Probe2Manager;
            var targetInsertion = manipulatorID == 1
                ? _probe1SelectedTargetProbeInsertion
                : _probe2SelectedTargetProbeInsertion;

            // Exit early if there is no target insertion selected
            if (targetInsertion == null)
            {
                if (manipulatorID == 1)
                {
                    _probe1LineGameObjects.ap.SetActive(false);
                    _probe1LineGameObjects.ml.SetActive(false);
                    _probe1LineGameObjects.dv.SetActive(false);
                }
                else
                {
                    _probe2LineGameObjects.ap.SetActive(false);
                    _probe2LineGameObjects.ml.SetActive(false);
                    _probe2LineGameObjects.dv.SetActive(false);
                }

                return;
            }

            // DV axis
            var dvInsertion = new ProbeInsertion(targetProbe.GetProbeController().Insertion)
            {
                dv = -0.5f
            };

            // AP axis
            var apInsertion = new ProbeInsertion(dvInsertion)
            {
                ap = targetInsertion.ap
            };

            // ML axis
            var mlInsertion = new ProbeInsertion(apInsertion)
            {
                ml = targetInsertion.ml
            };

            // Apply to insertion
            if (manipulatorID == 1)
            {
                _probe1MovementAxesInsertions.ap = apInsertion;
                _probe1MovementAxesInsertions.ml = mlInsertion;
                _probe1MovementAxesInsertions.dv = dvInsertion;
            }
            else
            {
                _probe2MovementAxesInsertions.ap = apInsertion;
                _probe2MovementAxesInsertions.ml = mlInsertion;
                _probe2MovementAxesInsertions.dv = dvInsertion;
            }

            // Pickup axes to use
            var axesInsertions = manipulatorID == 1
                ? _probe1MovementAxesInsertions
                : _probe2MovementAxesInsertions;
            var lineRenderer = manipulatorID == 1 ? _probe1LineRenderers : _probe2LineRenderers;

            // Enable game objects
            if (manipulatorID == 1)
            {
                _probe1LineGameObjects.ap.SetActive(true);
                _probe1LineGameObjects.ml.SetActive(true);
                _probe1LineGameObjects.dv.SetActive(true);
            }
            else
            {
                _probe2LineGameObjects.ap.SetActive(true);
                _probe2LineGameObjects.ml.SetActive(true);
                _probe2LineGameObjects.dv.SetActive(true);
            }

            // Set line positions
            lineRenderer.dv.SetPosition(0, targetProbe.GetProbeController().ProbeTipT.position);
            lineRenderer.dv.SetPosition(1, axesInsertions.dv.PositionWorld());

            lineRenderer.ap.SetPosition(0, axesInsertions.dv.PositionWorld());
            lineRenderer.ap.SetPosition(1, axesInsertions.ap.PositionWorld());

            lineRenderer.ml.SetPosition(0, axesInsertions.ap.PositionWorld());
            lineRenderer.ml.SetPosition(1, axesInsertions.ml.PositionWorld());
        }

        private void UpdateMoveButtonInteractable()
        {
            _gotoMoveButton.interactable = _probe1SelectedTargetProbeInsertion != null ||
                                           _probe2SelectedTargetProbeInsertion != null ||
                                           _expectedMovements != _completedMovements;
        }

        private Vector4 ConvertInsertionToManipulatorPosition(ProbeInsertion insertion, string manipulatorID)
        {
            var probeManager = manipulatorID == "1" ? Probe1Manager : Probe2Manager;
            var isManipulatorRightHanded =
                manipulatorID == "1" ? IsProbe1ManipulatorRightHanded : IsProbe2ManipulatorRightHanded;

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
            var brainSurfaceAdjustment = float.IsNaN(probeManager.BrainSurfaceOffset)
                ? 0
                : probeManager.BrainSurfaceOffset;
            if (probeManager.IsSetToDropToSurfaceWithDepth)
                posWithDepthAndCorrectAxes.w -= brainSurfaceAdjustment;
            else
                posWithDepthAndCorrectAxes.z -= brainSurfaceAdjustment;

            // Adjust for phi
            var probePhi = probeManager.GetProbeController().Insertion.phi * Mathf.Deg2Rad;
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
            posWithDepthAndCorrectAxes.y *= isManipulatorRightHanded ? 1 : -1;

            // Apply coordinate offsets and return result
            return posWithDepthAndCorrectAxes + probeManager.ZeroCoordinateOffset;
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

        public void UpdateManipulator1InsertionInputFields(int dropdownValue)
        {
            UpdateManipulatorInsertionSelection(dropdownValue, 1);
        }

        public void UpdateManipulator2InsertionInputFields(int dropdownValue)
        {
            UpdateManipulatorInsertionSelection(dropdownValue, 2);
        }

        public void MoveOrStopProbeToInsertionTarget()
        {
            if (_expectedMovements == _completedMovements)
            {
                // All movements completed

                // Set button text
                _gotoMoveButtonText.text = "Moving... Press Again to Stop";

                // Compute the number of expected movements
                _expectedMovements += _probe1SelectedTargetProbeInsertion != null ? 1 : 0;
                _expectedMovements += _probe2SelectedTargetProbeInsertion != null ? 1 : 0;

                // Reset completed movements
                _completedMovements = 0;

                // Move probe 1
                if (_probe1SelectedTargetProbeInsertion != null)
                    _communicationManager.SetCanWrite(Probe1Manager.ManipulatorId, true, 1, canWrite =>
                    {
                        if (canWrite)
                            _communicationManager.GotoPos(Probe1Manager.ManipulatorId,
                                ConvertInsertionToManipulatorPosition(_probe1MovementAxesInsertions.dv,
                                    Probe1Manager.ManipulatorId), Probe1Manager.AutomaticMovementSpeed,
                                _ =>
                                {
                                    print("Moved DV!");
                                    _completedMovements++;
                                    UpdateMoveButtonInteractable();
                                });
                    });
                
                // Move probe 2
                if (_probe2SelectedTargetProbeInsertion != null)
                    _communicationManager.SetCanWrite(Probe2Manager.ManipulatorId, true, 1, canWrite =>
                    {
                        if (canWrite)
                            _communicationManager.GotoPos(Probe2Manager.ManipulatorId,
                                ConvertInsertionToManipulatorPosition(_probe2MovementAxesInsertions.dv,
                                    Probe2Manager.ManipulatorId), Probe2Manager.AutomaticMovementSpeed,
                                _ =>
                                {
                                    print("Moved DV!");
                                    _completedMovements++;
                                    UpdateMoveButtonInteractable();
                                });
                    });
            }
            else
            {
                // Movement in progress

                // Stop all movements
                _communicationManager.Stop(state =>
                {
                    if (!state) return;
                    // Reset expected movements and completed movements
                    _expectedMovements = 0;
                    _completedMovements = 0;

                    // Reset text
                    _gotoMoveButtonText.text = "Move Manipulators into Position";

                    // Update button interactable
                    UpdateMoveButtonInteractable();
                });
            }
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
        [SerializeField] private Button _gotoMoveButton;
        [SerializeField] private TMP_Text _gotoMoveButtonText;

        private (TMP_InputField ap, TMP_InputField ml, TMP_InputField dv) _gotoManipulator1InsertionInputFields;
        private (TMP_InputField ap, TMP_InputField ml, TMP_InputField dv) _gotoManipulator2InsertionInputFields;

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

        private uint _step = 1;

        public ProbeManager Probe1Manager { private get; set; }
        public ProbeManager Probe2Manager { private get; set; }

        #region Step 2

        public bool IsProbe1ManipulatorRightHanded { private get; set; }
        public bool IsProbe2ManipulatorRightHanded { private get; set; }

        public HashSet<ProbeInsertion> TargetProbeInsertionsReference { private get; set; }
        private ProbeInsertion _probe1SelectedTargetProbeInsertion;
        private ProbeInsertion _probe2SelectedTargetProbeInsertion;

        private List<ProbeInsertion> Probe1TargetProbeInsertionOptions => TargetProbeInsertionsReference
            .Where(insertion => insertion != _probe2SelectedTargetProbeInsertion).ToList();

        private List<ProbeInsertion> Probe2TargetProbeInsertionOptions => TargetProbeInsertionsReference
            .Where(insertion => insertion != _probe1SelectedTargetProbeInsertion).ToList();

        private (ProbeInsertion ap, ProbeInsertion ml, ProbeInsertion dv) _probe1MovementAxesInsertions;
        private (ProbeInsertion ap, ProbeInsertion ml, ProbeInsertion dv) _probe2MovementAxesInsertions;

        private (GameObject ap, GameObject ml, GameObject dv) _probe1LineGameObjects;

        private (GameObject ap, GameObject ml, GameObject dv) _probe2LineGameObjects;

        private (LineRenderer ap, LineRenderer ml, LineRenderer dv) _probe1LineRenderers;
        private (LineRenderer ap, LineRenderer ml, LineRenderer dv) _probe2LineRenderers;

        private int _expectedMovements;
        private int _completedMovements;

        #endregion

        #endregion

        #region Unity

        private void Awake()
        {
            _communicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();

            // Input field access
            _gotoManipulator1InsertionInputFields = (_gotoManipulator1APInputField, _gotoManipulator1MLInputField,
                _gotoManipulator1DVInputField);
            _gotoManipulator2InsertionInputFields = (_gotoManipulator2APInputField, _gotoManipulator2MLInputField,
                _gotoManipulator2DVInputField);

            // Initialize line renderers
            InitializeLineRenderers();
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

                UpdateManipulatorInsertionInputFields(_gotoManipulator1TargetInsertionDropdown.value, 1);
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

                UpdateManipulatorInsertionInputFields(_gotoManipulator2TargetInsertionDropdown.value, 2);
            }
        }

        #endregion
    }
}