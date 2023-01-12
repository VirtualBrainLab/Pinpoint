using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EphysLink;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TrajectoryPlanner.UI.AutomaticManipulatorControl
{
    public class AutomaticManipulatorControlHandler : MonoBehaviour
    {
        #region Constants

        private const float DRIVE_PAST_TARGET_DISTANCE = 0.2f;
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
            // _zeroCoordinatePanel.ManipulatorsAttachedText.SetActive(false);
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
            InsertionSelectionPanelHandler.CommunicationManager = _communicationManager;
            InsertionSelectionPanelHandler.AnnotationDataset = AnnotationDataset;
            InsertionSelectionPanelHandler.ShouldUpdateTargetInsertionOptionsEvent.AddListener(
                UpdateMoveButtonInteractable);
            InsertionSelectionPanelHandler.AddResetDuraOffsetPanelCallback = AddResetDuraOffsetPanel;

            // Enable UI
            // _gotoPanel.CanvasGroup.alpha = 1;
            // _gotoPanel.CanvasGroup.interactable = true;
            // _gotoPanel.PanelText.color = _readyColor;
            // _zeroCoordinatePanel.PanelText.color = Color.white;
            _gotoPanel.PanelScrollView.SetActive(true);
            // _gotoPanel.ManipulatorsZeroedText.SetActive(false);
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
            // _gotoPanel.MoveButton.interactable = InsertionSelectionPanelHandler.SelectedTargetInsertion.Count > 0;
        }

        private void PostMovementActions()
        {
            // Reset text states
            // _gotoPanel.MoveButtonText.text =
            //     "Move Manipulators into Position";
            // _gotoPanel.PanelText.color = Color.white;

            // Enable step 3
            EnableStep3();

            // Update button intractability
            UpdateMoveButtonInteractable("");
        }

        #endregion

        #region Step 3

        private void EnableStep3()
        {
            // Check if needed
            if (_step != 2) return;
            _step = 3;

            // Setup shared resources
            ResetDuraOffsetPanelHandler.EnableStep4Callback = EnableStep4;
            ResetDuraOffsetPanelHandler.ProbesTargetDepth = _probesTargetDepth;
            ResetDuraOffsetPanelHandler.CommunicationManager = _communicationManager;

            // Enable UI
            // _duraOffsetPanel.CanvasGroup.alpha = 1;
            // _duraOffsetPanel.CanvasGroup.interactable = true;
            // _duraOffsetPanel.PanelText.color = Color.green;
            // _gotoPanel.PanelText.color = Color.white;
        }

        private void AddResetDuraOffsetPanel(ProbeManager probeManager)
        {
            // Show scroll view
            _duraOffsetPanel.PanelScrollView.SetActive(true);
            // _duraOffsetPanel.ManipulatorsDrivenText.SetActive(false);

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
            // _drivePanel.CanvasGroup.alpha = 1;
            // _drivePanel.CanvasGroup.interactable = true;
            // _duraOffsetPanel.PanelText.color = Color.white;
            // _drivePanel.PanelText.color = Color.green;
            // _drivePanel.StatusText.text = "Ready to Drive";
        }

        private void AddDrivePanel(ProbeManager probeManager)
        {
            var addDrivePanelGameObject =
                Instantiate(_drivePanel.DrivePanelPrefab, _drivePanel.PanelScrollViewContent.transform);
            var drivePanelHandler = addDrivePanelGameObject.GetComponent<DrivePanelHandler>();

            // Setup
            drivePanelHandler.ProbeManager = probeManager;
        }

        private void StartDriveChain()
        {
            // Compute furthest depth drive distance
            var maxTravelDistance = 0f;
            foreach (var manipulatorID in _probesTargetDepth.Keys.ToList())
            {
                // Compute max travel distance for this probe
                var distance = Vector3.Distance(
                    InsertionSelectionPanelHandler.SelectedTargetInsertion[manipulatorID].apmldv,
                    ProbeManagers.Find(manager => manager.ManipulatorId == manipulatorID).GetProbeController()
                        .Insertion.apmldv);

                // Update target depth for probe
                _probesTargetDepth[manipulatorID] += distance;

                // Update overall max travel distance
                maxTravelDistance = Math.Max(maxTravelDistance, distance + DRIVE_PAST_TARGET_DISTANCE);
            }

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
        }

        private IEnumerator CountDownTimer()
        {
            // Set timer text
            // _drivePanel.TimerText.text = TimeSpan.FromSeconds(_driveDuration).ToString(@"mm\:ss");

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
                // _drivePanel.StatusText.text = "Drive Complete!";
                // _drivePanel.TimerText.text = "Ready for Experiment";
                // _drivePanel.ButtonText.text = "Drive";
                // _drivePanel.PanelText.color = Color.white;
                _probesAtTarget.Clear();
            }
        }

        private void Drive200PastTarget()
        {
            // Set drive status
            // _drivePanel.StatusText.text = "Driving to 200 µm past target...";

            // Drive
            foreach (var kvp in _probesTargetDepth)
                // Start driving
                _communicationManager.SetCanWrite(kvp.Key, true, 1, canWrite =>
                {
                    if (canWrite)
                        _communicationManager.SetInsideBrain(kvp.Key, true, _ =>
                        {
                            _communicationManager.DriveToDepth(kvp.Key,
                                kvp.Value + DRIVE_PAST_TARGET_DISTANCE, DEPTH_DRIVE_SPEED, _ =>
                                {
                                    // Drive back up to target
                                    DriveBackToTarget(kvp.Key);
                                }, Debug.LogError);
                        });
                });
        }

        private void DriveBackToTarget(string manipulatorID)
        {
            // Set drive status
            // _drivePanel.StatusText.text = "Driving back to target...";

            // Drive
            _communicationManager.DriveToDepth(manipulatorID,
                _probesTargetDepth[manipulatorID], DEPTH_DRIVE_SPEED, _ =>
                {
                    // Finished movement, and is now settling
                    _probesAtTarget.Add(manipulatorID);

                    // Reset manipulator drive states
                    _communicationManager.SetInsideBrain(manipulatorID, false,
                        _ => { _communicationManager.SetCanWrite(manipulatorID, false, 1, _ => { }, Debug.LogError); },
                        Debug.LogError);

                    // Update status text if both are done
                    if (_probesAtTarget.Count != _probesTargetDepth.Keys.Count) return;
                    // _drivePanel.StatusText.text = "Settling... Please wait...";
                }, Debug.LogError);
        }

        #endregion

        #endregion

        #region UI Functions

        #region Step 2

        /// <summary>
        ///     Move probes with selected target insertions. Stop in progress movement.
        /// </summary>
        public void MoveOrStopProbeToInsertionTarget()
        {
            if (!InsertionSelectionPanelHandler.Moving)
            {
                // No movements completed. Pressing means start a new movement set

                // Set button text
                // _gotoPanel.MoveButtonText.text = "Moving... Press Again to Stop";

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
                    // _gotoPanel.MoveButtonText.text = "Move Manipulators into Position";

                    // Update button interactable
                    UpdateMoveButtonInteractable("");
                });
            }
        }

        #endregion


        #region Step 4

        /// <summary>
        ///     Drive manipulators to 200 µm past target insertion, bring back up to target, and let settle.
        ///     Stops in progress drive.
        /// </summary>
        public void DriveOrStopDepth()
        {
            if (_isDriving)
            {
                // Stop all movements and reset UI
                _communicationManager.Stop(state =>
                {
                    if (!state) return;
                    // _drivePanel.ButtonText.text = "Drive";
                    // _drivePanel.StatusText.text = "Ready to Drive";
                    // _drivePanel.TimerText.text = "";
                    // _drivePanel.PanelText.color = Color.white;
                    _isDriving = false;
                });
            }
            else
            {
                // Set UI for driving
                // _drivePanel.ButtonText.text = "Stop";
                // _drivePanel.PanelText.color = _workingColor;
                _isDriving = true;
                _probesAtTarget.Clear();

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
            public GameObject ResetZeroCoordinatePanelPrefab;
            public GameObject PanelScrollView;
            public GameObject PanelScrollViewContent;
        }

        [SerializeField] private ZeroCoordinatePanelComponents _zeroCoordinatePanel;

        #endregion

        #region Step 2

        [Serializable]
        private class GotoPanelComponents
        {
            public GameObject InsertionSelectionPanelPrefab;
            public GameObject PanelScrollView;
            public GameObject PanelScrollViewContent;
        }

        [SerializeField] private GotoPanelComponents _gotoPanel;

        #endregion

        #region Step 3

        [Serializable]
        private class DuraOffsetPanelComponents
        {
            public GameObject ResetDuraOffsetPanelPrefab;
            public GameObject PanelScrollView;
            public GameObject PanelScrollViewContent;
        }

        [SerializeField] private DuraOffsetPanelComponents _duraOffsetPanel;

        #endregion

        #region Step 4

        [Serializable]
        private class DrivePanelComponents
        {
            public GameObject DrivePanelPrefab;
            public GameObject PanelScrollView;
            public GameObject PanelScrollViewContent;
        }

        [SerializeField] private DrivePanelComponents _drivePanel;

        #endregion

        private CommunicationManager _communicationManager;

        #endregion

        #region Properties

        private uint _step = 1;

        public List<ProbeManager> ProbeManagers { private get; set; }
        public CCFAnnotationDataset AnnotationDataset { private get; set; }

        #region Step 2

        public HashSet<string> RightHandedManipulatorIDs { private get; set; }
        public HashSet<ProbeInsertion> TargetInsertionsReference { private get; set; }
        private readonly UnityEvent<Action> _moveToTargetInsertionEvent = new();

        #endregion

        #region Step 4

        private readonly Dictionary<string, float> _probesTargetDepth = new();
        private readonly HashSet<string> _probesAtTarget = new();
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
            // Populate properties
            ProbeManagers = ProbeManager.instances.Where(manager => manager.IsEphysLinkControlled).ToList();
            RightHandedManipulatorIDs = ProbeManager.RightHandedManipulatorIDs;
            AnnotationDataset = VolumeDatasetManager.AnnotationDataset;
            TargetInsertionsReference = ProbeInsertion.TargetableInstances;

            // Setup shared resources for panels
            ResetZeroCoordinatePanelHandler.ResetZeroCoordinateCallback = AddInsertionSelectionPanel;
            ResetZeroCoordinatePanelHandler.CommunicationManager = _communicationManager;

            // Spawn panels
            foreach (var probeManager in ProbeManagers)
            {
                // Step 1
                AddResetZeroCoordinatePanel(probeManager);

                // Step 2
                AddInsertionSelectionPanel(probeManager);

                // Step 3
                AddResetDuraOffsetPanel(probeManager);

                // Step 4
                AddDrivePanel(probeManager);
            }
        }

        private void OnDisable()
        {
            foreach (Transform panel in _zeroCoordinatePanel.PanelScrollViewContent.transform)
            {
                Destroy(panel.gameObject);
            }

            foreach (Transform panel in _gotoPanel.PanelScrollViewContent.transform)
            {
                Destroy(panel.gameObject);
            }

            foreach (Transform panel in _duraOffsetPanel.PanelScrollViewContent.transform)
            {
                Destroy(panel.gameObject);
            }

            foreach (Transform panel in _drivePanel.PanelScrollViewContent.transform)
            {
                Destroy(panel.gameObject);
            }
        }

        #endregion
    }
}