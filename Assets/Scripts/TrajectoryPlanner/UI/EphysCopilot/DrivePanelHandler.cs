using System;
using System.Collections;
using EphysLink;
using TMPro;
using UnityEngine;

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
        }

        #endregion

        #region UI Functions

        public void SetSafeDriveSpeed()
        {
            ComputeAndSetDriveTime(DriveSpeed.Safe);
        }

        public void SetFastDriveSpeed()
        {
            ComputeAndSetDriveTime(DriveSpeed.Fast);
        }

        public void SetTestDriveSpeed()
        {
            ComputeAndSetDriveTime(DriveSpeed.Test);
        }

        public void DriveSafe()
        {
            Drive(DriveSpeed.Safe);
        }

        public void DriveFast()
        {
            Drive(DriveSpeed.Fast);
        }

        public void DriveTest()
        {
            Drive(DriveSpeed.Test);
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
                    StartCoroutine(CountDownTimer(_surfaceDriveDuration));

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
                    _driveButtonGroup.SetActive(true);
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

        public enum DriveSpeed
        {
            Safe,
            Fast,
            Test
        }

        private const float DRIVE_PAST_TARGET_DISTANCE_SAFE = 0.2f;
        private const float DRIVE_PAST_TARGET_DISTANCE_FAST = 0.1f;
        private const float DRIVE_PAST_TARGET_DISTANCE_TEST = 0.05f;

        private const int DEPTH_DRIVE_BASE_SPEED_SAFE = 2;
        private const int DEPTH_DRIVE_BASE_SPEED_FAST = 10;
        private const int DEPTH_DRIVE_BASE_SPEED_TEST = 500;

        private const int RETURN_TO_SURFACE_DRIVE_SPEED_SAFE = 10;
        private const int RETURN_TO_SURFACE_DRIVE_SPEED_FAST = 50;
        private const int RETURN_TO_SURFACE_DRIVE_SPEED_TEST = 500;

        private const int EXIT_DURA_MARGIN_SPEED_SAFE = 25;
        private const int EXIT_DURA_MARGIN_SPEED_FAST = 100;
        private const int EXIT_DURA_MARGIN_SPEED_TEST = 1000;

        private const int OUTSIDE_DRIVE_SPEED_SAFE = 100;
        private const int OUTSIDE_DRIVE_SPEED_FAST = 500;
        private const int OUTSIDE_DRIVE_SPEED_TEST = 1000;

        private const int PER_1000_SPEED_SAFE = 1;
        private const int PER_1000_SPEED_FAST = 5;
        private const int PER_1000_SPEED_TEST = 10;

        #endregion

        #region Components

        [SerializeField] private TMP_Text _manipulatorIDText;
        [SerializeField] private GameObject _driveButtonGroup;
        [SerializeField] private GameObject _stopButton;
        [SerializeField] private GameObject _skipSettlingButton;
        [SerializeField] private GameObject _returnButton;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _timerText;

        public ProbeManager ProbeManager { private get; set; }

        #endregion

        #region Properties

        private DriveState _driveState;
        private float _duraDepth;
        private float _targetDepth;
        private float _targetDriveDuration;
        private float _surfaceDepth;
        private float _surfaceDriveDuration;
        private int _targetDriveSpeed;

        private float _drivePastTargetDistance;
        private int _depthDriveBaseSpeed;
        private int _returnToSurfaceDriveSpeed;
        private int _exitDuraMarginSpeed;
        private int _outsideDriveSpeed;
        private int _per1000Speed;
        private float _driveBackToTargetDuration;
        private float _exitDuraMarginDuration;

        #endregion

        #region Functions

        private void ComputeAndSetDriveTime(DriveSpeed speed, Action callback = null)
        {
            // Compute speed variables based on speed
            _drivePastTargetDistance = speed switch
            {
                DriveSpeed.Safe => DRIVE_PAST_TARGET_DISTANCE_SAFE,
                DriveSpeed.Fast => DRIVE_PAST_TARGET_DISTANCE_FAST,
                DriveSpeed.Test => DRIVE_PAST_TARGET_DISTANCE_TEST,
                _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, null)
            };
            _depthDriveBaseSpeed = speed switch
            {
                DriveSpeed.Safe => DEPTH_DRIVE_BASE_SPEED_SAFE,
                DriveSpeed.Fast => DEPTH_DRIVE_BASE_SPEED_FAST,
                DriveSpeed.Test => DEPTH_DRIVE_BASE_SPEED_TEST,
                _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, null)
            };
            _returnToSurfaceDriveSpeed = speed switch
            {
                DriveSpeed.Safe => RETURN_TO_SURFACE_DRIVE_SPEED_SAFE,
                DriveSpeed.Fast => RETURN_TO_SURFACE_DRIVE_SPEED_FAST,
                DriveSpeed.Test => RETURN_TO_SURFACE_DRIVE_SPEED_TEST,
                _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, null)
            };
            _exitDuraMarginSpeed = speed switch
            {
                DriveSpeed.Safe => EXIT_DURA_MARGIN_SPEED_SAFE,
                DriveSpeed.Fast => EXIT_DURA_MARGIN_SPEED_FAST,
                DriveSpeed.Test => EXIT_DURA_MARGIN_SPEED_TEST,
                _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, null)
            };
            _outsideDriveSpeed = speed switch
            {
                DriveSpeed.Safe => OUTSIDE_DRIVE_SPEED_SAFE,
                DriveSpeed.Fast => OUTSIDE_DRIVE_SPEED_FAST,
                DriveSpeed.Test => OUTSIDE_DRIVE_SPEED_TEST,
                _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, null)
            };
            _per1000Speed = speed switch
            {
                DriveSpeed.Safe => PER_1000_SPEED_SAFE,
                DriveSpeed.Fast => PER_1000_SPEED_FAST,
                DriveSpeed.Test => PER_1000_SPEED_TEST,
                _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, null)
            };
            _driveBackToTargetDuration = _drivePastTargetDistance * 1000 / _depthDriveBaseSpeed;
            _exitDuraMarginDuration = 100f / _exitDuraMarginSpeed;

            // Compute drive distance and duration
            CommunicationManager.Instance.GetPos(ProbeManager.ManipulatorBehaviorController.ManipulatorID, position =>
            {
                // Remember dura depth
                _duraDepth = position.w;

                // Calibrate target insertion depth based on surface position
                var targetInsertion =
                    InsertionSelectionPanelHandler.ManipulatorIDToSelectedTargetInsertion[
                        ProbeManager.ManipulatorBehaviorController.ManipulatorID];
                var targetPositionWorldT = targetInsertion.PositionWorldT();
                var relativePositionWorldT =
                    ProbeManager.ProbeController.Insertion.PositionWorldT() - targetPositionWorldT;
                var probeTipTUp = ProbeManager.ProbeController.ProbeTipT.up;
                var offsetAdjustedRelativeTargetPositionWorldT =
                    Vector3.ProjectOnPlane(relativePositionWorldT, probeTipTUp);
                var offsetAdjustedTargetPositionWorldT =
                    targetPositionWorldT + offsetAdjustedRelativeTargetPositionWorldT;

                // Converting worldT back to APMLDV (position transformed)
                var offsetAdjustedTargetPosition =
                    targetInsertion.CoordinateTransform.Space2TransformAxisChange(
                        targetInsertion.CoordinateSpace.World2Space(offsetAdjustedTargetPositionWorldT));

                // Update target insertion coordinate
                InsertionSelectionPanelHandler
                        .ManipulatorIDToSelectedTargetInsertion[
                            ProbeManager.ManipulatorBehaviorController.ManipulatorID].apmldv =
                    offsetAdjustedTargetPosition;

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
                    Vector3.Distance(
                        InsertionSelectionPanelHandler
                            .ManipulatorIDToSelectedTargetInsertion[
                                ProbeManager.ManipulatorBehaviorController.ManipulatorID].apmldv,
                        ProbeManager.ProbeController.Insertion.apmldv);
                var surfaceDriveDistance = Vector3.Distance(offsetAdjustedSurfacePosition,
                    ProbeManager.ProbeController.Insertion.apmldv);

                // Set target and surface
                _targetDepth = position.w +
                               ProbeManager.ManipulatorBehaviorController.CoordinateSpace
                                   .World2SpaceAxisChange(Vector3.down).z * targetDriveDistance;
                _surfaceDepth = position.w +
                                ProbeManager.ManipulatorBehaviorController.CoordinateSpace
                                    .World2SpaceAxisChange(Vector3.up).z * surfaceDriveDistance;

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

        private void Drive(DriveSpeed speed)
        {
            ComputeAndSetDriveTime(speed, () =>
            {
                CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorBehaviorController.ManipulatorID,
                    true, 1,
                    canWrite =>
                    {
                        if (!canWrite) return;
                        // Set drive status
                        _statusText.text = "Driving to " + _drivePastTargetDistance * 1000f + " µm past target...";

                        // Replace drive buttons with stop
                        _driveButtonGroup.SetActive(false);
                        _stopButton.SetActive(true);

                        // Set state
                        _driveState = DriveState.DrivingToTarget;

                        // Start timer
                        StartCoroutine(CountDownTimer(_targetDriveDuration));

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

        private IEnumerator CountDownTimer(float seconds)
        {
            // Set timer text
            _timerText.text = TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");

            // Wait for 1 second
            yield return new WaitForSeconds(1);

            switch (seconds)
            {
                // Check if timer is done
                case > 0 when
                    _driveState is DriveState.DrivingToTarget or DriveState.DrivingToSurface:
                    StartCoroutine(CountDownTimer(seconds - 1));
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
                        _driveButtonGroup.SetActive(true);
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