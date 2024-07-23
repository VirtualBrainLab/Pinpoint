using System;
using System.Collections.Generic;
using System.Globalization;
using EphysLink;
using TMPro;
using UnityEngine;

namespace Pinpoint.UI.EphysCopilot
{
    public class ResetDuraOffsetPanelHandler : MonoBehaviour
    {
        #region Components

        [SerializeField]
        private TMP_Text _manipulatorIDText;
        public ProbeManager ProbeManager { private get; set; }

        #endregion

        #region Properties

        public static readonly Dictionary<string, float> ManipulatorIdToDuraDepth = new();
        public static readonly Dictionary<string, Vector3> ManipulatorIdToDuraApmldv = new();

        public Action ResetDriveStateToDura { private get; set; }

        #endregion

        #region Unity

        private void Start()
        {
            _manipulatorIDText.text =
                "Manipulator " + ProbeManager.ManipulatorBehaviorController.ManipulatorID;
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
            ProbeManager.ManipulatorBehaviorController.ComputeBrainSurfaceOffset();

            CommunicationManager.Instance.GetPosition(
                ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                pos =>
                {
                    ManipulatorIdToDuraDepth[
                        ProbeManager.ManipulatorBehaviorController.ManipulatorID
                    ] = pos.w;
                    ManipulatorIdToDuraApmldv[
                        ProbeManager.ManipulatorBehaviorController.ManipulatorID
                    ] = ProbeManager.ProbeController.Insertion.APMLDV;
                    ResetDriveStateToDura.Invoke();
                }
            );

            // Log event.
            OutputLog.Log(
                new[]
                {
                    "Copilot",
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    "ResetDuraOffset",
                    ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                    ProbeManager.ManipulatorBehaviorController.BrainSurfaceOffset.ToString(
                        CultureInfo.InvariantCulture
                    )
                }
            );
        }

        #endregion
    }
}
