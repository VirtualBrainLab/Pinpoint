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
            switch (_driveState)
            {
                case DriveState.Ready:
                    // Set UI for driving
                    _buttonText.text = "Stop";
                    _driveState = DriveState.DrivingToTarget;

                    // Run drive chain
                    StartDriveChain();
                    break;
                case DriveState.DrivingToTarget:
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
                    _timerText.text = "Ready for Experiment";
                    _buttonText.text = "Return to surface";
                    _driveState = DriveState.AtTarget;
                    break;
                case DriveState.AtTarget:
                    // Return to surface + 500 dv
                    _buttonText.text = "Stop";
                    _statusText.text = "Returning to surface...";
                    _driveState = DriveState.DrivingToSurface;

                    // Run Drive
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

        private const float DRIVE_PAST_TARGET_DISTANCE = 0.2f;
        private const int DEPTH_DRIVE_SPEED = 2500;
        private const float DRIVE_BACK_TO_TARGET_DURATION = DRIVE_PAST_TARGET_DISTANCE * 1000 / DEPTH_DRIVE_SPEED;

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
        private float _targetDepth;
        private float _targetDriveDuration;
        private float _surfaceDepth;
        private float _surfaceDriveDuration;

        #endregion

        #region Functions

        private void StartDriveChain()
        {
            // Compute drive distance and duration
            CommunicationManager.Instance.GetPos(ProbeManager.ManipulatorId, position =>
            {
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


                // Compute drive duration
                targetDriveDistance += DRIVE_PAST_TARGET_DISTANCE;
                _targetDriveDuration = targetDriveDistance * 1000f / DEPTH_DRIVE_SPEED + DRIVE_BACK_TO_TARGET_DURATION +
                                       Math.Max(120, targetDriveDistance * 600);
                _surfaceDriveDuration = surfaceDriveDistance * 1000f / DEPTH_DRIVE_SPEED +
                                        Math.Max(120, surfaceDriveDistance * 600);

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
                    _statusText.text = "Drive Complete!";
                    if (_driveState == DriveState.DrivingToTarget)
                    {
                        // Set timer and button text
                        _timerText.text = "Ready for Experiment";
                        _buttonText.text = "Return to surface";

                        // Set drive state
                        _driveState = DriveState.AtTarget;
                    }
                    else
                    {
                        // Set timer and button text
                        _timerText.text = "";
                        _buttonText.text = "Drive";

                        // Set drive state
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
                        _targetDepth + DRIVE_PAST_TARGET_DISTANCE, DEPTH_DRIVE_SPEED, _ => DriveBackToTarget(),
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
                _targetDepth, DEPTH_DRIVE_SPEED, _ =>
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

                // Start driving
                CommunicationManager.Instance.DriveToDepth(ProbeManager.ManipulatorId, _surfaceDepth,
                    DEPTH_DRIVE_SPEED, _ =>
                    {
                        // Reset manipulator drive states
                        CommunicationManager.Instance.SetInsideBrain(ProbeManager.ManipulatorId, false, _ =>
                        {
                            CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorId, false, 1,
                                _ => { _statusText.text = ""; },
                                Debug.LogError);
                        }, Debug.LogError);
                    }, Debug.LogError);
            });
        }

        #endregion
    }
}