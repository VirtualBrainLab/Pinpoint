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
        #region Unity

        private void Start()
        {
            // Set manipulator ID text
            _manipulatorIDText.text = "Manipulator " + ProbeManager.ManipulatorBehaviorController.ManipulatorID;
            _manipulatorIDText.color = ProbeManager.Color;

            // Add drive past distance input field to focusable inputs
            UIManager.FocusableInputs.Add(_drivePastDistanceInputField);

            // Compute with default speed
            OnSpeedChanged(DEPTH_DRIVE_BASE_SPEED);
        }

        #endregion

        #region UI Functions

        public void OnSpeedChanged(float value)
        {
            // Updates speed text and snap slider
            _driveSpeedText.text = "Speed: " + value + " µm/s";
            _driveSpeedSlider.SetValueWithoutNotify((int)value);

            // Warn if speed is too high
            if (!_acknowledgeHighSpeeds && value > 5)
            {
                QuestionDialogue.Instance.YesCallback = () => _acknowledgeHighSpeeds = true;
                QuestionDialogue.Instance.NoCallback = () => OnSpeedChanged(DEPTH_DRIVE_BASE_SPEED);
                QuestionDialogue.Instance.NewQuestion("We don't recommend using an insertion speed above " +
                                                      DEPTH_DRIVE_BASE_SPEED +
                                                      " µm/s. Are you sure you want to continue?");
            }

            // Compute with speed
            ComputeAndSetDriveTime((int)value);
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
                QuestionDialogue.Instance.NoCallback = () => { OnSpeedChanged(_depthDriveBaseSpeed); };
                QuestionDialogue.Instance.NewQuestion(
                    "Please ensure this is for testing purposes only. Do you want to continue?");
            }

            return;

            void UseTestSpeed()
            {
                _acknowledgeHighSpeeds = true;
                OnSpeedChanged(DEPTH_DRIVE_BASE_SPEED_TEST);
                _acknowledgeHighSpeeds = false;
            }
        }

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
            ComputeAndSetDriveTime(_targetDriveSpeed, () =>
            {
                CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                    true, 1,
                    canWrite =>
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
                        CommunicationManager.Instance.SetInsideBrain(
                            ProbeManager.ManipulatorBehaviorController.ManipulatorID, true, _ =>
                            {
                                // FIXME: Dependent on CoordinateSpace direction. Should be standardized by Ephys Link.
                                CommunicationManager.Instance.DriveToDepth(
                                    ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                                    _targetDepth +
                                    ProbeManager.ManipulatorBehaviorController.CoordinateSpace
                                        .World2SpaceAxisChange(Vector3.down).z * _drivePastTargetDistance,
                                    _targetDriveSpeed, _ => DriveBackToTarget(),
                                    Debug.LogError);
                            });
                    });
            });
        }

        public void CompleteDrive()
        {
            _statusText.text = "Drive complete";
            _skipSettlingButton.SetActive(false);
            _returnButton.SetActive(true);
            _driveState = DriveState.AtTarget;
        }

        public void DriveBackToSurface()
        {
            // Drive
            CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorBehaviorController.ManipulatorID, true, 1,
                canWrite =>
                {
                    if (!canWrite) return;
                    // Set drive status and show stop button
                    _statusText.text = "Driving back to surface...";
                    _returnButton.SetActive(false);
                    _stopButton.SetActive(true);
                    _driveState = DriveState.DrivingToSurface;

                    // Start timer
                    StartCoroutine(CountDownTimer(_surfaceDriveDuration, _driveState));

                    // Start driving back to dura
                    CommunicationManager.Instance.DriveToDepth(ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                        _duraDepth,
                        _returnToSurfaceDriveSpeed, _ =>
                        {
                            // Drive 100 um to move away from dura
                            // FIXME: Dependent on CoordinateSpace direction. Should be standardized by Ephys Link.
                            CommunicationManager.Instance.DriveToDepth(
                                ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                                _duraDepth + ProbeManager.ManipulatorBehaviorController.CoordinateSpace
                                    .World2SpaceAxisChange(Vector3.up).z * .1f,
                                _exitDuraMarginSpeed,
                                i =>
                                {
                                    // Drive the rest of the way to the surface
                                    CommunicationManager.Instance.DriveToDepth(
                                        ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                                        _surfaceDepth, _outsideDriveSpeed, j =>
                                        {
                                            // Reset manipulator drive states
                                            CommunicationManager.Instance.SetInsideBrain(
                                                ProbeManager.ManipulatorBehaviorController.ManipulatorID, false,
                                                setting =>
                                                {
                                                    CommunicationManager.Instance.SetCanWrite(
                                                        ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                                                        false,
                                                        1,
                                                        null,
                                                        Debug.LogError);
                                                }, Debug.LogError);
                                        }, Debug.LogError);
                                }, Debug.LogError);
                        }, Debug.LogError);
                });
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

        #region Constants

        private enum DriveState
        {
            Ready,
            DrivingToTarget,
            AtTarget,
            DrivingToSurface
        }

        private const float DRIVE_PAST_TARGET_DISTANCE = 0.05f;

        private const int DEPTH_DRIVE_BASE_SPEED = 5;
        private const int DEPTH_DRIVE_BASE_SPEED_TEST = 500;

        private const float NEAR_TARGET_SPEED_MULTIPLIER = 2f / 3f;

        private const int RETURN_TO_SURFACE_DRIVE_SPEED_MULTIPLIER = 5;

        private const int OUTSIDE_DRIVE_SPEED_MULTIPLIER = 20;

        private const int PER_1000_SPEED = 1;
        private const int PER_1000_SPEED_TEST = 10;

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

        #endregion

        #region Properties

        private bool _acknowledgeOutOfBounds;
        private bool _acknowledgeTestSpeeds;
        private bool _acknowledgeHighSpeeds;

        private DriveState _driveState;
        private float _duraDepth;
        private float _targetDepth;
        private float _targetDriveDuration;
        private float _surfaceDepth;
        private float _surfaceDriveDuration;
        private int _targetDriveSpeed;

        private float _drivePastTargetDistance = DRIVE_PAST_TARGET_DISTANCE;
        private int _depthDriveBaseSpeed = DEPTH_DRIVE_BASE_SPEED;
        private int _returnToSurfaceDriveSpeed;
        private int _exitDuraMarginSpeed;
        private int _outsideDriveSpeed;
        private int _per1000Speed;
        private float _driveBackToTargetDuration;
        private float _exitDuraMarginDuration;

        #endregion

        #region Functions

        private void ComputeAndSetDriveTime(int driveSpeed, Action callback = null)
        {
            // Compute speed variables based on speed
            _depthDriveBaseSpeed = driveSpeed;
            _returnToSurfaceDriveSpeed = driveSpeed * RETURN_TO_SURFACE_DRIVE_SPEED_MULTIPLIER;
            _outsideDriveSpeed = driveSpeed * OUTSIDE_DRIVE_SPEED_MULTIPLIER;
            _per1000Speed = driveSpeed < DEPTH_DRIVE_BASE_SPEED_TEST ? PER_1000_SPEED : PER_1000_SPEED_TEST;
            _driveBackToTargetDuration = _drivePastTargetDistance * 1000 / _depthDriveBaseSpeed;
            _exitDuraMarginDuration = 100f / _returnToSurfaceDriveSpeed;

            // Update drive past distance and return to surface button text
            _returnButtonText.text = "Return to Surface (" + _returnToSurfaceDriveSpeed + " µm/s)";

            // Compute drive distance and duration
            CommunicationManager.Instance.GetPos(ProbeManager.ManipulatorBehaviorController.ManipulatorID, position =>
            {
                // Remember dura depth
                _duraDepth = position.w;

                // Calibrate target insertion depth based on surface position
                var targetInsertion = new ProbeInsertion(
                    InsertionSelectionPanelHandler.ManipulatorIDToSelectedTargetInsertion[
                        ProbeManager.ManipulatorBehaviorController.ManipulatorID]);
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

                // Compute return surface position (500 dv above surface)

                var surfaceInsertion = new ProbeInsertion(0, 0, 0.5f, 0, 0, 0, targetInsertion.CoordinateSpace,
                    targetInsertion.CoordinateTransform, false);
                var surfacePositionWorldT = surfaceInsertion.PositionWorldT();
                var surfacePlane = new Plane(Vector3.down, surfacePositionWorldT);
                var direction = new Ray(ProbeManager.ProbeController.Insertion.PositionWorldT(), probeTipTUp);
                var offsetAdjustedSurfacePositionWorldT = Vector3.zero;

                if (surfacePlane.Raycast(direction, out var distanceToSurface))
                    offsetAdjustedSurfacePositionWorldT = direction.GetPoint(distanceToSurface);

                // Converting worldT back to APMLDV (position transformed)
                var offsetAdjustedSurfacePosition =
                    surfaceInsertion.CoordinateTransform.Space2TransformAxisChange(
                        surfaceInsertion.CoordinateSpace.World2Space(offsetAdjustedSurfacePositionWorldT));

                // Compute drive distances
                var targetDriveDistance =
                    Vector3.Distance(targetInsertion.apmldv, ProbeManager.ProbeController.Insertion.apmldv);
                var surfaceDriveDistance = Vector3.Distance(offsetAdjustedSurfacePosition,
                    ProbeManager.ProbeController.Insertion.apmldv);

                // Set target and surface
                _targetDepth = position.w +
                               ProbeManager.ManipulatorBehaviorController.CoordinateSpace
                                   .World2SpaceAxisChange(Vector3.down).z * targetDriveDistance;
                _surfaceDepth = position.w +
                                ProbeManager.ManipulatorBehaviorController.CoordinateSpace
                                    .World2SpaceAxisChange(Vector3.up).z * surfaceDriveDistance;

                // Warn if target depth is out of bounds
                if (!_acknowledgeOutOfBounds &&
                    (_targetDepth > ProbeManager.ManipulatorBehaviorController.CoordinateSpace.Dimensions.z ||
                     _targetDepth < 0))
                {
                    QuestionDialogue.Instance.NewQuestion(
                        "Target depth is out of bounds. Are you sure you want to continue?");
                    QuestionDialogue.Instance.YesCallback = () => _acknowledgeOutOfBounds = true;
                }

                // Set drive speeds (base + x sec / 1000 um of depth)

                _targetDriveSpeed = Mathf.RoundToInt(_depthDriveBaseSpeed + targetDriveDistance * _per1000Speed);

                // Compute drive duration
                _surfaceDriveDuration = targetDriveDistance * 1000f / _returnToSurfaceDriveSpeed +
                                        _exitDuraMarginDuration +
                                        (surfaceDriveDistance - targetDriveDistance - 0.1f) *
                                        1000f / _outsideDriveSpeed;
                targetDriveDistance += _drivePastTargetDistance;
                _targetDriveDuration = targetDriveDistance * 1000f / _targetDriveSpeed +
                                       _driveBackToTargetDuration +
                                       Math.Max(120, targetDriveDistance * 60);

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


        private void DriveBackToTarget()
        {
            // Set drive status
            _statusText.text = "Driving back to target...";

            // Drive
            CommunicationManager.Instance.DriveToDepth(ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                _targetDepth, _depthDriveBaseSpeed, _ =>
                {
                    // Reset manipulator drive states
                    CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                        false, 1,
                        _ =>
                        {
                            // Set status text
                            _statusText.text = "Settling... Please wait...";

                            // Set buttons
                            _stopButton.SetActive(false);
                            _skipSettlingButton.SetActive(true);
                        },
                        Debug.LogError);
                },
                Debug.LogError);
        }

        #endregion
    }
}