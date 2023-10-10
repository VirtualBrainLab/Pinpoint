using System.Collections.Generic;
using EphysLink;
using TMPro;
using TrajectoryPlanner.Probes;
using UnityEngine;

namespace TrajectoryPlanner.UI.EphysCopilot
{
    public class ResetDuraOffsetPanelHandler : MonoBehaviour
    {
        #region Components

        [SerializeField] private TMP_Text _manipulatorIDText;
        public ProbeManager ProbeManager { private get; set; }
        private ManipulatorBehaviorController _manipulatorBehaviorController;

        #endregion

        #region Properties

        public static Dictionary<string, float> ManipulatorIdToDuraDepth = new();

        #endregion
        
        #region Unity

        private void Start()
        {
            _manipulatorBehaviorController = ProbeManager.gameObject.GetComponent<ManipulatorBehaviorController>();

            _manipulatorIDText.text = "Manipulator " + _manipulatorBehaviorController.ManipulatorID;
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
            _manipulatorBehaviorController.ComputeBrainSurfaceOffset();

            CommunicationManager.Instance.GetPos(_manipulatorBehaviorController.ManipulatorID,
                pos => { ManipulatorIdToDuraDepth[_manipulatorBehaviorController.ManipulatorID] = pos.w; });
        }

        #endregion
    }
}