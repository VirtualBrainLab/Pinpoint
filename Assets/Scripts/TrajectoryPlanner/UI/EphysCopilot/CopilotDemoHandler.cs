using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using EphysLink;
using TMPro;
using UnityEngine;

namespace TrajectoryPlanner.UI.EphysCopilot
{
    #region Structures

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal struct DemoDataJson
    {
        public List<ManipulatorDataJson> data;
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal struct ManipulatorDataJson
    {
        public List<float> angle;
        public List<float> idle;
        public List<float> insertion;
    }

    /// <summary>
    ///     Angle: Yaw, Pitch, Roll
    ///     Idle: AP, ML, DV
    ///     Insertion: AP, ML, DV, Depth
    /// </summary>
    internal struct ManipulatorData
    {
        public Vector3 Angle;
        public Vector4 IdlePos;
        public Vector4 EntryCoordinatePos;
        public Vector4 DuraPos;
        public float Depth;
    }

    internal enum ManipulatorState
    {
        Idle,
        Calibrated,
        AtEntryCoordinate,
        AtDura,
        Inserted,
        Retracted,
        Traveling
    }

    #endregion


    public class CopilotDemoHandler : MonoBehaviour
    {
        #region Constants

        // Manipulator movement speed when outside in mm/s
        private const float OUTSIDE_MOVEMENT_SPEED = 1.5f;

        // Manipulator movement speed when inside in mm/s
        private const float INSIDE_MOVEMENT_SPEED = .75f;

        // Manipulator movement speed when close to target in mm/s
        private const float CLOSE_MOVEMENT_SPEED = 0.3f;

        // DV ceiling in um
        private const float DV_CEILING = 3500f;

        // Close to target distance (mm)
        private const float CLOSE_TO_TARGET_DISTANCE = 0.1f;

        // Exit margin depth (mm)
        private const float EXIT_MARGIN_DEPTH = 0.1f;

        // Go past distance (mm)
        private const float GO_PAST_DISTANCE = 0.05f;

        // Pause time in seconds
        private const long PAUSE_TIME = 1;

        #endregion

        #region Components

        [SerializeField] private TMP_Text _calibratingToBregmaText;
        [SerializeField] private TMP_Text _goingToEntryCoordinateText;
        [SerializeField] private TMP_Text _goingToDuraText;
        [SerializeField] private TMP_Text _insertingText;
        [SerializeField] private TMP_Text _retractingText;

        [SerializeField] private GameObject _startButton;
        [SerializeField] private GameObject _stopButton;
        [SerializeField] private BrainCameraController _brainCameraController;

        #endregion

        #region Properties

        private readonly Dictionary<ProbeManager, ManipulatorData> _demoManipulatorToData = new();
        private readonly Dictionary<ProbeManager, ManipulatorState> _manipulatorToStates = new();


        // Text progress colors
        private static Color WaitingColor => ProbeProperties.ProbeColors[15];
        private static Color InProgressColor => ProbeProperties.ProbeColors[3];
        private static Color CompletedColor => ProbeProperties.ProbeColors[5];

        #endregion

        #region Unity

        private void OnEnable()
        {
            // Parse JSON
            var jsonString = File.ReadAllText(Application.streamingAssetsPath + "/copilot_demo.json");
            var data = JsonUtility.FromJson<DemoDataJson>(jsonString);

            // Convert to ManipulatorData and match with manipulator
            foreach (var manipulatorData in data.data)
            {
                var convertedAngle = new Vector3(manipulatorData.angle[0], manipulatorData.angle[1],
                    manipulatorData.angle[2]);

                // Match to manipulator
                var matchingManipulator = ProbeManager.Instances.FirstOrDefault(
                    manager => manager.IsEphysLinkControlled &&
                               IsCoterminal(manager.ProbeController.Insertion.angles, convertedAngle));

                // Skip if there are no matching manipulators
                if (matchingManipulator == null) continue;

                // Convert data
                var convertedData = new ManipulatorData
                {
                    Angle = convertedAngle,
                    IdlePos =
                        matchingManipulator.ManipulatorBehaviorController.ConvertInsertionAPMLDVToManipulatorPosition(
                            new Vector3(manipulatorData.idle[0], manipulatorData.idle[1], manipulatorData.idle[2]) /
                            1000f),
                    EntryCoordinatePos =
                        matchingManipulator.ManipulatorBehaviorController.ConvertInsertionAPMLDVToManipulatorPosition(
                            new Vector3(manipulatorData.insertion[0], manipulatorData.insertion[1], DV_CEILING) /
                            1000f),
                    DuraPos =
                        matchingManipulator.ManipulatorBehaviorController.ConvertInsertionAPMLDVToManipulatorPosition(
                            new Vector3(manipulatorData.insertion[0], manipulatorData.insertion[1],
                                manipulatorData.insertion[2]) / 1000f)
                };
                convertedData.Depth = convertedData.DuraPos.w + manipulatorData.insertion[3] / 1000f;

                _demoManipulatorToData.Add(matchingManipulator, convertedData);

                // Default to traveling state on setup (will be moving to Idle soon)
                _manipulatorToStates.Add(matchingManipulator, ManipulatorState.Traveling);
            }

            // Show start button if there are manipulators to control
            _startButton.SetActive(_demoManipulatorToData.Count > 0);
        }

        private void Update()
        {
            if (_manipulatorToStates.Values.All(state => state == ManipulatorState.Idle))
            {
                print("All manipulators are at idle");

                // Set text colors
                _calibratingToBregmaText.color = InProgressColor;
                _goingToEntryCoordinateText.color = WaitingColor;
                _goingToDuraText.color = WaitingColor;
                _insertingText.color = WaitingColor;
                _retractingText.color = WaitingColor;
                
                // Set state to traveling
                SetAllToTraveling();

                // Chill for a bit then calibrate
                StartCoroutine(Pause(Calibrate));
            }
            else if (_manipulatorToStates.Values.All(state => state == ManipulatorState.Calibrated))
            {
                print("All manipulators are calibrated");

                // Set text colors
                _calibratingToBregmaText.color = CompletedColor;
                _goingToEntryCoordinateText.color = InProgressColor;
                _goingToDuraText.color = WaitingColor;
                _insertingText.color = WaitingColor;
                _retractingText.color = WaitingColor;

                // Set state to traveling
                SetAllToTraveling();
                
                // Chill for a bit then go to entry coordinate
                StartCoroutine(Pause(GoToEntryCoordinate));
            }
            else if (_manipulatorToStates.Values.All(state => state == ManipulatorState.AtEntryCoordinate))
            {
                print("All manipulators are at entry coordinate");

                // Set text colors
                _calibratingToBregmaText.color = CompletedColor;
                _goingToEntryCoordinateText.color = CompletedColor;
                _goingToDuraText.color = InProgressColor;
                _insertingText.color = WaitingColor;
                _retractingText.color = WaitingColor;
                
                // Set state to traveling
                SetAllToTraveling();

                // Chill for a bit then go to dura
                StartCoroutine(Pause(GoToDura));
            }
            else if (_manipulatorToStates.Values.All(state => state == ManipulatorState.AtDura))
            {
                print("All manipulators are at dura");

                // Set text colors
                _calibratingToBregmaText.color = CompletedColor;
                _goingToEntryCoordinateText.color = CompletedColor;
                _goingToDuraText.color = CompletedColor;
                _insertingText.color = InProgressColor;
                _retractingText.color = WaitingColor;
                
                // Set state to traveling
                SetAllToTraveling();

                // Chill for a bit then go to target insertion
                StartCoroutine(Pause(Insert));
            }
            else if (_manipulatorToStates.Values.All(state => state == ManipulatorState.Inserted))
            {
                print("All manipulators are inserted");

                // Set text colors
                _calibratingToBregmaText.color = CompletedColor;
                _goingToEntryCoordinateText.color = CompletedColor;
                _goingToDuraText.color = CompletedColor;
                _insertingText.color = CompletedColor;
                _retractingText.color = InProgressColor;
                
                // Set state to traveling
                SetAllToTraveling();

                // Chill for a bit then bring them back out
                StartCoroutine(Pause(Retract));
            }
            else if (_manipulatorToStates.Values.All(state => state == ManipulatorState.Retracted))
            {
                print("All manipulators are retracted");

                // Set text colors
                _calibratingToBregmaText.color = CompletedColor;
                _goingToEntryCoordinateText.color = CompletedColor;
                _goingToDuraText.color = CompletedColor;
                _insertingText.color = CompletedColor;
                _retractingText.color = CompletedColor;
                
                // Set state to traveling
                SetAllToTraveling();

                // Chill for a bit then go back to idle
                StartCoroutine(Pause(GoToIdle));
            }
        }

        #endregion

        #region UI Functions

        public void OnStartPressed()
        {
            // Set all manipulators to can write
            var manipulatorIndex = 0;
            SetCanWrite(_demoManipulatorToData.Keys.ToList()[manipulatorIndex]);
            return;

            void SetCanWrite(ProbeManager manipulator)
            {
                CommunicationManager.Instance.SetCanWrite(manipulator.ManipulatorBehaviorController.ManipulatorID, true,
                    100, _ =>
                    {
                        if (++manipulatorIndex < _demoManipulatorToData.Count)
                            SetCanWrite(_demoManipulatorToData.Keys.ToList()[manipulatorIndex]);
                        else
                            ActuallyStart();
                    });
            }

            void ActuallyStart()
            {
                // Swap start and stop buttons
                _startButton.SetActive(false);
                _stopButton.SetActive(true);

                // Reset state colors
                _calibratingToBregmaText.color = WaitingColor;
                _goingToEntryCoordinateText.color = WaitingColor;
                _goingToDuraText.color = WaitingColor;
                _insertingText.color = WaitingColor;
                _retractingText.color = WaitingColor;

                // Start brain rotation
                _brainCameraController.SetCameraContinuousRotation(true);

                // Move to idle position
                SetAllToTraveling();
                GoToIdle();
            }
        }

        public void OnStopPressed()
        {
            CommunicationManager.Instance.Stop(_ => print("Stopped"));

            // Swap start and stop buttons
            _startButton.SetActive(true);
            _stopButton.SetActive(false);

            // Stop brain rotation
            _brainCameraController.SetCameraContinuousRotation(false);
        }

        #endregion

        #region Movement Functions

        private void GoToIdle()
        {
            foreach (var manipulatorToData in _demoManipulatorToData)
                CommunicationManager.Instance.GotoPos(
                    manipulatorToData.Key.ManipulatorBehaviorController.ManipulatorID,
                    manipulatorToData.Value.IdlePos, OUTSIDE_MOVEMENT_SPEED,
                    _ => _manipulatorToStates[manipulatorToData.Key] = ManipulatorState.Idle, Debug.LogError);
        }

        private void Calibrate()
        {
            var manipulatorIndex = 0;
            CalibrateManipulator(_demoManipulatorToData.Keys.ToList()[manipulatorIndex]);
            return;

            void CalibrateManipulator(ProbeManager manipulator)
            {
                var manipulatorBehaviorController = manipulator.ManipulatorBehaviorController;

                // Goto above bregma then down to bregma
                CommunicationManager.Instance.GotoPos(manipulatorBehaviorController.ManipulatorID,
                    manipulatorBehaviorController.ZeroCoordinateOffset + new Vector4(0, DV_CEILING / 1000f, 0, 0),
                    OUTSIDE_MOVEMENT_SPEED, _ =>
                        CommunicationManager.Instance.GotoPos(manipulatorBehaviorController.ManipulatorID,
                            manipulatorBehaviorController.ZeroCoordinateOffset,
                            OUTSIDE_MOVEMENT_SPEED,
                            _ =>
                            {
                                // Come back to idle
                                StartCoroutine(Pause(() => CommunicationManager.Instance.GotoPos(
                                    manipulatorBehaviorController.ManipulatorID,
                                    _demoManipulatorToData[manipulator].IdlePos,
                                    OUTSIDE_MOVEMENT_SPEED,
                                    _ =>
                                    {
                                        // Complete and start next manipulator
                                        StartCoroutine(Pause(() =>
                                        {
                                            _manipulatorToStates[manipulator] = ManipulatorState.Calibrated;
                                            if (++manipulatorIndex < _demoManipulatorToData.Count)
                                                CalibrateManipulator(
                                                    _demoManipulatorToData.Keys.ToList()[manipulatorIndex]);
                                        }));
                                    }, Debug.LogError)));
                            }, Debug.LogError),
                    Debug.LogError);
            }
        }

        private void GoToEntryCoordinate()
        {
            foreach (var manipulatorData in _demoManipulatorToData)
                CommunicationManager.Instance.GotoPos(manipulatorData.Key.ManipulatorBehaviorController.ManipulatorID,
                    manipulatorData.Value.EntryCoordinatePos, OUTSIDE_MOVEMENT_SPEED,
                    _ => _manipulatorToStates[manipulatorData.Key] = ManipulatorState.AtEntryCoordinate,
                    Debug.LogError);
        }

        private void GoToDura()
        {
            foreach (var manipulatorData in _demoManipulatorToData)
                CommunicationManager.Instance.GotoPos(manipulatorData.Key.ManipulatorBehaviorController.ManipulatorID,
                    manipulatorData.Value.DuraPos, OUTSIDE_MOVEMENT_SPEED,
                    _ => _manipulatorToStates[manipulatorData.Key] = ManipulatorState.AtDura, Debug.LogError);
        }

        private void Insert()
        {
            foreach (var manipulatorData in _demoManipulatorToData)
                CommunicationManager.Instance.DriveToDepth(
                    manipulatorData.Key.ManipulatorBehaviorController.ManipulatorID,
                    manipulatorData.Value.Depth - CLOSE_TO_TARGET_DISTANCE,
                    INSIDE_MOVEMENT_SPEED,
                    _ => CommunicationManager.Instance.DriveToDepth(
                        manipulatorData.Key.ManipulatorBehaviorController.ManipulatorID,
                        manipulatorData.Value.Depth + GO_PAST_DISTANCE, CLOSE_MOVEMENT_SPEED,
                        _ => CommunicationManager.Instance.DriveToDepth(
                            manipulatorData.Key.ManipulatorBehaviorController.ManipulatorID,
                            manipulatorData.Value.Depth, CLOSE_MOVEMENT_SPEED,
                            _ => _manipulatorToStates[manipulatorData.Key] = ManipulatorState.Inserted, Debug.LogError),
                        Debug.LogError),
                    Debug.LogError);
        }

        private void Retract()
        {
            foreach (var manipulatorData in _demoManipulatorToData)
                CommunicationManager.Instance.DriveToDepth(
                    manipulatorData.Key.ManipulatorBehaviorController.ManipulatorID,
                    manipulatorData.Value.Depth - CLOSE_TO_TARGET_DISTANCE, CLOSE_MOVEMENT_SPEED,
                    _ => CommunicationManager.Instance.DriveToDepth(
                        manipulatorData.Key.ManipulatorBehaviorController.ManipulatorID,
                        manipulatorData.Value.DuraPos.w - EXIT_MARGIN_DEPTH, INSIDE_MOVEMENT_SPEED,
                        _ => CommunicationManager.Instance.GotoPos(
                            manipulatorData.Key.ManipulatorBehaviorController.ManipulatorID,
                            manipulatorData.Value.EntryCoordinatePos, OUTSIDE_MOVEMENT_SPEED,
                            _ => _manipulatorToStates[manipulatorData.Key] = ManipulatorState.Retracted,
                            Debug.LogError),
                        Debug.LogError),
                    Debug.LogError);
        }

        #endregion

        #region Helper functions

        /// <summary>
        ///     Determine if two Vector3 angles are coterminal
        /// </summary>
        /// <param name="first">one Vector3 angle</param>
        /// <param name="second">another Vector3 angle</param>
        /// <returns></returns>
        private static bool IsCoterminal(Vector3 first, Vector3 second)
        {
            return Mathf.Abs(first.x - second.x) % 360 < 0.01f && Mathf.Abs(first.y - second.y) % 360 < 0.01f &&
                   Mathf.Abs(first.z - second.z) % 360 < 0.01f;
        }

        /// <summary>
        ///     Basic spin timer
        /// </summary>
        /// <param name="doAfter">Callback after timer ends</param>
        /// <param name="duration">Timer length in milliseconds</param>
        private static IEnumerator Pause(Action doAfter, long duration = PAUSE_TIME)
        {
            yield return new WaitForSeconds(duration / 1000f);
            doAfter();
        }

        /// <summary>
        ///     Set all manipulator states to Traveling
        /// </summary>
        private void SetAllToTraveling()
        {
            foreach (var manipulatorData in _demoManipulatorToData)
                _manipulatorToStates[manipulatorData.Key] = ManipulatorState.Traveling;
        }

        #endregion
    }
}