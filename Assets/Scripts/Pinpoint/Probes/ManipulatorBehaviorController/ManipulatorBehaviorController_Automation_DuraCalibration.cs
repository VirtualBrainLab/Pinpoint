using System;
using System.Globalization;
using EphysLink;
using UnityEngine;

namespace Pinpoint.Probes.ManipulatorBehaviorController
{
    /// <summary>
    ///     Manage the Dura calibration of the manipulator.
    /// </summary>
    public partial class ManipulatorBehaviorController
    {
        #region Properties

        /// <summary>
        ///     Record of the manipulator's depth coordinate at the Dura.
        /// </summary>
        private float _duraDepth;

        /// <summary>
        ///     Record of the probe's coordinate at the Dura.
        /// </summary>
        private Vector3 _duraCoordinate;

        #endregion

        /// <summary>
        ///     Reset the dura offset of the probe and enable the next step
        /// </summary>
        public void ResetDuraOffset()
        {
            // Reset dura offset.
            ComputeBrainSurfaceOffset();

            // Record the dura depth.
            CommunicationManager.Instance.GetPosition(
                ManipulatorID,
                pos =>
                {
                    _duraDepth = pos.w;
                    _duraCoordinate = _probeController.Insertion.APMLDV;
                }
            );

            // Log the event.
            OutputLog.Log(
                new[]
                {
                    "Automation",
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    "ResetDuraOffset",
                    ManipulatorID,
                    BrainSurfaceOffset.ToString(CultureInfo.InvariantCulture)
                }
            );
        }
    }
}
