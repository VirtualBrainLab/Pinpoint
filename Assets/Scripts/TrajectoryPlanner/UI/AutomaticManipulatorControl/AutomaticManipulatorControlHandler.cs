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

        #region Internal UI Functions

        #region Step 1

        private void AddResetZeroCoordinatePanel(ProbeManager probeManager)
        {
            // Instantiate
            var resetZeroCoordinatePanelGameObject = Instantiate(
                _zeroCoordinatePanel.ResetZeroCoordinatePanelPrefab,
                _zeroCoordinatePanel.PanelScrollViewContent.transform);
            var resetZeroCoordinatePanelHandler =
                resetZeroCoordinatePanelGameObject.GetComponent<ResetZeroCoordinatePanelHandler>();
            _panels.Add(resetZeroCoordinatePanelGameObject);

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
            _panels.Add(insertionSelectionPanelGameObject);

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
            _panels.Add(resetDuraPanelGameObject);

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
            _panels.Add(addDrivePanelGameObject);

            // Setup
            drivePanelHandler.ProbeManager = probeManager;
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
                CommunicationManager.Instance.Stop(state =>
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
            public GameObject PanelScrollViewContent;
        }

        [SerializeField] private DrivePanelComponents _drivePanel;

        #endregion

        private readonly HashSet<GameObject> _panels = new();

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

        private void OnEnable()
        {
            // Populate properties
            ProbeManagers = ProbeManager.instances.Where(manager => manager.IsEphysLinkControlled).ToList();
            RightHandedManipulatorIDs = ProbeManager.RightHandedManipulatorIDs;
            AnnotationDataset = VolumeDatasetManager.AnnotationDataset;
            TargetInsertionsReference = ProbeInsertion.TargetableInstances;

            // Setup shared resources for panels
            ResetZeroCoordinatePanelHandler.ResetZeroCoordinateCallback = AddInsertionSelectionPanel;
            InsertionSelectionPanelHandler.TargetInsertionsReference = TargetInsertionsReference;
            InsertionSelectionPanelHandler.AnnotationDataset = AnnotationDataset;
            InsertionSelectionPanelHandler.ShouldUpdateTargetInsertionOptionsEvent.AddListener(
                UpdateMoveButtonInteractable);
            InsertionSelectionPanelHandler.AddResetDuraOffsetPanelCallback = AddResetDuraOffsetPanel;
            ResetDuraOffsetPanelHandler.ProbesTargetDepth = _probesTargetDepth;


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
            foreach (var panel in _panels)
            {
                Destroy(panel);
            }
        }

        #endregion
    }
}