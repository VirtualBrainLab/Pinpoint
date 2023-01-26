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
                case 0:
                    // Set UI for driving
                    _buttonText.text = "Stop";
                    _driveState++;

                    // Run drive chain
                    StartDriveChain();
                    break;
                case 1:
                    // Stop all movements and reset UI
                    CommunicationManager.Instance.Stop(state =>
                    {
                        if (!state) return;
                        _buttonText.text = "Drive";
                        _statusText.text = "Ready to Drive";
                        _timerText.text = "";
                        _driveState = 0;
                    });
                    break;
                case 2:
                    // Return to surface + 500 dv
                    print("Driving back to surface");
                    _buttonText.text = "Stop";
                    _statusText.text = "Returning to surface...";
                    _driveState = 1;
                    break;
                default:
                    Debug.LogError("Unknown drive state: " + _driveState);
                    break;
            }
        }

        #endregion

        #region Constants

        private const float DRIVE_PAST_TARGET_DISTANCE = 0.2f;
        private const int DEPTH_DRIVE_SPEED = 5;
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

        private int _driveState; // 0 = ready, 1 = driving, 2 = at target
        private float _targetDepth;
        private float _driveDuration;

        #endregion

        #region Functions

        private void StartDriveChain()
        {
            // Compute drive distance and duration
            CommunicationManager.Instance.GetPos(ProbeManager.ManipulatorId, position =>
            {
                // Calibrate target insertion depth to surface position
                var targetInsertion =
                    InsertionSelectionPanelHandler.SelectedTargetInsertion[ProbeManager.ManipulatorId];
                var targetPosition = targetInsertion.PositionWorldT();
                var relativePosition = ProbeManager.GetProbeController().Insertion.PositionWorldT() - targetPosition;
                var offsetAdjustedRelativeTargetPosition =
                    Vector3.ProjectOnPlane(relativePosition, ProbeManager.GetProbeController().ProbeTipT.up);
                print("Target position: " + targetInsertion.PositionWorldT());
                // print("Convert back: " + targetInsertion.World2Transformed(targetInsertion.PositionWorldU()));
                print("Probe position: " + ProbeManager.GetProbeController().Insertion.PositionWorldT());
                print("Plane normal: " + ProbeManager.GetProbeController().ProbeTipT.up);
                print("Offset adjusted relative target position: " + offsetAdjustedRelativeTargetPosition);
                print("Offset adjusted target position: " +
                      (targetPosition + offsetAdjustedRelativeTargetPosition));
                


                // Set target depth
                var driveDistance =
                    Vector3.Distance(
                        InsertionSelectionPanelHandler.SelectedTargetInsertion[ProbeManager.ManipulatorId].apmldv,
                        ProbeManager.GetProbeController().Insertion.apmldv);

                _targetDepth = position.w + driveDistance;

                // Compute drive duration
                driveDistance += DRIVE_PAST_TARGET_DISTANCE;
                _driveDuration = driveDistance * 1000f / DEPTH_DRIVE_SPEED + DRIVE_BACK_TO_TARGET_DURATION +
                                 Math.Max(120, driveDistance * 600);

                // Start timer
                StartCoroutine(CountDownTimer());

                // Start drive chain
                Drive200PastTarget();
            });
        }

        private IEnumerator CountDownTimer()
        {
            // Set timer text
            _timerText.text = TimeSpan.FromSeconds(_driveDuration).ToString(@"mm\:ss");

            // Wait for 1 second
            yield return new WaitForSeconds(1);

            // Decrement timer
            _driveDuration--;

            // Check if timer is done
            if (_driveDuration > 0 && _driveState == 1)
                // Start next timer
            {
                StartCoroutine(CountDownTimer());
            }
            else
            {
                // Set timer text
                _statusText.text = "Drive Complete!";
                _timerText.text = "Ready for Experiment";
                _buttonText.text = "Return to surface";

                // Set drive state
                _driveState = 2;
            }
        }

        private void Drive200PastTarget()
        {
            // Set drive status
            _statusText.text = "Driving to 200 µm past target...";

            // Drive
            // Start driving
            CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorId, true, 1, canWrite =>
            {
                if (canWrite)
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
                    CommunicationManager.Instance.SetInsideBrain(ProbeManager.ManipulatorId, false,
                        _ =>
                        {
                            CommunicationManager.Instance.SetCanWrite(ProbeManager.ManipulatorId, false, 1,
                                _ => { _statusText.text = "Settling... Please wait..."; },
                                Debug.LogError);
                        },
                        Debug.LogError);
                }, Debug.LogError);
        }

        #endregion
    }
}