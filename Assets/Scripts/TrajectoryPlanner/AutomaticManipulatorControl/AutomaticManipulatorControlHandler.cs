using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EphysLink;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TrajectoryPlanner.AutomaticManipulatorControl
{
    public class AutomaticManipulatorControlHandler : MonoBehaviour
    {
        #region Constants

        private const int DEPTH_DRIVE_SPEED = 5;

        #endregion

        #region Internal UI Functions

        #region Step 1

        private void EnableStep1()
        {
            if (!ProbeManagers.Any()) return;

            // Setup shared resources
            ResetZeroCoordinatePanelHandler.ResetZeroCoordinateCallback = AddInsertionSelectionPanel;
            ResetZeroCoordinatePanelHandler.CommunicationManager = _communicationManager;

            // Add panels
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

            // Setup shared resources
            InsertionSelectionPanelHandler.TargetInsertionsReference = TargetInsertionsReference;
            InsertionSelectionPanelHandler.RightHandedManipulatorIDs = RightHandedManipulatorIDs;
            InsertionSelectionPanelHandler.CommunicationManager = _communicationManager;
            InsertionSelectionPanelHandler.AnnotationDataset = AnnotationDataset;
            InsertionSelectionPanelHandler.ShouldUpdateTargetInsertionOptionsEvent.AddListener(
                UpdateMoveButtonInteractable);
            InsertionSelectionPanelHandler.AddResetDuraOffsetPanelCallback = AddResetDuraOffsetPanel;

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
            _moveToTargetInsertionEvent.AddListener(insertionSelectionPanelHandler.MoveToTargetInsertion);
        }

        private void UpdateMoveButtonInteractable(string _)
        {
            _gotoPanel.MoveButton.interactable = InsertionSelectionPanelHandler.SelectedTargetInsertion.Count > 0;
        }

        private void PostMovementActions()
        {
            // Reset text states
            _gotoPanel.MoveButtonText.text =
                "Move Manipulators into Position";
            _gotoPanel.PanelText.color = Color.white;

            // Enable step 3
            if (_step != 2) return;
            _step = 3;
            EnableStep3();

            // Update button intractability
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

        private void AddResetDuraOffsetPanel(ProbeManager probeManager)
        {
            // Show scroll view
            _duraOffsetPanel.PanelScrollView.SetActive(true);
            _duraOffsetPanel.ManipulatorsDrivenText.SetActive(false);
            
            // Instantiate
            var resetDuraPanelGameObject = Instantiate(_duraOffsetPanel.ResetDuraOffsetPanelPrefab,
                _duraOffsetPanel.PanelScrollViewContent.transform);
            var resetDuraPanelHandler = resetDuraPanelGameObject.GetComponent<ResetDuraOffsetPanelHandler>();

            // Setup
            resetDuraPanelHandler.ProbeManager = probeManager;
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

        #region Step 2

        public void MoveOrStopProbeToInsertionTarget()
        {
            if (!InsertionSelectionPanelHandler.Moving)
            {
                // No movements completed. Pressing means start a new movement set

                // Set button text
                _gotoMoveButtonText.text = "Moving... Press Again to Stop";

                // Trigger movement
                _moveToTargetInsertionEvent.Invoke(PostMovementActions);
            }
            else
            {
                // Movement in progress

                // Stop all movements
                _communicationManager.Stop(state =>
                {
                    if (!state) return;

                    InsertionSelectionPanelHandler.MovementStopped();

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

        private readonly UnityEvent<Action> _moveToTargetInsertionEvent = new();

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
            // Enable step 1
            EnableStep1();
        }

        #endregion
    }
}