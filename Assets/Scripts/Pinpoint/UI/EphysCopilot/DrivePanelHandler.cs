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
            AtTarget
        }

        // Relative distances (in mm)
        private const float OUTSIDE_DISTANCE = 3.5f;
        private const float DURA_MARGIN_DISTANCE = 0.1f;
        private const float NEAR_TARGET_DISTANCE = 1f;
        private const float DRIVE_PAST_TARGET_DISTANCE = 0.05f;

        // Base speeds (in mm/s)
        private const float DEPTH_DRIVE_BASE_SPEED_TEST = 0.5f;
        private const float DEPTH_DRIVE_BASE_SPEED = DEPTH_DRIVE_BASE_SPEED_TEST;
        // private const float DEPTH_DRIVE_BASE_SPEED = 0.005f;

        // Speed multipliers
        private const float NEAR_TARGET_SPEED_MULTIPLIER = 2f / 3f;
        private const int EXIT_DRIVE_SPEED_MULTIPLIER = 5;
        private const int OUTSIDE_DRIVE_SPEED_MULTIPLIER = 20;

        // Per 1000 um speed increase (in mm/s)
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
        [SerializeField] private GameObject _exitButton;
        [SerializeField] private TMP_Text _exitButtonText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _timerText;

        public ProbeManager ProbeManager { private get; set; }

        private class DriveStateManager
        {
            // Define state, defaults to at dura
            public DriveState State { get; private set; } = DriveState.AtDura;

            /// <summary>
            ///     Increments drive state to be in progress driving down.
            ///     Unable to drive down when not at a landmark.
            /// </summary>
            public void DriveIncrement()
            {
                switch (State)
                {
                    case DriveState.AtDura:
                        State = DriveState.DrivingToNearTarget;
                        break;
                    case DriveState.AtNearTarget:
                        State = DriveState.DriveToPastTarget;
                        break;
                    case DriveState.AtPastTarget:
                        State = DriveState.ReturningToTarget;
                        break;

                    // Error cases: Cannot drive down from these states
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
                        Debug.LogError("Cannot drive down from state: " + State);
                        break;
                }
            }

            public void ExitIncrement()
            {
                switch (State)
                {
                    // Typical case: Increment to next state from landmarks
                    case DriveState.AtExitMargin:
                        State = DriveState.ExitingToOutside;
                        break;
                    case DriveState.AtDura:
                        State = DriveState.ExitingToMargin;
                        break;
                    case DriveState.AtNearTarget:
                        State = DriveState.ExitingToDura;
                        break;
                    case DriveState.AtPastTarget:
                    case DriveState.AtTarget:
                        State = DriveState.ExitingToNearTarget;
                        break;

                    // Driving transition cases: Switch to exit transition state
                    case DriveState.DrivingToNearTarget:
                        State = DriveState.ExitingToDura;
                        break;
                    case DriveState.ReturningToTarget:
                    case DriveState.DriveToPastTarget:
                        State = DriveState.ExitingToNearTarget;
                        break;

                    // Error cases: Cannot exit from these states
                    case DriveState.Outside:
                    case DriveState.ExitingToMargin:
                    case DriveState.ExitingToDura:
                    case DriveState.ExitingToNearTarget:
                    case DriveState.ExitingToOutside:
                    default:
                        Debug.LogError("Cannot exit from state: " + State);
                        break;
                }
            }

            public void CompleteMovement()
            {
                switch (State)
                {
                    // Typical cases (was in driving state)
                    case DriveState.ExitingToOutside:
                        State = DriveState.Outside;
                        break;
                    case DriveState.ExitingToMargin:
                        State = DriveState.AtExitMargin;
                        break;
                    case DriveState.ExitingToDura:
                        State = DriveState.AtDura;
                        break;
                    case DriveState.DrivingToNearTarget:
                        State = DriveState.AtNearTarget;
                        break;
                    case DriveState.ExitingToNearTarget:
                        State = DriveState.AtNearTarget;
                        break;
                    case DriveState.DriveToPastTarget:
                        State = DriveState.AtPastTarget;
                        break;
                    case DriveState.ReturningToTarget:
                        State = DriveState.AtTarget;
                        break;

                    // Error cases: cannot complete movement from non-transitional states
                    case DriveState.Outside:
                    case DriveState.AtExitMargin:
                    case DriveState.AtDura:
                    case DriveState.AtNearTarget:
                    case DriveState.AtPastTarget:
                    case DriveState.AtTarget:
                    default:
                        Debug.LogError("Cannot complete movement from non-transitional state: " + State);
                        break;
                }
            }
        }

        #endregion

        #region Properties

        // Manipulator ID
        private string _manipulatorId => ProbeManager.ManipulatorBehaviorController.ManipulatorID;

        // Boundary acknowledgements
        private bool _acknowledgeOutOfBounds;
        private bool _acknowledgeTestSpeeds;
        private bool _acknowledgeHighSpeeds = true;

        // Drive state
        private DriveState _driveState;
        private readonly DriveStateManager _driveStateManager = new();

        // Null checked dura APMLDV (NaN if not on dura)
        private Vector3 _duraAPMLDV => ResetDuraOffsetPanelHandler.ManipulatorIdToDuraApmldv.TryGetValue(
            _manipulatorId, out var duraApmldv)
            ? duraApmldv
            : new Vector3(float.NaN, float.NaN, float.NaN);


        // Target drive distance (returns NaN if not on dura)
        private float _targetDriveDistance
        {
            get
            {
                // Calibrate target insertion depth based on surface position
                var targetInsertion = new ProbeInsertion(
                    InsertionSelectionPanelHandler.ManipulatorIDToSelectedTargetProbeManager[
                        _manipulatorId].ProbeController.Insertion, false);
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

                return Vector3.Distance(targetInsertion.apmldv, _duraAPMLDV);
            }
        }

        // Landmark depths
        private Vector4 _outsidePosition
        {
            get
            {
                // Create outside position APMLDV
                var targetAPMLDV = _duraAPMLDV;
                targetAPMLDV.z = ProbeManager.ProbeController.Insertion
                    .World2TransformedAxisChange(InsertionSelectionPanelHandler.PRE_DEPTH_DRIVE_DV_OFFSET).z;

                // Convert to manipulator position 
                return ProbeManager.ManipulatorBehaviorController.ConvertInsertionAPMLDVToManipulatorPosition(
                    targetAPMLDV);
            }
        }

        private float _outsideDepth => _duraDepth - OUTSIDE_DISTANCE;
        private float _exitMarginDepth => _duraDepth - DURA_MARGIN_DISTANCE;

        private float _duraDepth =>
            ResetDuraOffsetPanelHandler.ManipulatorIdToDuraDepth.TryGetValue(
                _manipulatorId, out var depth)
                ? depth
                : float.NaN;

        private float _targetDepth => _duraDepth + _targetDriveDistance;
        private float _nearTargetDepth => _targetDepth - NEAR_TARGET_DISTANCE;
        private float _drivePastTargetDistance = DRIVE_PAST_TARGET_DISTANCE;
        private float _pastTargetDepth => _targetDepth + _drivePastTargetDistance;

        // Durations
        private float _targetDriveDuration;
        private float _duraMarginDriveDuration => DURA_MARGIN_DISTANCE / _exitDriveBaseSpeed;
        private float _exitDriveDuration;


        // Drive Speeds
        private float _driveBaseSpeed = DEPTH_DRIVE_BASE_SPEED;
        private float _per1000Speed => _driveBaseSpeed <= DEPTH_DRIVE_BASE_SPEED ? PER_1000_SPEED : PER_1000_SPEED_TEST;
        private float _targetDriveSpeed => _driveBaseSpeed + _targetDriveDistance * _per1000Speed;
        private float _nearTargetDriveSpeed => _driveBaseSpeed * NEAR_TARGET_SPEED_MULTIPLIER;
        private float _exitDriveBaseSpeed => _driveBaseSpeed * EXIT_DRIVE_SPEED_MULTIPLIER;
        private float _exitDriveSpeed => _exitDriveBaseSpeed + _targetDriveDistance * _per1000Speed;
        private float _nearTargetExitSpeed => _exitDriveBaseSpeed * NEAR_TARGET_SPEED_MULTIPLIER;
        private float _outsideDriveSpeed => _driveBaseSpeed * OUTSIDE_DRIVE_SPEED_MULTIPLIER;

        #endregion

        #region Unity

        private void Start()
        {
            // Set manipulator ID text
            _manipulatorIDText.text = "Manipulator " + _manipulatorId;
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
            // _driveSpeedText.text = "Speed: " + SpeedToString(value / 1000f);
            // _driveSpeedSlider.SetValueWithoutNotify((int)value);

            // Warn if speed is too high
            if (!_acknowledgeHighSpeeds && value > 5)
            {
                QuestionDialogue.Instance.YesCallback = () => _acknowledgeHighSpeeds = true;
                QuestionDialogue.Instance.NoCallback = () => OnSpeedChanged(DEPTH_DRIVE_BASE_SPEED * 1000f);
                QuestionDialogue.Instance.NewQuestion("We don't recommend using an insertion speed above " +
                                                      SpeedToString(DEPTH_DRIVE_BASE_SPEED) +
                                                      ". Are you sure you want to continue?");
            }

            // Update base speed accordingly
            _driveBaseSpeed = value / 1000f;
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
                QuestionDialogue.Instance.NoCallback = () => { OnSpeedChanged(_driveBaseSpeed * 1000f); };
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
        }

        public void Drive()
        {
            // Get current position
            CommunicationManager.Instance.GetPos(_manipulatorId, position =>
            {
                // Increment state
                _driveStateManager.DriveIncrement();

                CommunicationManager.Instance.SetCanWrite(_manipulatorId, true, 1,
                    canWrite =>
                    {
                        if (!canWrite) return;

                        // Do something based on current state
                        switch (_driveStateManager.State)
                        {
                            case DriveState.DrivingToNearTarget:
                                // Update status text
                                _statusText.text = "Driving to " + _drivePastTargetDistance * 1000f +
                                                   " µm past target...";

                                // Replace drive buttons with stop
                                _driveGroup.SetActive(false);
                                _stopButton.SetActive(true);

                                // Drive to near target depth
                                if (position.w < _nearTargetDepth)
                                    CommunicationManager.Instance.DriveToDepth(_manipulatorId, _nearTargetDepth,
                                        _targetDriveSpeed, _ => CompleteAndAdvance(), Debug.LogError);
                                else
                                    // Already closer than near target depth, so continue
                                    CompleteAndAdvance();
                                break;
                            case DriveState.DriveToPastTarget:
                                // Update status text
                                _statusText.text = "Driving to " + _drivePastTargetDistance * 1000f +
                                                   " µm past target...";

                                // Replace drive buttons with stop
                                _driveGroup.SetActive(false);
                                _stopButton.SetActive(true);

                                // Drive to past target depth
                                if (position.w < _pastTargetDepth)
                                    CommunicationManager.Instance.DriveToDepth(_manipulatorId, _pastTargetDepth,
                                        _nearTargetDriveSpeed, _ => CompleteAndAdvance(), Debug.LogError);
                                else
                                    // Already further than past target depth, so continue
                                    CompleteAndAdvance();
                                break;
                            case DriveState.ReturningToTarget:
                                // Update status text
                                _statusText.text = "Returning to target...";

                                // Replace drive buttons with stop
                                _driveGroup.SetActive(false);
                                _stopButton.SetActive(true);

                                // Drive to target and complete movement
                                CommunicationManager.Instance.DriveToDepth(
                                    _manipulatorId, _targetDepth,
                                    _nearTargetDriveSpeed, _ =>
                                    {
                                        CommunicationManager.Instance.SetCanWrite(_manipulatorId, false, 0,
                                            _ =>
                                            {
                                                _driveStateManager.CompleteMovement();

                                                // Complete driving
                                                _statusText.text = "Drive complete";
                                                _stopButton.SetActive(false);

                                                // Enable return to surface button
                                                _exitButton.SetActive(true);
                                            });
                                    }, Debug.LogError);
                                break;
                            case DriveState.Outside:
                            case DriveState.ExitingToOutside:
                            case DriveState.AtExitMargin:
                            case DriveState.ExitingToMargin:
                            case DriveState.AtDura:
                            case DriveState.ExitingToDura:
                            case DriveState.AtNearTarget:
                            case DriveState.ExitingToNearTarget:
                            case DriveState.AtPastTarget:
                            case DriveState.AtTarget:
                            default:
                                Debug.LogError("Invalid Drive state for driving.");
                                return;
                        }
                    }, Debug.LogError);
            }, Debug.LogError);
            return;

            void CompleteAndAdvance()
            {
                CommunicationManager.Instance.SetCanWrite(_manipulatorId,
                    false, 0,
                    _ =>
                    {
                        _driveStateManager.CompleteMovement();
                        Drive();
                    }, Debug.LogError);
            }
        }

        public void Exit()
        {
            // Get current position
            CommunicationManager.Instance.GetPos(_manipulatorId, position =>
            {
                // Increment state
                _driveStateManager.ExitIncrement();

                CommunicationManager.Instance.SetCanWrite(_manipulatorId, true, 1, canWrite =>
                {
                    if (!canWrite) return;

                    // Do something based on current state
                    switch (_driveStateManager.State)
                    {
                        case DriveState.ExitingToNearTarget:
                            // Update status text
                            _statusText.text = "Returning to surface...";

                            // Replace drive buttons with stop
                            _exitButton.SetActive(false);
                            _stopButton.SetActive(true);

                            // Drive to near target depth
                            if (_nearTargetDepth > _duraDepth && position.w > _nearTargetDepth)
                                CommunicationManager.Instance.DriveToDepth(_manipulatorId, _nearTargetDepth,
                                    _nearTargetExitSpeed, _ => CompleteAndAdvance(), Debug.LogError);
                            else
                                // Dura depth is within near target distance, so continue
                                CompleteAndAdvance();
                            break;
                        case DriveState.ExitingToDura:
                            // Update status text
                            _statusText.text = "Returning to surface...";

                            // Replace drive buttons with stop
                            _exitButton.SetActive(false);
                            _stopButton.SetActive(true);

                            // Drive to dura depth (set speed based on dura depth and near target depth)
                            if (position.w > _duraDepth)
                                CommunicationManager.Instance.DriveToDepth(_manipulatorId, _duraDepth,
                                    position.w > _nearTargetDepth ? _nearTargetExitSpeed : _exitDriveSpeed,
                                    _ => CompleteAndAdvance(), Debug.LogError);
                            else
                                // Already at dura depth, so continue
                                CompleteAndAdvance();
                            break;
                        case DriveState.ExitingToMargin:
                            // Update status text
                            _statusText.text = "Exiting Dura...";

                            // Replace drive buttons with stop
                            _exitButton.SetActive(false);
                            _stopButton.SetActive(true);

                            // Drive to dura margin depth
                            if (position.w > _exitMarginDepth)
                                CommunicationManager.Instance.DriveToDepth(_manipulatorId, _exitMarginDepth,
                                    position.w > _nearTargetDepth ? _nearTargetExitSpeed : _exitDriveSpeed,
                                    _ => CompleteAndAdvance(), Debug.LogError);
                            else
                                // Already at dura margin depth, so continue
                                CompleteAndAdvance();
                            break;
                        case DriveState.ExitingToOutside:
                            // Update status text
                            _statusText.text = "Exiting Dura...";

                            // Replace drive buttons with stop
                            _exitButton.SetActive(false);
                            _stopButton.SetActive(true);

                            // Drive to outside position
                            if (position.y < _outsidePosition.y)
                            {
                                CommunicationManager.Instance.GotoPos(_manipulatorId, _outsidePosition,
                                    _outsideDriveSpeed, _ => CompleteOutside(), Debug.LogError);
                            }
                            // Drive to outside depth if DV movement is unavailable
                            else if (position.w > _outsideDepth)
                                CommunicationManager.Instance.DriveToDepth(_manipulatorId, _outsideDepth,
                                    _outsideDriveSpeed, _ => CompleteOutside(), Debug.LogError);
                            else
                                // Already outside, so complete
                                CompleteOutside();

                            break;
                        case DriveState.Outside:
                        case DriveState.AtExitMargin:
                        case DriveState.AtDura:
                        case DriveState.DrivingToNearTarget:
                        case DriveState.AtNearTarget:
                        case DriveState.DriveToPastTarget:
                        case DriveState.AtPastTarget:
                        case DriveState.ReturningToTarget:
                        case DriveState.AtTarget:
                        default:
                            Debug.LogError("Invalid Drive state for exiting.");
                            return;
                    }
                }, Debug.LogError);
            }, Debug.LogError);
            return;

            void CompleteAndAdvance()
            {
                CommunicationManager.Instance.SetCanWrite(_manipulatorId, false, 0, _ =>
                {
                    _driveStateManager.CompleteMovement();
                    Exit();
                }, Debug.LogError);
            }

            void CompleteOutside()
            {
                CommunicationManager.Instance.SetCanWrite(_manipulatorId, false, 0, _ =>
                {
                    _driveStateManager.CompleteMovement();

                    // Reset UI
                    _statusText.text = "Ready to Drive";
                    _stopButton.SetActive(false);
                    _driveGroup.SetActive(true);
                }, Debug.LogError);
            }
        }

        public void Stop()
        {
            CommunicationManager.Instance.Stop(b =>
            {
                if (!b) return;

                // Reset UI based on state
                _stopButton.SetActive(false);
                switch (_driveStateManager.State)
                {
                    case DriveState.AtExitMargin:
                    case DriveState.AtDura:
                    case DriveState.AtNearTarget:
                    case DriveState.AtTarget:
                    case DriveState.AtPastTarget:
                    case DriveState.DriveToPastTarget:
                    case DriveState.ReturningToTarget:
                    case DriveState.DrivingToNearTarget:
                        _exitButton.SetActive(true);
                        _statusText.text = "Stopped";
                        break;
                    case DriveState.Outside:
                    case DriveState.ExitingToOutside:
                    case DriveState.ExitingToMargin:
                    case DriveState.ExitingToDura:
                    case DriveState.ExitingToNearTarget:
                        _driveGroup.SetActive(true);
                        _statusText.text = "Drive when outside";
                        break;
                    default:
                        Debug.LogError("Unknown state for stopping: " + _driveStateManager.State);
                        break;
                }
            });
        }

        #endregion


        #region Helper Functions

        private static string SpeedToString(float speedMillimeters)
        {
            return Settings.DisplayUM ? speedMillimeters * 1000 + " µm/s" : speedMillimeters + " mm/s";
        }

        #endregion
    }
}