using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EphysLink;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrajectoryPlanner.AutomaticManipulatorControl
{
    public class AutomaticManipulatorControlHandler : MonoBehaviour
    {
        #region Constants

        private const float LINE_WIDTH = 0.1f;
        private const int NUM_SEGMENTS = 2;
        private static readonly Vector3 PRE_DEPTH_DRIVE_BREGMA_OFFSET_W = new(0, 0.5f, 0);
        private const int DEPTH_DRIVE_SPEED = 5;

        #endregion

        #region Internal UI Functions

        #region Step 1

        private void EnableStep1()
        {
            if (!ProbeManagers.Any()) return;
            _zeroCoordinatePanel.PanelScrollView.SetActive(true);
            _zeroCoordinatePanel.ManipulatorsAttachedText.SetActive(false);
            foreach (var probeManager in ProbeManagers) AddResetZeroCoordinatePanel(probeManager);
        }

        private void AddResetZeroCoordinatePanel(ProbeManager probeManager)
        {
            // Instantiate
            var resetZeroCoordinatePanelGameObject = Instantiate(
                _zeroCoordinatePanel.ResetZeroCoordinatePanelPrefab,
                _zeroCoordinatePanel.PanelScrollViewContent.transform);
            var resetZeroCoordinatePanelHandler =
                resetZeroCoordinatePanelGameObject.GetComponent<ResetZeroCoordinatePanelHandler>();

            // Setup
            resetZeroCoordinatePanelHandler.ProbeManager = probeManager;
        }

        #endregion

        #region Step 2

        private void EnableStep2()
        {
            // Check if needed
            if (_step != 1) return;
            _step = 2;

            // Enable UI
            _gotoPanel.CanvasGroup.alpha = 1;
            _gotoPanel.CanvasGroup.interactable = true;
            _gotoPanel.PanelText.color = _readyColor;
            _zeroCoordinatePanel.PanelText.color = Color.white;
            _gotoPanel.PanelScrollView.SetActive(true);
            _gotoPanel.ManipulatorsZeroedText.SetActive(false);
        }

        private void AddInsertionSelectionPanel(ProbeManager probeManager)
        {
            // Enable step 2 (automatically checks if needed)
            EnableStep2();

            // Instantiate
            var insertionSelectionPanelGameObject = Instantiate(_gotoPanel.InsertionSelectionPanelPrefab,
                _gotoPanel.PanelScrollViewContent.transform);
            var insertionSelectionPanelHandler =
                insertionSelectionPanelGameObject.GetComponent<InsertionSelectionPanelHandler>();

            // Setup
            insertionSelectionPanelHandler.ProbeManager = probeManager;
        }

        private void UpdateMoveButtonInteractable(string _)
        {
            _gotoPanel.MoveButton.interactable = InsertionSelectionPanelHandler.SelectedTargetInsertion.Count > 0;
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

        private void CheckCompletionState()
        {
            if (_expectedMovements == _completedMovements)
            {
                // Reset text states
                _gotoMoveButtonText.text =
                    "Move Manipulators into Position";
                _gotoPanelText.color = Color.white;

                // Reset movement counters
                _expectedMovements = 0;
                _completedMovements = 0;

                // Enable step 3
                if (_step != 2) return;
                _step = 3;
                EnableStep3();
            }

            // Update button intractability (might do nothing)
            UpdateMoveButtonInteractable("");
        }

        #endregion

        #region Step 3

        private void EnableStep3()
        {
            // Enable UI
            _duraPanelCanvasGroup.alpha = 1;
            _duraPanelCanvasGroup.interactable = true;
            _duraPanelText.color = Color.green;
            _gotoPanelText.color = Color.white;
        }

        #endregion

        #region Step 4

        private void EnableStep4()
        {
            // Enable UI
            _drivePanel.CanvasGroup.alpha = 1;
            _drivePanel.CanvasGroup.interactable = true;
            _duraPanelText.color = Color.white;
            _drivePanel.PanelText.color = Color.green;
            _drivePanel.StatusText.text = "Ready to Drive";
        }

        private void StartDriveChain()
        {
            _communicationManager.GetPos("1", manipulator1Pos =>
            {
                _communicationManager.GetPos("2", manipulator2Pos =>
                {
                    // Compute furthest depth drive distance
                    var probe1MaxTravelDistance = _probeAtDura[0]
                        ? Vector3.Distance(_probe1SelectedTargetProbeInsertion.apmldv,
                            Probe1Manager.GetProbeController().Insertion.apmldv) + .2f
                        : 0;
                    var probe2MaxTravelDistance = _probeAtDura[1]
                        ? Vector3.Distance(_probe2SelectedTargetProbeInsertion.apmldv,
                            Probe2Manager.GetProbeController().Insertion.apmldv) + .2f
                        : 0;
                    var maxTravelDistance = Math.Max(probe1MaxTravelDistance, probe2MaxTravelDistance);

                    // Apply to target depth location
                    _probeTargetDepth[0] = manipulator1Pos.w + probe1MaxTravelDistance - .2f;
                    _probeTargetDepth[1] = manipulator2Pos.w + probe2MaxTravelDistance - .2f;

                    // Compute drive time:
                    // Time to move manipulator to 200 µm past target @ 5 µm/s
                    _driveDuration = maxTravelDistance * 1000f / DEPTH_DRIVE_SPEED;

                    // Time to move back to target at 5 µm/s
                    _driveDuration += 40;

                    // Time to let settle (at least 2 minutes, or total distance / 1000 µm per minute)
                    _driveDuration += Math.Max(120, maxTravelDistance * 60f);

                    // Start timer
                    StartCoroutine(CountDownTimer());

                    // Start drive chain
                    Drive200PastTarget();
                });
            });
        }

        private IEnumerator CountDownTimer()
        {
            // Set timer text
            _drivePanel.TimerText.text = TimeSpan.FromSeconds(_driveDuration).ToString(@"mm\:ss");

            // Wait for 1 second
            yield return new WaitForSeconds(1);

            // Decrement timer
            _driveDuration--;

            // Check if timer is done
            if (_driveDuration > 0 && _isDriving)
                // Start next timer
            {
                StartCoroutine(CountDownTimer());
            }
            else
            {
                // Set timer text
                _drivePanel.StatusText.text = "Drive Complete!";
                _drivePanel.TimerText.text = "Ready for Experiment";
                _drivePanel.ButtonText.text = "Drive";
                _drivePanel.PanelText.color = Color.white;
            }
        }

        private void Drive200PastTarget()
        {
            // Set drive status
            _drivePanel.StatusText.text = "Driving to 200 µm past target...";

            // Drive
            for (var manipulatorId = 1; manipulatorId <= 2; manipulatorId++)
            {
                // Skip probe if not at dura
                if (!_probeAtDura[manipulatorId - 1]) continue;

                // ID as string
                var idString = manipulatorId.ToString();

                // Get target depth
                var targetDepth = _probeTargetDepth[manipulatorId - 1];

                // Start driving
                _communicationManager.SetCanWrite(idString, true, 1, canWrite =>
                {
                    if (canWrite)
                        _communicationManager.SetInsideBrain(idString, true, _ =>
                        {
                            _communicationManager.DriveToDepth(idString,
                                targetDepth + .2f, DEPTH_DRIVE_SPEED, _ =>
                                {
                                    // Drive back up to target
                                    DriveBackToTarget(idString);
                                }, Debug.LogError);
                        });
                });
            }
        }

        private void DriveBackToTarget(string manipulatorID)
        {
            // Set drive status
            _drivePanel.StatusText.text = "Driving back to target...";

            // Get target insertion
            var targetDepth = _probeTargetDepth[int.Parse(manipulatorID) - 1];

            // Drive
            _communicationManager.DriveToDepth(manipulatorID,
                targetDepth, DEPTH_DRIVE_SPEED, _ =>
                {
                    // Finished movement, and is now settling
                    _probeAtTarget[manipulatorID == "1" ? 0 : 1] = true;

                    // Reset manipulator drive states
                    _communicationManager.SetInsideBrain(manipulatorID, false,
                        _ => { _communicationManager.SetCanWrite(manipulatorID, false, 1, _ => { }, Debug.LogError); },
                        Debug.LogError);

                    // Update status text if both are done
                    if (!_probeAtTarget[0] || !_probeAtTarget[1]) return;
                    _drivePanel.StatusText.text = "Settling... Please wait...";
                }, Debug.LogError);
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

        public void MoveOrStopProbeToInsertionTarget()
        {
            if (_expectedMovements == _completedMovements)
            {
                // All movements completed, pressing means start a new movement set

                // Set button text
                _gotoMoveButtonText.text = "Moving... Press Again to Stop";

                // Compute the number of expected movements
                _expectedMovements += _probe1SelectedTargetProbeInsertion != null ? 1 : 0;
                _expectedMovements += _probe2SelectedTargetProbeInsertion != null ? 1 : 0;

                // Reset completed movements
                _completedMovements = 0;

                // Move probe 1
                if (_probe1SelectedTargetProbeInsertion != null)
                {
                    // Change text color to show movement is happening
                    _gotoPanelText.color = _workingColor;

                    // Calculate movement
                    var manipulatorID = Probe1Manager.ManipulatorId;
                    var automaticMovementSpeed = Probe1Manager.AutomaticMovementSpeed;
                    var apPosition =
                        ConvertInsertionToManipulatorPosition(_probe1MovementAxesInsertions.ap, manipulatorID);
                    var mlPosition =
                        ConvertInsertionToManipulatorPosition(_probe1MovementAxesInsertions.ml, manipulatorID);
                    var dvPosition =
                        ConvertInsertionToManipulatorPosition(_probe1MovementAxesInsertions.dv, manipulatorID);

                    _communicationManager.SetCanWrite(manipulatorID, true, 1, canWrite =>
                    {
                        if (canWrite)
                            _communicationManager.GotoPos(manipulatorID, dvPosition,
                                automaticMovementSpeed, _ =>
                                {
                                    _communicationManager.GotoPos(manipulatorID, apPosition,
                                        automaticMovementSpeed, _ =>
                                        {
                                            _communicationManager.GotoPos(manipulatorID, mlPosition,
                                                automaticMovementSpeed, _ =>
                                                {
                                                    _communicationManager.SetCanWrite(manipulatorID, false, 1, _ =>
                                                    {
                                                        // Hide lines
                                                        _probe1LineGameObjects.ap.SetActive(false);
                                                        _probe1LineGameObjects.ml.SetActive(false);
                                                        _probe1LineGameObjects.dv.SetActive(false);

                                                        // Increment movement counter
                                                        _completedMovements++;

                                                        // Check completion state
                                                        CheckCompletionState();
                                                    }, Debug.LogError);
                                                }, Debug.LogError);
                                        }, Debug.LogError);
                                });
                    });
                }

                // Move probe 2
                if (_probe2SelectedTargetProbeInsertion == null) return;
                {
                    // Change text color to show movement is happening
                    _gotoPanelText.color = _workingColor;

                    // Calculate movement
                    var manipulatorID = Probe2Manager.ManipulatorId;
                    var automaticMovementSpeed = Probe2Manager.AutomaticMovementSpeed;
                    var apPosition =
                        ConvertInsertionToManipulatorPosition(_probe2MovementAxesInsertions.ap, manipulatorID);
                    var mlPosition =
                        ConvertInsertionToManipulatorPosition(_probe2MovementAxesInsertions.ml, manipulatorID);
                    var dvPosition =
                        ConvertInsertionToManipulatorPosition(_probe2MovementAxesInsertions.dv, manipulatorID);

                    _communicationManager.SetCanWrite(manipulatorID, true, 1, canWrite =>
                    {
                        if (canWrite)
                            _communicationManager.GotoPos(manipulatorID, dvPosition,
                                automaticMovementSpeed, _ =>
                                {
                                    _communicationManager.GotoPos(manipulatorID, apPosition,
                                        automaticMovementSpeed, _ =>
                                        {
                                            _communicationManager.GotoPos(manipulatorID, mlPosition,
                                                automaticMovementSpeed, _ =>
                                                {
                                                    _communicationManager.SetCanWrite(manipulatorID, false, 1, _ =>
                                                    {
                                                        // Hide lines
                                                        _probe2LineGameObjects.ap.SetActive(false);
                                                        _probe2LineGameObjects.ml.SetActive(false);
                                                        _probe2LineGameObjects.dv.SetActive(false);

                                                        // Increment movement counter
                                                        _completedMovements++;

                                                        // Check completion state
                                                        CheckCompletionState();
                                                    }, Debug.LogError);
                                                }, Debug.LogError);
                                        }, Debug.LogError);
                                });
                    });
                }
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
                    UpdateMoveButtonInteractable("");
                });
            }
        }

        #endregion

        #region Step 3

        public void ZeroDepth(int manipulatorID)
        {
            switch (manipulatorID)
            {
                case 1:
                    Probe1Manager.SetBrainSurfaceOffset();
                    break;
                case 2:
                    Probe2Manager.SetBrainSurfaceOffset();
                    break;
                default:
                    Debug.LogError("Unknown manipulator ID: " + manipulatorID);

                    // Exit rest of function if failed
                    return;
            }

            // Update whether a probe has been set to Dura
            _probeAtDura[manipulatorID - 1] = true;

            // Enable Step 4 (if needed)
            if (_step != 3) return;
            _step = 4;
            EnableStep4();
        }

        #endregion

        #region Step 4

        public void DriveOrStopDepth()
        {
            if (_isDriving)
            {
                // Stop all movements
                _communicationManager.Stop(state =>
                {
                    if (!state) return;
                    _drivePanel.ButtonText.text = "Drive";
                    _drivePanel.StatusText.text = "Ready to Drive";
                    _drivePanel.TimerText.text = "";
                    _drivePanel.PanelText.color = Color.white;
                    _isDriving = false;
                });
            }
            else
            {
                _drivePanel.ButtonText.text = "Stop";
                _drivePanel.PanelText.color = _workingColor;
                _isDriving = true;

                // Run drive chain
                StartDriveChain();
            }
        }

        #endregion

        #endregion

        #region Components

        #region Colors

        [SerializeField] private Color _readyColor, _workingColor;

        #endregion

        #region Step 1

        [Serializable]
        private class ZeroCoordinatePanelComponents
        {
            public TMP_Text PanelText;
            public GameObject ResetZeroCoordinatePanelPrefab;
            public GameObject PanelScrollView;
            public GameObject PanelScrollViewContent;
            public GameObject ManipulatorsAttachedText;
        }

        [SerializeField] private ZeroCoordinatePanelComponents _zeroCoordinatePanel;

        #endregion

        #region Step 2

        [Serializable]
        private class GotoPanelComponents
        {
            public CanvasGroup CanvasGroup;
            public TMP_Text PanelText;
            public GameObject InsertionSelectionPanelPrefab;
            public GameObject PanelScrollView;
            public GameObject PanelScrollViewContent;
            public GameObject ManipulatorsZeroedText;
            public Button MoveButton;
            public TMP_Text MoveButtonText;
        }

        [SerializeField] private GotoPanelComponents _gotoPanel;

        [SerializeField] private TMP_Text _gotoPanelText;
        [SerializeField] private TMP_Text _gotoMoveButtonText;

        #endregion

        #region Step 3

        [Serializable]
        private class DuraOffsetPanelComponents
        {
            public CanvasGroup CanvasGroup;
            public TMP_Text PanelText;
            public GameObject ResetDuraOffsetPanelPrefab;
            public GameObject PanelScrollView;
            public GameObject PanelScrollViewContent;
            public GameObject ManipulatorsDrivenText;
        }

        [SerializeField] private DuraOffsetPanelComponents _duraOffsetPanel;

        [SerializeField] private CanvasGroup _duraPanelCanvasGroup;
        [SerializeField] private TMP_Text _duraPanelText;

        #endregion

        #region Step 4

        [Serializable]
        private class DrivePanelComponents
        {
            public CanvasGroup CanvasGroup;
            public TMP_Text PanelText;
            public TMP_Text StatusText;
            public TMP_Text TimerText;
            public TMP_Text ButtonText;
        }

        [SerializeField] private DrivePanelComponents _drivePanel;

        #endregion

        private CommunicationManager _communicationManager;

        #endregion

        #region Properties

        private uint _step = 1;

        public List<ProbeManager> ProbeManagers { private get; set; }


        public ProbeManager Probe1Manager { private get; set; }
        public ProbeManager Probe2Manager { private get; set; }

        public CCFAnnotationDataset AnnotationDataset { private get; set; }

        #region Step 2

        public HashSet<string> RightHandedManipulatorIDs { private get; set; }
        public bool IsProbe1ManipulatorRightHanded { private get; set; }
        public bool IsProbe2ManipulatorRightHanded { private get; set; }

        public HashSet<ProbeInsertion> TargetInsertionsReference { private get; set; }
        private ProbeInsertion _probe1SelectedTargetProbeInsertion;
        private ProbeInsertion _probe2SelectedTargetProbeInsertion;


        private (ProbeInsertion ap, ProbeInsertion ml, ProbeInsertion dv) _probe1MovementAxesInsertions;
        private (ProbeInsertion ap, ProbeInsertion ml, ProbeInsertion dv) _probe2MovementAxesInsertions;

        private (GameObject ap, GameObject ml, GameObject dv) _probe1LineGameObjects;

        private (GameObject ap, GameObject ml, GameObject dv) _probe2LineGameObjects;

        private int _expectedMovements;
        private int _completedMovements;

        #endregion

        #region Step 4

        private readonly bool[] _probeAtDura = { false, false };
        private readonly bool[] _probeAtTarget = { false, false };
        private readonly float[] _probeTargetDepth = { 0, 0 };
        private bool _isDriving;
        private float _driveDuration;

        #endregion

        #endregion

        #region Unity

        private void Awake()
        {
            _communicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();
        }

        private void OnEnable()
        {
            // Populate static fields
            ResetZeroCoordinatePanelHandler.ResetZeroCoordinateCallback = AddInsertionSelectionPanel;
            ResetZeroCoordinatePanelHandler.CommunicationManager = _communicationManager;

            InsertionSelectionPanelHandler.TargetInsertionsReference = TargetInsertionsReference;
            InsertionSelectionPanelHandler.AnnotationDataset = AnnotationDataset;
            InsertionSelectionPanelHandler.ShouldUpdateTargetInsertionOptionsEvent.AddListener(
                UpdateMoveButtonInteractable);

            // Enable step 1
            EnableStep1();
        }

        #endregion
    }
}