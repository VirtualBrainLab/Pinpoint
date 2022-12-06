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
                ? Manipulator1TargetProbeInsertionOptions
                : Manipulator2TargetProbeInsertionOptions;

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
            _manipulator1SelectedTargetProbeInsertion = _gotoManipulator1TargetInsertionDropdown.value > 0
                ? TargetProbeInsertionsReference.First(insertion =>
                    insertion.PositionToString().Equals(_gotoManipulator1TargetInsertionDropdown
                        .options[_gotoManipulator1TargetInsertionDropdown.value].text))
                : null;
            _manipulator2SelectedTargetProbeInsertion = _gotoManipulator2TargetInsertionDropdown.value > 0
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
            _gotoManipulator1TargetInsertionDropdown.AddOptions(Manipulator1TargetProbeInsertionOptions
                .Select(insertion => insertion.PositionToString()).ToList());
            _gotoManipulator2TargetInsertionDropdown.AddOptions(Manipulator2TargetProbeInsertionOptions
                .Select(insertion => insertion.PositionToString()).ToList());

            // Restore selection option (if possible)
            _gotoManipulator1TargetInsertionDropdown.SetValueWithoutNotify(
                Manipulator1TargetProbeInsertionOptions.IndexOf(_manipulator1SelectedTargetProbeInsertion) + 1);

            _gotoManipulator2TargetInsertionDropdown.SetValueWithoutNotify(
                Manipulator2TargetProbeInsertionOptions.IndexOf(_manipulator2SelectedTargetProbeInsertion) + 1);
        }

        private void DrawPath(int manipulatorID)
        {
            var targetProbe = manipulatorID == 1
                ? Probe1Manager
                : Probe2Manager;
            var targetInsertion = manipulatorID == 1
                ? _manipulator1SelectedTargetProbeInsertion
                : _manipulator2SelectedTargetProbeInsertion;

            // Exit early if there is no target insertion selected
            if (targetInsertion == null)
            {
                return;
            }

            // DV axis
            var dvInsertion = new ProbeInsertion(targetProbe.GetProbeController().Insertion)
            {
                dv = -10
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
                _manipulator1MovementAxesInsertions.ap = apInsertion;
                _manipulator1MovementAxesInsertions.ml = mlInsertion;
                _manipulator1MovementAxesInsertions.dv = dvInsertion;
            }
            else
            {
                _manipulator2MovementAxesInsertions.ap = apInsertion;
                _manipulator2MovementAxesInsertions.ml = mlInsertion;
                _manipulator2MovementAxesInsertions.dv = dvInsertion;
            }
            
            // Pickup axes to use
            var axesInsertions = manipulatorID == 1
                ? _manipulator1MovementAxesInsertions
                : _manipulator2MovementAxesInsertions;
            
            // Create line objects and renderers
            (GameObject ap, GameObject ml, GameObject dv) lineObjects = (new GameObject("APLine") { layer = 5 },
                new GameObject("MLLine") { layer = 5 }, new GameObject("DVLine") { layer = 5 });
            (LineRenderer ap, LineRenderer ml, LineRenderer dv) lineRenderers = (
                lineObjects.ap.AddComponent<LineRenderer>(),
                lineObjects.ml.AddComponent<LineRenderer>(), lineObjects.dv.AddComponent<LineRenderer>());

            // Setup line renderers
            lineRenderers.ap.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = Color.magenta
            };
            lineRenderers.ml.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = Color.green
            };
            lineRenderers.dv.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = Color.cyan
            };

            lineRenderers.ap.startWidth = LINE_WIDTH;
            lineRenderers.ml.startWidth = LINE_WIDTH;
            lineRenderers.dv.startWidth = LINE_WIDTH;

            lineRenderers.ap.endWidth = LINE_WIDTH;
            lineRenderers.ml.endWidth = LINE_WIDTH;
            lineRenderers.dv.endWidth = LINE_WIDTH;

            lineRenderers.ap.positionCount = NUM_SEGMENTS;
            lineRenderers.ml.positionCount = NUM_SEGMENTS;
            lineRenderers.dv.positionCount = NUM_SEGMENTS;

            // Set line positions
            lineRenderers.dv.SetPosition(0, targetProbe.GetProbeController().ProbeTipT.position);
            lineRenderers.dv.SetPosition(1, axesInsertions.dv.PositionWorld());

            lineRenderers.ap.SetPosition(0, axesInsertions.dv.PositionWorld());
            lineRenderers.ap.SetPosition(1, axesInsertions.ap.PositionWorld());

            lineRenderers.ml.SetPosition(0, axesInsertions.ap.PositionWorld());
            lineRenderers.ml.SetPosition(1, axesInsertions.ml.PositionWorld());
        }

        private void UpdateMoveButtonInteractable()
        {
            _gotoMoveButton.interactable = _manipulator1SelectedTargetProbeInsertion != null ||
                                           _manipulator2SelectedTargetProbeInsertion != null;
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

        public HashSet<ProbeInsertion> TargetProbeInsertionsReference { private get; set; }
        private ProbeInsertion _manipulator1SelectedTargetProbeInsertion;
        private ProbeInsertion _manipulator2SelectedTargetProbeInsertion;

        private List<ProbeInsertion> Manipulator1TargetProbeInsertionOptions => TargetProbeInsertionsReference
            .Where(insertion => insertion != _manipulator2SelectedTargetProbeInsertion).ToList();

        private List<ProbeInsertion> Manipulator2TargetProbeInsertionOptions => TargetProbeInsertionsReference
            .Where(insertion => insertion != _manipulator1SelectedTargetProbeInsertion).ToList();

        private (ProbeInsertion ap, ProbeInsertion ml, ProbeInsertion dv) _manipulator1MovementAxesInsertions;
        private (ProbeInsertion ap, ProbeInsertion ml, ProbeInsertion dv) _manipulator2MovementAxesInsertions;

        #endregion

        #endregion

        #region Unity

        private void Awake()
        {
            _communicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();

            _gotoManipulator1InsertionInputFields = (_gotoManipulator1APInputField, _gotoManipulator1MLInputField,
                _gotoManipulator1DVInputField);
            _gotoManipulator2InsertionInputFields = (_gotoManipulator2APInputField, _gotoManipulator2MLInputField,
                _gotoManipulator2DVInputField);
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