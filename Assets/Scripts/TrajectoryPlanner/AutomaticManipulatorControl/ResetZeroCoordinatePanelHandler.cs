using System;
using EphysLink;
using TMPro;
using UnityEngine;

namespace TrajectoryPlanner.AutomaticManipulatorControl
{
    public class ResetZeroCoordinatePanelHandler : MonoBehaviour
    {
        public void ResetZeroCoordinate()
        {
            CommunicationManager.GetPos(_probeManager.ManipulatorId, zeroCoordinate =>
            {
                _probeManager.ZeroCoordinateOffset = zeroCoordinate;
                _probeManager.BrainSurfaceOffset = 0;
                _onResetZeroCoordinate.Invoke();
            });
        }

        public void SetupResetZeroCoordinatePanel(ProbeManager probeManager, Action onResetZeroCoordinate)
        {
            _probeManager = probeManager;
            _onResetZeroCoordinate = onResetZeroCoordinate;
            _manipulatorIDText.text = "Manipulator " + probeManager.ManipulatorId;
        }

        #region Components

        [SerializeField] private TMP_Text _manipulatorIDText;

        private ProbeManager _probeManager;

        public CommunicationManager CommunicationManager { private get; set; }
        
        private Action _onResetZeroCoordinate;

        #endregion
    }
}