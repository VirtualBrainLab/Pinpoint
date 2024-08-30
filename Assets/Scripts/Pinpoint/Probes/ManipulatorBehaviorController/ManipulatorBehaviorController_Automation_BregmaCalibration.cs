using System;
using System.Globalization;
using EphysLink;

namespace Pinpoint.Probes.ManipulatorBehaviorController
{
    public partial class ManipulatorBehaviorController
    {
        /// <summary>
        ///     Reset zero coordinate of the manipulator
        /// </summary>
        public void ResetZeroCoordinate()
        {
            CommunicationManager.Instance.GetPosition(
                ManipulatorID,
                zeroCoordinate =>
                {
                    ZeroCoordinateOffset = zeroCoordinate;
                    BrainSurfaceOffset = 0;
                }
            );

            // Log event.
            OutputLog.Log(
                new[]
                {
                    "Copilot",
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    "ResetZeroCoordinate",
                    ManipulatorID,
                    ZeroCoordinateOffset.ToString()
                }
            );
        }
    }
}
