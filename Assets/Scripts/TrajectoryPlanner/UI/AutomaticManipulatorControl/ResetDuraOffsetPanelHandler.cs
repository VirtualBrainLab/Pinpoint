using System;
using System.Collections.Generic;
using EphysLink;
using TMPro;
using UnityEngine;

namespace TrajectoryPlanner.UI.AutomaticManipulatorControl
{
    public class ResetDuraOffsetPanelHandler : MonoBehaviour
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
        ///     Reset the dura offset of the probe and enable the next step
        /// </summary>
        public void ResetDuraOffset()
        {
            // Reset dura offset
            ProbeManager.SetBrainSurfaceOffset();

            // Record current position and mark down as at dura
            AutomaticManipulatorControlHandler.CommunicationManager.GetPos(ProbeManager.ManipulatorId, position =>
            {
                // Record depth for this probe
                ProbesTargetDepth[ProbeManager.ManipulatorId] = position.w;

                // Enable next step (may do nothing)
                EnableStep4Callback.Invoke();
            });
        }

        #endregion

        #region Components

        [SerializeField] private TMP_Text _manipulatorIDText;
        public ProbeManager ProbeManager { private get; set; }

        #endregion

        #region Properties

        public static Dictionary<string, float> ProbesTargetDepth { private get; set; }
        public static Action EnableStep4Callback { private get; set; }

        #endregion
    }
}