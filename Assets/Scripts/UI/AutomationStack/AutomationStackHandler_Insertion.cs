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
        #region Implementations

        private partial void OnDriveToTargetPressed()
        {
            // Throw exception if invariant is violated.
            if (
                !_state.IsDriveToTargetInsertionButtonEnabled
                || _state.DriveButtonDisplayStyle == DisplayStyle.None
            )
                throw new InvalidOperationException(
                    "Cannot drive to target insertion if the button is not enabled or visible (ready to drive)."
                );

            // Get target insertion probe manager.
            var targetInsertionProbeManager =
                _state.SurfaceCoordinateStringToTargetInsertionOptionProbeManagers[
                    _state.TargetInsertionOptions.ElementAt(_state.SelectedTargetInsertionIndex)
                ];

            // Compute base speed.
            var baseSpeed = _state.SelectedBaseSpeedIndex switch
            {
                0 => 0.002f,
                1 => 0.005f,
                2 => 0.01f,
                3 => 0.5f,
                _ => _state.CustomBaseSpeed / 1000f
            };

            // Call drive.
            ActiveManipulatorBehaviorController.Drive(
                targetInsertionProbeManager,
                baseSpeed,
                _state.DrivePastTargetDistance / 1000f
            );
        }

        private partial void OnStopDrivePressed()
        {
            // Throw exception if invariant is violated.
            if (_state.StopButtonDisplayStyle == DisplayStyle.None)
                throw new InvalidOperationException(
                    "Cannot stop driving to target insertion if the button is not visible (driving)."
                );
            
            // Call stop.
            ActiveManipulatorBehaviorController.Stop();
        }

        private partial void OnExitPressed()
        {
            // Throw exception if invariant is violated.
            if (_state.ExitButtonDisplayStyle == DisplayStyle.None)
                throw new InvalidOperationException(
                    "Cannot exit to target insertion if the button is not visible (ready to exit)."
                );
            
            // Call exit.
        }

        #endregion
    }
}
