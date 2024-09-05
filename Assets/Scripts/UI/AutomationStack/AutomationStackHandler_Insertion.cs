using System;
using System.Linq;
using UnityEngine.UIElements;

namespace UI.AutomationStack
{
    /// <summary>
    ///     Implements Bregma calibration in the Automation Stack.<br />
    /// </summary>
    public partial class AutomationStackHandler
    {
        #region Properties

        /// <summary>
        ///     Compute the target insertion probe manager from selected target insertion index.
        /// </summary>
        private ProbeManager TargetInsertionProbeManager =>
            _state.SurfaceCoordinateStringToTargetInsertionOptionProbeManagers[
                _state.TargetInsertionOptions.ElementAt(_state.SelectedTargetInsertionIndex)
            ];

        /// <summary>
        ///     Compute the base speed from selected base speed index.
        /// </summary>
        private float BaseSpeed =>
            _state.SelectedBaseSpeedIndex switch
            {
                0 => 0.002f,
                1 => 0.005f,
                2 => 0.01f,
                3 => 0.5f,
                _ => _state.CustomBaseSpeed / 1000f
            };

        #endregion

        #region Implementations

        private partial void OnDriveToTargetInsertionButtonPressed()
        {
            // Throw exception if in an invalid state.
            if (
                !_state.IsDriveToTargetInsertionButtonEnabled
                || _state.DriveToTargetInsertionButtonDisplayStyle == DisplayStyle.None
            )
                throw new InvalidOperationException(
                    "Cannot drive to target insertion if the button is not enabled or visible (ready to drive)."
                );

            // Call drive.
            ActiveManipulatorBehaviorController.Drive(
                TargetInsertionProbeManager,
                BaseSpeed,
                _state.DrivePastTargetDistance / 1000f
            );
        }

        private partial void OnStopDriveButtonPressed()
        {
            // Throw exception if in an invalid state.
            if (_state.StopButtonDisplayStyle == DisplayStyle.None)
                throw new InvalidOperationException(
                    "Cannot stop driving to target insertion if the button is not visible (driving)."
                );

            // Call stop.
            ActiveManipulatorBehaviorController.Stop();
        }

        private partial void OnExitButtonPressed()
        {
            // Throw exception if in an invalid state.
            if (_state.ExitButtonDisplayStyle == DisplayStyle.None)
                throw new InvalidOperationException(
                    "Cannot exit to target insertion if the button is not visible (ready to exit)."
                );

            // Call exit.
            ActiveManipulatorBehaviorController.Exit(TargetInsertionProbeManager, BaseSpeed);
        }

        #endregion
    }
}
