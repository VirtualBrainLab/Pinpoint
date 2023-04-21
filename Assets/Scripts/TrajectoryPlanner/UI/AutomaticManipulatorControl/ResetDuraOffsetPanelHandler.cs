using TMPro;
using TrajectoryPlanner.Probes;
using UnityEngine;

namespace TrajectoryPlanner.UI.AutomaticManipulatorControl
{
    public class ResetDuraOffsetPanelHandler : MonoBehaviour
    {
        #region Unity

        private void Start()
        {
            _manipulatorIDText.text = "Manipulator " + ManipulatorBehaviorController.ManipulatorID;
            _manipulatorIDText.color = ProbeManager.Color;
        }

        #endregion

        #region UI Functions

        /// <summary>
        ///     Reset the dura offset of the probe and enable the next step
        /// </summary>
        public void ResetDuraOffset()
        {
            // Reset dura offset
            ManipulatorBehaviorController.SetBrainSurfaceOffset();
        }

        #endregion

        #region Components

        [SerializeField] private TMP_Text _manipulatorIDText;
        public ProbeManager ProbeManager { private get; set; }
        public ManipulatorBehaviorController ManipulatorBehaviorController { private get; set; }

        #endregion
    }
}