using System;
using EphysLink;
using TMPro;
using UnityEngine;

namespace TrajectoryPlanner.UI.AutomaticManipulatorControl
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

        /// <summary>
        ///     Reset zero coordinate of the manipulator
        /// </summary>
        public void ResetZeroCoordinate()
        {
            CommunicationManager.Instance.GetPos(ProbeManager.ManipulatorId, zeroCoordinate =>
            {
                ProbeManager.ZeroCoordinateOffset = zeroCoordinate;
                ProbeManager.BrainSurfaceOffset = 0;
                ResetZeroCoordinateCallback.Invoke(ProbeManager);
            });
        }

        #endregion

        #region Components

        #region Shared

        public static Action<ProbeManager> ResetZeroCoordinateCallback { private get; set; }

        #endregion

        [SerializeField] private TMP_Text _manipulatorIDText;

        public ProbeManager ProbeManager { private get; set; }

        #endregion
    }
}