using System;
using System.Collections;
using EphysLink;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrajectoryPlanner.UI.EphysCopilot
{
    public class DrivePanelHandler : MonoBehaviour
    {
        #region Constants

        private enum DriveState
        {
            Ready,
            DrivingToSurface,
            DrivingToTarget,

            Outside,
            ExitingToOutside,
            AtExitMargin,
            ExitingToMargin,
            AtDura,
            ExitingToDura,
            DrivingToNearTarget,
            AtNearTarget,
            ExitingToNearTarget,
            DriveToPastTarget,
            AtPastTarget,
            ReturningToTarget,
            AtTarget,
        }

        private const float DRIVE_PAST_TARGET_DISTANCE = 0.05f;
        private const float DURA_MARGIN_DISTANCE = 0.1f;
        private const float NEAR_TARGET_DISTANCE = 1f;

        private const float DEPTH_DRIVE_BASE_SPEED = 0.005f;
        private const float DEPTH_DRIVE_BASE_SPEED_TEST = 0.5f;

        private const float NEAR_TARGET_SPEED_MULTIPLIER = 2f / 3f;
        private const int EXIT_DRIVE_SPEED_MULTIPLIER = 5;
        private const int OUTSIDE_DRIVE_SPEED_MULTIPLIER = 20;

        private const float PER_1000_SPEED = 0.001f;
        private const float PER_1000_SPEED_TEST = 0.01f;

        #endregion

        #region Components

        [SerializeField] private TMP_Text _manipulatorIDText;
        [SerializeField] private GameObject _driveGroup;
        [SerializeField] private TMP_Text _driveSpeedText;
        [SerializeField] private Slider _driveSpeedSlider;
        [SerializeField] private TMP_InputField _drivePastDistanceInputField;
        [SerializeField] private GameObject _stopButton;
        [SerializeField] private GameObject _skipSettlingButton;
        [SerializeField] private GameObject _returnButton;
        [SerializeField] private TMP_Text _returnButtonText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _timerText;

        public ProbeManager ProbeManager { private get; set; }

        private class DriveStateManager
        {
            // Define state, defaults to at dura
            public DriveState DriveState { get; private set; } = DriveState.AtDura;

            /// <summary>
            ///     Increments drive state to be in progress driving down.
            ///     Unable to drive down when not at a landmark.
            /// </summary>
            public void DriveIncrement()
            {
                switch (DriveState)
                {
                    case DriveState.AtDura:
                        DriveState = DriveState.DrivingToNearTarget;
                        break;
                    case DriveState.AtNearTarget:
                        DriveState = DriveState.DriveToPastTarget;
                        break;
                    case DriveState.AtPastTarget:
                        DriveState = DriveState.ReturningToTarget;
                        break;

                    // Error cases: Cannot drive down from these states
                    case DriveState.Ready:
                    case DriveState.DrivingToSurface:
                    case DriveState.DrivingToTarget:
                    case DriveState.Outside:
                    case DriveState.ExitingToOutside:
                    case DriveState.AtExitMargin:
                    case DriveState.ExitingToMargin:
                    case DriveState.ExitingToDura:
                    case DriveState.DrivingToNearTarget:
                    case DriveState.ExitingToNearTarget:
                    case DriveState.DriveToPastTarget:
                    case DriveState.ReturningToTarget:
                    case DriveState.AtTarget:
                    default:
                        Debug.LogError("Cannot drive down from state: " + DriveState);
                        break;
                }
            }

            public void ExitIncrement()
            {
                switch (DriveState)
                {
                    // Typical case: Increment to next state from landmarks
                    case DriveState.AtExitMargin:
                        DriveState = DriveState.ExitingToOutside;
                        break;
                    case DriveState.AtDura:
                        DriveState = DriveState.ExitingToMargin;
                        break;
                    case DriveState.AtNearTarget:
                        DriveState = DriveState.ExitingToDura;
                        break;
                    case DriveState.AtTarget:
                        DriveState = DriveState.ExitingToNearTarget;
                        break;

                    // Driving transition cases: Switch to exit transition state
                    case DriveState.DrivingToNearTarget:
                        DriveState = DriveState.ExitingToDura;
                        break;
                    case DriveState.DriveToPastTarget or DriveState.ReturningToTarget:
                        DriveState = DriveState.ExitingToNearTarget;
                        break;

                    // Error cases: Cannot exit from these states
                    case DriveState.Ready:
                    case DriveState.DrivingToSurface:
                    case DriveState.DrivingToTarget:
                    case DriveState.Outside:
                    case DriveState.ExitingToMargin:
                    case DriveState.ExitingToDura:
                    case DriveState.ExitingToNearTarget:
                    case DriveState.AtPastTarget:
                    default:
                        Debug.LogError("Cannot exit from state: " + DriveState);
                        break;
                }
            }

            public void MovementCompleted()
            {
                switch (DriveState)
                {
                    // Typical cases (was in driving state)
                    case DriveState.ExitingToOutside:
                        DriveState = DriveState.Outside;
                        break;
                    case DriveState.ExitingToMargin:
                        DriveState = DriveState.AtExitMargin;
                        break;
                    case DriveState.ExitingToDura:
                        DriveState = DriveState.AtDura;
                        break;
                    case DriveState.DrivingToNearTarget:
                        DriveState = DriveState.AtNearTarget;
                        break;
                    case DriveState.ExitingToNearTarget:
                        DriveState = DriveState.AtNearTarget;
                        break;
                    case DriveState.DriveToPastTarget:
                        DriveState = DriveState.AtPastTarget;
                        break;
                    case DriveState.ReturningToTarget:
                        DriveState = DriveState.AtTarget;
                        break;

                    // Error cases: cannot complete movement from non-transitional states
                    case DriveState.Ready:
                    case DriveState.DrivingToSurface:
                    case DriveState.DrivingToTarget:
                    case DriveState.Outside:
                    case DriveState.AtExitMargin:
                    case DriveState.AtDura:
                    case DriveState.AtNearTarget:
                    case DriveState.AtPastTarget:
                    case DriveState.AtTarget:
                    default:
                        Debug.LogError("Cannot complete movement from non-transitional state: " + DriveState);
                        break;
                }
            }
        }

        #endregion

        #region Properties

        private bool _acknowledgeOutOfBounds;
        private bool _acknowledgeTestSpeeds;
        private bool _acknowledgeHighSpeeds;

        private DriveState _driveState;
        private float _duraDepth;
        private float _targetDepth;
        private float _targetDriveDuration;
        private float _duraMarginDepth;
        private float _duraMarginDriveDuration => DURA_MARGIN_DISTANCE / _exitDriveBaseSpeed;
        private float _exitDepth;
        private float _exitDriveDuration;
        private float _targetDriveSpeed;

        private float _drivePastTargetDistance = DRIVE_PAST_TARGET_DISTANCE;
        private float _depthDriveBaseSpeed = DEPTH_DRIVE_BASE_SPEED;
        private float _exitDriveBaseSpeed => _depthDriveBaseSpeed * EXIT_DRIVE_SPEED_MULTIPLIER;
        private float _exitDuraMarginSpeed;
        private float _outsideDriveSpeed;
        private float _per1000Speed;

        #endregion

        #region Unity

        private void Start()
        {
            // Set manipulator ID text
            _manipulatorIDText.text = "Manipulator " + ProbeManager.ManipulatorBehaviorController.ManipulatorID;
            _manipulatorIDText.color = ProbeManager.Color;

            // Add drive past distance input field to focusable inputs
            UIManager.FocusableInputs.Add(_drivePastDistanceInputField);

            // Compute with default speed
            OnSpeedChanged(DEPTH_DRIVE_BASE_SPEED * 1000f);
        }

        #endregion

        #region UI Functions

        /// <summary>
        ///     Change drive speed (input is in um/s to keep whole numbers, but is converted to mm/s for computation)
        /// </summary>
        /// <param name="value">Speed in um/s</param>
        public void OnSpeedChanged(float value)
        {
            // Updates speed text and snap slider
            _driveSpeedText.text = "Speed: " + SpeedToString(value / 1000f);
            _driveSpeedSlider.SetValueWithoutNotify((int)value);

            // Warn if speed is too high
            if (!_acknowledgeHighSpeeds && value > 5)
            {
                QuestionDialogue.Instance.YesCallback = () => _acknowledgeHighSpeeds = true;
                QuestionDialogue.Instance.NoCallback = () => OnSpeedChanged(DEPTH_DRIVE_BASE_SPEED * 1000f);
                QuestionDialogue.Instance.NewQuestion("We don't recommend using an insertion speed above " +
                                                      SpeedToString(DEPTH_DRIVE_BASE_SPEED) +
                                                      ". Are you sure you want to continue?");
            }

            // Compute with speed converted to mm/s
            ComputeAndSetDriveTime(value / 1000f);
        }

        public void OnUseTestSpeedPressed()
        {
            if (_acknowledgeTestSpeeds)
            {
                UseTestSpeed();
            }
            else
            {
                QuestionDialogue.Instance.YesCallback = () =>
                {
                    _acknowledgeTestSpeeds = true;
                    UseTestSpeed();
                };
                QuestionDialogue.Instance.NoCallback = () => { OnSpeedChanged(_depthDriveBaseSpeed * 1000f); };
                QuestionDialogue.Instance.NewQuestion(
                    "Please ensure this is for testing purposes only. Do you want to continue?");
            }

            return;

            void UseTestSpeed()
            {
                _acknowledgeHighSpeeds = true;
                OnSpeedChanged(DEPTH_DRIVE_BASE_SPEED_TEST * 1000f);
                _acknowledgeHighSpeeds = false;
            }
        }

        /// <summary>
        ///     Handle drive past distance (in µm) input field change
        /// </summary>
        /// <param name="value"></param>
        public void OnDrivePastDistanceChanged(string value)
        {
            if (float.TryParse(value, out var distance))
                if (distance > 0)
                {
                    _drivePastTargetDistance = distance / 1000f;
                    return;
                }

            _drivePastTargetDistance = 50;
            _drivePastDistanceInputField.SetTextWithoutNotify("50");
            ComputeAndSetDriveTime(_targetDriveSpeed);
        }

        public void Drive()
        {
            ComputeAndSetDriveTime(_depthDriveBaseSpeed, () =>
            {
                CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                    true, 1, canWrite =>
                    {
                        if (!canWrite) return;
                        // Set drive status
                        _statusText.text = "Driving to " + _drivePastTargetDistance * 1000f + " µm past target...";

                        // Replace drive buttons with stop
                        _driveGroup.SetActive(false);
                        _stopButton.SetActive(true);

                        // Set state
                        _driveState = DriveState.DrivingToTarget;

                        // Start timer
                        StartCoroutine(CountDownTimer(_targetDriveDuration, _driveState));

                        // Drive

                        // Compute initial drive depth (before getting to near target distance)
                        var driveDepth = _duraDepth;
                        if (_targetDepth - _duraDepth > NEAR_TARGET_DISTANCE)
                            driveDepth = _targetDepth - NEAR_TARGET_DISTANCE;

                        // Drive until within near target distance
                        CommunicationManager.Instance.DriveToDepth(
                            ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                            driveDepth,
                            _targetDriveSpeed, _ =>
                            {
                                // Drive through past target distance
                                CommunicationManager.Instance.DriveToDepth(
                                    ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                                    _targetDepth + _drivePastTargetDistance,
                                    _targetDriveSpeed * NEAR_TARGET_SPEED_MULTIPLIER,
                                    _ =>
                                    {
                                        _statusText.text = "Driving back to target...";

                                        // Drive back to target
                                        CommunicationManager.Instance.DriveToDepth(
                                            ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                                            _targetDepth,
                                            _targetDriveSpeed * NEAR_TARGET_SPEED_MULTIPLIER,
                                            _ => StartSettling(), Debug.LogError);
                                    }, Debug.LogError);
                            },
                            Debug.LogError);
                    }, Debug.LogError);
            });
        }


        public void DriveBackToSurface()
        {
            CommunicationManager.Instance.GetPos(ProbeManager.ManipulatorBehaviorController.ManipulatorID, pos =>
            {
                // Drive
                CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                    true, 1,
                    canWrite =>
                    {
                        if (!canWrite) return;
                        // Set drive status and show stop button
                        _statusText.text = "Driving back to surface...";
                        _returnButton.SetActive(false);
                        _stopButton.SetActive(true);
                        _driveState = DriveState.DrivingToSurface;

                        // Start timer
                        StartCoroutine(CountDownTimer(_exitDriveDuration, _driveState));

                        // Drive

                        // Compute drive back to dura (while still in near target distance)
                        var driveDepth = _duraDepth;
                        if (pos.w - _duraDepth > _targetDepth - NEAR_TARGET_DISTANCE - _duraDepth)
                            driveDepth = pos.w - NEAR_TARGET_DISTANCE;

                        // Drive back to dura by near target distance (as much as possible)
                        CommunicationManager.Instance.DriveToDepth(
                            ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                            driveDepth, _exitDriveBaseSpeed * NEAR_TARGET_SPEED_MULTIPLIER, _ =>
                            {
                                // Drive back to dura
                                CommunicationManager.Instance.DriveToDepth(
                                    ProbeManager.ManipulatorBehaviorController.ManipulatorID, _duraDepth,
                                    _exitDriveBaseSpeed, _ =>
                                    {
                                        // Drive out by dura exit margin
                                        // FIXME: Dependent on CoordinateSpace direction. Should be standardized by Ephys Link.
                                        CommunicationManager.Instance.DriveToDepth(
                                            ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                                            _duraDepth - ProbeManager.ManipulatorBehaviorController.CoordinateSpace
                                                .World2SpaceAxisChange(Vector3.up).z * DURA_MARGIN_DISTANCE,
                                            _exitDriveBaseSpeed, _ =>
                                            {
                                                // Drive the rest of the way to the surface
                                                CommunicationManager.Instance.DriveToDepth(
                                                    ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                                                    _exitDepth, _outsideDriveSpeed, _ =>
                                                    {
                                                        // Reset manipulator drive states
                                                        CommunicationManager.Instance.SetCanWrite(
                                                            ProbeManager.ManipulatorBehaviorController
                                                                .ManipulatorID,
                                                            false,
                                                            1,
                                                            null,
                                                            Debug.LogError);
                                                    }, Debug.LogError);
                                            }, Debug.LogError);
                                    }, Debug.LogError);
                            }, Debug.LogError);
                    }, Debug.LogError);
            }, Debug.LogError);
        }

        public void Stop()
        {
            CommunicationManager.Instance.Stop(b =>
            {
                if (!b) return;
                _statusText.text = "Stopped";

                if (_driveState == DriveState.DrivingToTarget)
                {
                    // Setup for returning to surface
                    _driveState = DriveState.AtTarget;
                    _stopButton.SetActive(false);
                    _returnButton.SetActive(true);
                }
                else
                {
                    // Otherwise, just reset
                    _driveState = DriveState.Ready;
                    _stopButton.SetActive(false);
                    _driveGroup.SetActive(true);
                }
            });
        }

        #endregion


        #region Helper Functions

        private void ComputeAndSetDriveTime(float depthDriveBaseSpeed, Action callback = null)
        {
            // Compute speed variables based on speed
            _depthDriveBaseSpeed = depthDriveBaseSpeed;
            _outsideDriveSpeed = depthDriveBaseSpeed * OUTSIDE_DRIVE_SPEED_MULTIPLIER;
            _per1000Speed = depthDriveBaseSpeed < DEPTH_DRIVE_BASE_SPEED_TEST ? PER_1000_SPEED : PER_1000_SPEED_TEST;

            // Update drive past distance and return to surface button text
            _returnButtonText.text = "Return to Surface (" + SpeedToString(_exitDriveBaseSpeed) + ")";

            // Compute drive distance and duration
            CommunicationManager.Instance.GetPos(ProbeManager.ManipulatorBehaviorController.ManipulatorID, position =>
            {
                // Remember dura depth
                _duraDepth = position.w;

                // Calibrate target insertion depth based on surface position
                var targetInsertion = new ProbeInsertion(
                    InsertionSelectionPanelHandler.ManipulatorIDToSelectedTargetProbeManager[
                        ProbeManager.ManipulatorBehaviorController.ManipulatorID].ProbeController.Insertion);
                var targetPositionWorldT = targetInsertion.PositionWorldT();
                var relativePositionWorldT =
                    ProbeManager.ProbeController.Insertion.PositionWorldT() - targetPositionWorldT;
                var probeTipTUp = ProbeManager.ProbeController.ProbeTipT.up;
                var offsetAdjustedRelativeTargetPositionWorldT =
                    Vector3.ProjectOnPlane(relativePositionWorldT, probeTipTUp);
                var offsetAdjustedTargetPositionWorldT =
                    targetPositionWorldT + offsetAdjustedRelativeTargetPositionWorldT;

                // Converting worldT back to APMLDV (position transformed)
                targetInsertion.apmldv =
                    targetInsertion.CoordinateTransform.Space2TransformAxisChange(
                        targetInsertion.CoordinateSpace.World2Space(offsetAdjustedTargetPositionWorldT));

                // Compute return exit position (500 dv above surface)
                var exitInsertion = new ProbeInsertion(0, 0, 0.5f, 0, 0, 0, targetInsertion.CoordinateSpace,
                    targetInsertion.CoordinateTransform, false);
                var exitPositionWorldT = exitInsertion.PositionWorldT();
                var exitPlane = new Plane(Vector3.down, exitPositionWorldT);
                var direction = new Ray(ProbeManager.ProbeController.Insertion.PositionWorldT(), probeTipTUp);
                var offsetAdjustedSurfacePositionWorldT = Vector3.zero;

                if (exitPlane.Raycast(direction, out var distanceToSurface))
                    offsetAdjustedSurfacePositionWorldT = direction.GetPoint(distanceToSurface);

                // Converting worldT back to APMLDV (position transformed)
                var offsetAdjustedSurfacePosition =
                    exitInsertion.CoordinateTransform.Space2TransformAxisChange(
                        exitInsertion.CoordinateSpace.World2Space(offsetAdjustedSurfacePositionWorldT));

                // Compute drive distances
                var targetDriveDistance =
                    Vector3.Distance(targetInsertion.apmldv, ProbeManager.ProbeController.Insertion.apmldv);
                var exitDriveDistance = Vector3.Distance(offsetAdjustedSurfacePosition,
                    ProbeManager.ProbeController.Insertion.apmldv);

                // Set target and exit depths
                _targetDepth = _duraDepth + targetDriveDistance;
                _exitDepth = _duraDepth - exitDriveDistance;

                // Warn if target depth is out of bounds
                if (!_acknowledgeOutOfBounds &&
                    (_targetDepth > ProbeManager.ManipulatorBehaviorController.Dimensions.z || _targetDepth < 0))
                {
                    QuestionDialogue.Instance.NewQuestion(
                        "Target depth is out of bounds. Are you sure you want to continue?");
                    QuestionDialogue.Instance.YesCallback = () => _acknowledgeOutOfBounds = true;
                }

                // Set drive speeds (base + x mm/s / 1 mm of depth)
                _targetDriveSpeed = _depthDriveBaseSpeed + targetDriveDistance * _per1000Speed;

                /*
                 * Compute target drive duration
                 * 1. Drive down towards target until at near target distance at target drive speed
                 * 2. Drive past target by drive past distance at near target speed
                 * 3. Drive back to target at near target speed
                 * 4. Settle for 1 minute per 1 mm of target drive distance with a minimum of 2 minutes
                 */
                _targetDriveDuration =
                    Mathf.Max(0, targetDriveDistance - NEAR_TARGET_DISTANCE) / _targetDriveSpeed +
                    (Mathf.Min(NEAR_TARGET_DISTANCE, targetDriveDistance) + 2 * _drivePastTargetDistance) /
                    (_targetDriveSpeed * NEAR_TARGET_SPEED_MULTIPLIER) +
                    Mathf.Max(120, 60 * (targetDriveDistance + _drivePastTargetDistance));

                /*
                 * Compute exit drive duration
                 * 1. Drive out by near target distance at near target speed
                 * 2. Drive out to dura at exit speed
                 * 3. Drive out by dura margin distance at exit speed
                 * 4. Drive out to surface at outside speed
                 */
                _exitDriveDuration =
                    Mathf.Max(0, NEAR_TARGET_DISTANCE - targetDriveDistance) /
                    (_exitDriveBaseSpeed * NEAR_TARGET_SPEED_MULTIPLIER) +
                    Mathf.Max(0, targetDriveDistance - NEAR_TARGET_DISTANCE) / _exitDriveBaseSpeed +
                    _duraMarginDriveDuration +
                    (exitDriveDistance - targetDriveDistance - DURA_MARGIN_DISTANCE) /
                    _outsideDriveSpeed;

                // Set timer text
                _timerText.text = TimeSpan.FromSeconds(_targetDriveDuration).ToString(@"mm\:ss");

                // Run callback (if any)
                callback?.Invoke();
            });
        }

        private IEnumerator CountDownTimer(float seconds, DriveState driveState)
        {
            // Set timer text
            _timerText.text = TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");

            // Wait for 1 second
            yield return new WaitForSeconds(1);

            switch (seconds)
            {
                // Check if timer is done
                case > 0 when
                    _driveState == driveState:
                    StartCoroutine(CountDownTimer(seconds - 1, driveState));
                    break;
                case <= 0:
                {
                    // Set status to complete
                    _timerText.text = "";
                    if (_driveState == DriveState.DrivingToTarget)
                    {
                        // Completed driving to target (finished settling)
                        CompleteDrive();
                    }
                    else
                    {
                        // Completed returning to surface
                        _statusText.text = "Ready to Drive";
                        _stopButton.SetActive(false);
                        _driveGroup.SetActive(true);
                        _driveState = DriveState.Ready;
                    }

                    break;
                }
                default:
                    _timerText.text = "";
                    break;
            }
        }

        private void CompleteDrive()
        {
            _statusText.text = "Drive complete";
            _skipSettlingButton.SetActive(false);
            _returnButton.SetActive(true);
            _driveState = DriveState.AtTarget;
        }

        private void StartSettling()
        {
            CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorBehaviorController.ManipulatorID, false,
                0,
                _ =>
                {
                    // Set status text
                    _statusText.text = "Settling... Please wait...";

                    // Set buttons
                    _stopButton.SetActive(false);
                    _skipSettlingButton.SetActive(true);
                });
        }

        private static string SpeedToString(float speedMillimeters)
        {
            return Settings.DisplayUM ? speedMillimeters * 1000 + " µm/s" : speedMillimeters + " mm/s";
        }

        #endregion
    }
}