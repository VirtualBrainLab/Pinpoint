using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrajectoryPlanner.UI.EphysCopilot
{
    public class EphysCopilotHandler : MonoBehaviour
    {
        #region Properties

        private readonly Dictionary<ProbeManager, List<GameObject>> _probeManagerToPanels = new();

        #endregion

        #region Unity

        private void Start()
        {
            // Subscribe to changes in Ephys Link connections
            ProbeManager.EphysLinkControlledProbesChangedEvent.AddListener(UpdateManipulatorPanels);
        }

        private void OnEnable()
        {
            // Populate panels on first enable
            UpdateManipulatorPanels(ProbeManager.Instances.Where(manager => manager.IsEphysLinkControlled).ToHashSet());
        }

        #endregion

        #region UI Functions

        private void UpdateManipulatorPanels(HashSet<ProbeManager> ephysLinkControlledProbeManagers)
        {
            // Compute ones that don't have panels and one that should be removed (existing ones stay)
            var newProbeManagers = ephysLinkControlledProbeManagers.Except(_probeManagerToPanels.Keys);
            var removedProbeManagers = _probeManagerToPanels.Keys.Except(ephysLinkControlledProbeManagers);

            // Remove panels for removed probe managers
            foreach (var removedProbeManager in removedProbeManagers.ToList())
            {
                foreach (var panel in _probeManagerToPanels[removedProbeManager]) Destroy(panel);
                _probeManagerToPanels.Remove(removedProbeManager);
            }

            // Spawn panels for new probe managers
            foreach (var probeManager in newProbeManagers)
            {
                // Create list
                _probeManagerToPanels.Add(probeManager, new List<GameObject>());

                // Step 1
                AddResetZeroCoordinatePanel(probeManager);

                // Step 2
                AddInsertionSelectionPanel(probeManager);

                // Step 3
                AddResetDuraOffsetPanel(probeManager);

                // Step 4
                AddDrivePanel(probeManager);
            }

            // Sort panels
            foreach (var probeManager in _probeManagerToPanels.Keys.OrderByDescending(manager =>
                         manager.ManipulatorBehaviorController.ManipulatorID))
            foreach (var panel in _probeManagerToPanels[probeManager])
                panel.transform.SetAsFirstSibling();
        }

        #region Step 1

        private void AddResetZeroCoordinatePanel(ProbeManager probeManager)
        {
            // Instantiate
            var resetZeroCoordinatePanelGameObject = Instantiate(
                _zeroCoordinatePanel.ResetZeroCoordinatePanelPrefab,
                _zeroCoordinatePanel.PanelScrollViewContent.transform);
            var resetZeroCoordinatePanelHandler =
                resetZeroCoordinatePanelGameObject.GetComponent<ResetZeroCoordinatePanelHandler>();
            _probeManagerToPanels[probeManager].Add(resetZeroCoordinatePanelGameObject);

            // Setup
            resetZeroCoordinatePanelHandler.ProbeManager = probeManager;
        }

        #endregion

        #region Step 2

        private void AddInsertionSelectionPanel(ProbeManager probeManager)
        {
            // Instantiate
            var insertionSelectionPanelGameObject = Instantiate(_gotoPanel.InsertionSelectionPanelPrefab,
                _gotoPanel.PanelScrollViewContent.transform);
            var insertionSelectionPanelHandler =
                insertionSelectionPanelGameObject.GetComponent<InsertionSelectionPanelHandler>();
            _probeManagerToPanels[probeManager].Add(insertionSelectionPanelGameObject);

            // Setup
            insertionSelectionPanelHandler.ProbeManager = probeManager;
        }

        #endregion

        #region Step 3

        private void AddResetDuraOffsetPanel(ProbeManager probeManager)
        {
            // Instantiate
            var resetDuraPanelGameObject = Instantiate(_duraOffsetPanel.ResetDuraOffsetPanelPrefab,
                _duraOffsetPanel.PanelScrollViewContent.transform);
            var resetDuraPanelHandler = resetDuraPanelGameObject.GetComponent<ResetDuraOffsetPanelHandler>();


            _probeManagerToPanels[probeManager].Add(resetDuraPanelGameObject);


            // Setup
            resetDuraPanelHandler.ProbeManager = probeManager;
        }

        #endregion

        #region Step 4

        private void AddDrivePanel(ProbeManager probeManager)
        {
            var addDrivePanelGameObject =
                Instantiate(_drivePanel.DrivePanelPrefab, _drivePanel.PanelScrollViewContent.transform);
            var drivePanelHandler = addDrivePanelGameObject.GetComponent<DrivePanelHandler>();
            _probeManagerToPanels[probeManager].Add(addDrivePanelGameObject);

            // Setup
            drivePanelHandler.ProbeManager = probeManager;
        }

        #endregion

        #endregion

        #region Components

        #region Step 1

        [Serializable]
        private class ZeroCoordinatePanelComponents
        {
            public GameObject ResetZeroCoordinatePanelPrefab;
            public GameObject PanelScrollViewContent;
        }

        [SerializeField] private ZeroCoordinatePanelComponents _zeroCoordinatePanel;

        #endregion

        #region Step 2

        [Serializable]
        private class GotoPanelComponents
        {
            public GameObject InsertionSelectionPanelPrefab;
            public GameObject PanelScrollViewContent;
        }

        [SerializeField] private GotoPanelComponents _gotoPanel;

        #endregion

        #region Step 3

        [Serializable]
        private class DuraOffsetPanelComponents
        {
            public GameObject ResetDuraOffsetPanelPrefab;
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

        #endregion
    }
}