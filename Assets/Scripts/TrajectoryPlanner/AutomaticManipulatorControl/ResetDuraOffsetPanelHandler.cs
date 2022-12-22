using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TrajectoryPlanner.AutomaticManipulatorControl
{
    public class ResetDuraOffsetPanelHandler : MonoBehaviour
    {
        #region Components

        [SerializeField] private TMP_Text _manipulatorIDText;
        public ProbeManager ProbeManager { private get; set; }

        #endregion

        #region Properties

        public static HashSet<string> ProbesAtDura { private get; set; }
        public static Action EnableStep4Callback { private get; set; }

        #endregion

        #region Unity

        private void Start()
        {
            _manipulatorIDText.text = "Manipulator " + ProbeManager.ManipulatorId;
            _manipulatorIDText.color = ProbeManager.GetColor();
        }

        #endregion

        #region UI Functions

        public void ResetDuraOffset()
        {
            // Reset dura offset
            ProbeManager.SetBrainSurfaceOffset();
            
            // Add probe to list of probes at dura
            ProbesAtDura.Add(ProbeManager.ManipulatorId);
            
            // Enable next step
            EnableStep4Callback.Invoke();
        }

        #endregion
    }
}
