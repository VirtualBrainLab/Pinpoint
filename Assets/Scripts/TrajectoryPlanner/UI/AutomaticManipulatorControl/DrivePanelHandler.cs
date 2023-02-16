using System;
using System.Collections;
using EphysLink;
using TMPro;
using UnityEngine;

namespace TrajectoryPlanner.UI.AutomaticManipulatorControl
{
    public class DrivePanelHandler : MonoBehaviour
    {
        #region Unity

        private void Start()
        {
            _manipulatorIDText.text = "Manipulator " + ProbeManager.ManipulatorId;
            _manipulatorIDText.color = ProbeManager.GetColor();
        }

        #endregion

        #region UI Functions

        /// <summary>
        ///     Drive manipulators to 200 µm past target insertion, bring back up to target, and let settle.
        ///     Stops in progress drive.
        /// </summary>
        public void DriveOrStopDepth()
        {
            // Pressed while drive state is...
            switch (_driveState)
            {
                case DriveState.Ready:
                    // Set UI for driving
                    _statusText.text = "Driving to target...";
                    _buttonText.text = "Stop";
                    _driveState = DriveState.DrivingToTarget;

                    // Run drive chain
                    StartDriveChain();
                    break;
                case DriveState.DrivingToTarget:
                    // Stop all movements and set UI to drive to surface
                    CommunicationManager.Instance.Stop(state =>
                    {
                        if (!state) return;
                        _buttonText.text = "Return to Surface";
                        _statusText.text = "Ready to Drive";
                        _timerText.text = "";
                        _driveState = DriveState.AtTarget;
                    });
                    break;
                case DriveState.DrivingToSurface:
                    // Stop all movements and reset UI
                    CommunicationManager.Instance.Stop(state =>
                    {
                        if (!state) return;
                        _buttonText.text = "Drive";
                        _statusText.text = "Ready to Drive";
                        _timerText.text = "";
                        _driveState = DriveState.Ready;
                    });
                    break;
                case DriveState.Settling:
                    // Skip settling and be at target
                    _statusText.text = "";
                    _timerText.text = "Ready for Experiment";
                    _buttonText.text = "Return to surface";
                    _driveState = DriveState.AtTarget;
                    break;
                case DriveState.AtTarget:
                    // Return to surface + 500 dv
                    _statusText.text = "Returning to surface...";
                    _buttonText.text = "Stop";
                    _driveState = DriveState.DrivingToSurface;

                    // Run Drive Back to Surface
                    DriveBackToSurface();
                    break;
                default:
                    Debug.LogError("Unknown drive state: " + _driveState);
                    break;
            }
        }

        #endregion

        #region Constants

        private enum DriveState
        {
            Ready,
            DrivingToTarget,
            AtTarget,
            DrivingToSurface,
            Settling
        }

#if UNITY_EDITOR
        private const float DRIVE_PAST_TARGET_DISTANCE = 0.01f;
        private const int DEPTH_DRIVE_BASE_SPEED = 10; // Hard cap @ 100 um/s
        private const int RETURN_TO_SURFACE_DRIVE_SPEED = 100;
        private const int EXIT_DURA_MARGIN_SPEED = 25;
        private const int OUTSIDE_DRIVE_SPEED = 500; // Hard cap @ 1000 um/s
#else
        private const float DRIVE_PAST_TARGET_DISTANCE = 0.2f;
        private const int DEPTH_DRIVE_BASE_SPEED = 2;
        private const int RETURN_TO_SURFACE_DRIVE_SPEED = 10;
        private const int EXIT_DURA_MARGIN_SPEED = 25;
        private const int OUTSIDE_DRIVE_SPEED = 100;
#endif
        private const float DRIVE_BACK_TO_TARGET_DURATION = DRIVE_PAST_TARGET_DISTANCE * 1000 / DEPTH_DRIVE_BASE_SPEED;
        private const float EXIT_DURA_MARGIN_DURATION = 100f / EXIT_DURA_MARGIN_SPEED;

        #endregion

        #region Components

        [SerializeField] private TMP_Text _manipulatorIDText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _buttonText;

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

        #endregion

        #region Functions

        private void StartDriveChain()
        {
            // Compute drive distance and duration
            CommunicationManager.Instance.GetPos(ProbeManager.ManipulatorId, position =>
            {
                // Remember dura depth
                _duraDepth = position.w;

                // Calibrate target insertion depth based on surface position
                var targetInsertion =
                    InsertionSelectionPanelHandler.SelectedTargetInsertion[ProbeManager.ManipulatorId];
                var targetPositionWorldT = targetInsertion.PositionWorldT();
                var relativePositionWorldT =
                    ProbeManager.GetProbeController().Insertion.PositionWorldT() - targetPositionWorldT;
                var offsetAdjustedRelativeTargetPositionWorldT =
                    Vector3.ProjectOnPlane(relativePositionWorldT, ProbeManager.GetProbeController().ProbeTipT.up);
                var offsetAdjustedTargetPositionWorldT =
                    targetPositionWorldT + offsetAdjustedRelativeTargetPositionWorldT;

                // Converting worldT back to APMLDV (position transformed)
                var offsetAdjustedTargetPosition =
                    targetInsertion.CoordinateTransform.Space2TransformAxisChange(
                        targetInsertion.CoordinateSpace.World2Space(offsetAdjustedTargetPositionWorldT));

                // Update target insertion coordinate
                InsertionSelectionPanelHandler.SelectedTargetInsertion[ProbeManager.ManipulatorId].apmldv =
                    offsetAdjustedTargetPosition;

                // Compute return surface position (500 dv above surface)

                var surfaceInsertion = new ProbeInsertion(0, 0, 0.5f, 0, 0, 0, targetInsertion.CoordinateSpace,
                    targetInsertion.CoordinateTransform, false);
                var surfacePositionWorldT = surfaceInsertion.PositionWorldT();
                var surfacePlane = new Plane(Vector3.down, surfacePositionWorldT);
                var direction = new Ray(ProbeManager.GetProbeController().Insertion.PositionWorldT(),
                    ProbeManager.GetProbeController().ProbeTipT.up);
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
                        InsertionSelectionPanelHandler.SelectedTargetInsertion[ProbeManager.ManipulatorId].apmldv,
                        ProbeManager.GetProbeController().Insertion.apmldv);
                var surfaceDriveDistance = Vector3.Distance(offsetAdjustedSurfacePosition,
                    ProbeManager.GetProbeController().Insertion.apmldv);

                // Draw lines
                Debug.DrawLine(ProbeManager.GetProbeController().Insertion.PositionWorldT(),
                    offsetAdjustedTargetPositionWorldT, Color.red, 60);
                Debug.DrawLine(ProbeManager.GetProbeController().Insertion.PositionWorldT(),
                    offsetAdjustedSurfacePositionWorldT, Color.green, 60);


                // Set target and surface
                _targetDepth = position.w + targetDriveDistance;
                _surfaceDepth = position.w - surfaceDriveDistance;

                // Set drive speeds (base + 1 sec / 1000 um of depth)
                _targetDriveSpeed = Mathf.RoundToInt(DEPTH_DRIVE_BASE_SPEED + targetDriveDistance);

                // Compute drive duration
                targetDriveDistance += DRIVE_PAST_TARGET_DISTANCE;
                _targetDriveDuration = targetDriveDistance * 1000f / _targetDriveSpeed +
                                       DRIVE_BACK_TO_TARGET_DURATION +
                                       Math.Max(120, targetDriveDistance * 600);
                _surfaceDriveDuration = targetDriveDistance * 1000f / RETURN_TO_SURFACE_DRIVE_SPEED +
                                        EXIT_DURA_MARGIN_DURATION +
                                        (surfaceDriveDistance - targetDriveDistance - 0.1f) *
                                        1000f / OUTSIDE_DRIVE_SPEED;

                // Start drive chain
                Drive200PastTarget();
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
                    _driveState is DriveState.DrivingToTarget or DriveState.DrivingToSurface or DriveState.Settling:
                    StartCoroutine(CountDownTimer(seconds - 1));
                    break;
                case <= 0:
                {
                    // Set status to complete
                    _statusText.text = "Drive complete";
                    _timerText.text = "";
                    if (_driveState == DriveState.DrivingToTarget)
                    {
                        // Completed driving to target (finished settling)
                        _buttonText.text = "Return to surface";
                        _driveState = DriveState.AtTarget;
                    }
                    else
                    {
                        // Completed returning to surface
                        _buttonText.text = "Drive";
                        _driveState = DriveState.Ready;
                    }

                    break;
                }
            }
        }

        private void Drive200PastTarget()
        {
            CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorId, true, 1, canWrite =>
            {
                if (!canWrite) return;
                // Set drive status
                _statusText.text = "Driving to 200 µm past target...";

                // Start timer
                StartCoroutine(CountDownTimer(_targetDriveDuration));

                // Drive
                CommunicationManager.Instance.SetInsideBrain(ProbeManager.ManipulatorId, true, _ =>
                {
                    CommunicationManager.Instance.DriveToDepth(ProbeManager.ManipulatorId,
                        _targetDepth + DRIVE_PAST_TARGET_DISTANCE, _targetDriveSpeed, _ => DriveBackToTarget(),
                        Debug.LogError);
                });
            });
        }

        private void DriveBackToTarget()
        {
            // Set drive status
            _statusText.text = "Driving back to target...";

            // Drive
            CommunicationManager.Instance.DriveToDepth(ProbeManager.ManipulatorId,
                _targetDepth, DEPTH_DRIVE_BASE_SPEED, _ =>
                {
                    // Reset manipulator drive states
                    CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorId, false, 1,
                        _ =>
                        {
                            _statusText.text = "Settling... Please wait...";
                            _buttonText.text = "Skip settling";
                            _driveState = DriveState.Settling;
                        },
                        Debug.LogError);
                },
                Debug.LogError);
        }

        private void DriveBackToSurface()
        {
            // Drive
            CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorId, true, 1, canWrite =>
            {
                if (!canWrite) return;
                // Set drive status
                _statusText.text = "Driving back to surface...";

                // Start timer
                StartCoroutine(CountDownTimer(_surfaceDriveDuration));

                // Start driving back to dura
                CommunicationManager.Instance.DriveToDepth(ProbeManager.ManipulatorId, _duraDepth,
                    RETURN_TO_SURFACE_DRIVE_SPEED, _ =>
                    {
                        print("At dura");
                        // Reset manipulator drive states
                        CommunicationManager.Instance.SetInsideBrain(ProbeManager.ManipulatorId, false,
                            setting =>
                            {
                                print("Set outside brain: "+setting);
                                // Drive 100 um to move away from dura
                                CommunicationManager.Instance.DriveToDepth(ProbeManager.ManipulatorId, _duraDepth - .1f,
                                    EXIT_DURA_MARGIN_SPEED,
                                    i =>
                                    {
                                        print("At dura margin: "+i);
                                        // Drive the rest of the way to the surface
                                        CommunicationManager.Instance.DriveToDepth(ProbeManager.ManipulatorId,
                                            _surfaceDepth, OUTSIDE_DRIVE_SPEED, j =>
                                            {
                                                print("At surface depth: "+j);
                                                CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorId,
                                                    false,
                                                    1,
                                                    _ => { _statusText.text = ""; },
                                                    Debug.LogError);
                                            }, Debug.LogError);
                                    }, Debug.LogError);
                            }, Debug.LogError);
                    }, Debug.LogError);
            });
        }

        #endregion
    }
}