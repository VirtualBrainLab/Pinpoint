using System;
using EphysLink;
using TMPro;
using UnityEngine;

namespace TrajectoryPlanner.AutomaticManipulatorControl
{
    public class ResetZeroCoordinatePanelHandler : MonoBehaviour
    {
        #region Unity

        private void Start()
        {
            _manipulatorIDText.text = "Manipulator " + ProbeManager.ManipulatorId;
            _manipulatorIDText.color = ProbeManager.GetColor();
        }

        #endregion

        #region UI Functions

        public void ResetZeroCoordinate()
        {
            CommunicationManager.GetPos(ProbeManager.ManipulatorId, zeroCoordinate =>
            {
                ProbeManager.ZeroCoordinateOffset = zeroCoordinate;
                ProbeManager.BrainSurfaceOffset = 0;
                ResetZeroCoordinateCallback.Invoke(ProbeManager);
            });
        }

        #endregion

        #region Components

        [SerializeField] private TMP_Text _manipulatorIDText;

        public ProbeManager ProbeManager { private get; set; }

        public Action<ProbeManager> ResetZeroCoordinateCallback { private get; set; }

        public CommunicationManager CommunicationManager { private get; set; }

        #endregion
    }
}