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
        }

        #endregion

        #region Components

        [SerializeField] private TMP_Text _manipulatorIDText;
        public ProbeManager ProbeManager { private get; set; }

        #endregion
    }
}