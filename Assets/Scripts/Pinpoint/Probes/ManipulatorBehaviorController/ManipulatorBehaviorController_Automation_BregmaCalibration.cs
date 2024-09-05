using System;
using System.Globalization;
using EphysLink;
using UnityEngine;

namespace Pinpoint.Probes.ManipulatorBehaviorController
{
    public partial class ManipulatorBehaviorController
    {
        #region Constants

        /// <summary>
        ///     How far the depth axis can be off center of its range before the user is warned.
        /// </summary>
        /// <remarks>Applies to 3-axis manipulators when calibrating.</remarks>
        private const float CENTER_DEVIATION_FACTOR = 0.125f;

        #endregion

        /// <summary>
        ///     Reset zero coordinate of the manipulator
        /// </summary>
        /// <remarks>
        ///     Alerts user of 4-axis manipulators if depth axis is not at 0 and 3-axis manipulators if depth is too far from
        ///     center.
        /// </remarks>
        public void ResetZeroCoordinate()
        {
            CommunicationManager.Instance.GetPosition(
                ManipulatorID,
                zeroCoordinate =>
                {
                    // Setup alert affirmative callback (continue with reset).
                    QuestionDialogue.Instance.YesCallback = DoReset;

                    // Check depth position and alert.
                    switch (NumAxes)
                    {
                        case 3
                            when Mathf.Abs(Dimensions.z / 2f - zeroCoordinate.w)
                                > CENTER_DEVIATION_FACTOR * Dimensions.z:
                            QuestionDialogue.Instance.NewQuestion(
                                "The depth axis is too far from the center of its range and may not have enough space to reach the target. Are you sure you want to continue?"
                            );
                            break;
                        case 4 when Mathf.Approximately(zeroCoordinate.w, 0f):
                            QuestionDialogue.Instance.NewQuestion(
                                "The depth axis is not at 0 and may not have enough space to reach the target. Are you sure you want to continue?"
                            );
                            break;
                        default:
                            DoReset();
                            break;
                    }

                    return;

                    void DoReset()
                    {
                        ZeroCoordinateOffset = zeroCoordinate;
                        BrainSurfaceOffset = 0;

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
            );
        }
    }
}
