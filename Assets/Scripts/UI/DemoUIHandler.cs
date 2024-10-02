using System;
using System.IO;
using System.Linq;
using EphysLink;
using Pinpoint.Probes.ManipulatorBehaviorController;
using Pinpoint.UI.EphysLinkSettings;
using UI.States;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class DemoUIHandler : MonoBehaviour
    {
        #region Components

        [SerializeField]
        private DemoUIState _state;

        [SerializeField]
        private UIDocument _uiDocument;

        [SerializeField]
        private EphysLinkSettings _ephysLinkSettings;

        private VisualElement _root => _uiDocument.rootVisualElement;

        [SerializeField]
        private BrainCameraController _brainCameraController;

        private Button _exitButton;

        #endregion

        #region Properties

        private struct DemoData
        {
            public string Id1;
            public Vector3 Home1;
            public Vector4 Target1;

            public string Id2;
            public Vector3 Home2;
            public Vector4 Target2;

            public string Id3;
            public Vector3 Home3;
            public Vector4 Target3;
        }

        private ManipulatorBehaviorController _manipulator1;
        private ManipulatorBehaviorController _manipulator2;
        private ManipulatorBehaviorController _manipulator3;

        private DemoData _demoData;

        private bool _isDemoRunning;

        private const float MOVEMENT_SPEED = ManipulatorBehaviorController.AUTOMATIC_MOVEMENT_SPEED;
        #endregion

        #region Unity

        private void OnEnable()
        {
            // Set Camera.
            _brainCameraController.SetZoom(10);
            _brainCameraController.transform.rotation = Quaternion.Euler(180, -180, -180);

            // Set button.
            _exitButton = _root.Q<Button>("exit-button");
            _exitButton.clicked += OnExitDemoPressed;

            // Get manipulators.
            foreach (
                var probeManager in ProbeManager.Instances.Where(probeManager =>
                    probeManager.IsEphysLinkControlled
                )
            )
            {
                if (_manipulator1 == null)
                {
                    _manipulator1 = probeManager.ManipulatorBehaviorController;
                }
                else if (_manipulator2 == null)
                {
                    _manipulator2 = probeManager.ManipulatorBehaviorController;
                }
                else if (_manipulator3 == null)
                {
                    _manipulator3 = probeManager.ManipulatorBehaviorController;
                }
            }

            // Parse demo data.
            _demoData = JsonUtility.FromJson<DemoData>(
                File.ReadAllText(Application.streamingAssetsPath + "/DemoData.json")
            );

            // Start demo.
            _isDemoRunning = true;
            RunDemo();
        }

        private void Update()
        {
            _brainCameraController.transform.Rotate(0, 5 * Time.deltaTime, 0);
        }

        private void OnDisable()
        {
            _exitButton.clicked -= OnExitDemoPressed;
        }

        #endregion

        #region Functions

        private async void RunDemo()
        {
            while (_isDemoRunning)
            {
                // Home.
                _state.Stage = DemoStage.Home;

                var home1 = CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator1.ManipulatorID,
                        _manipulator1.ConvertInsertionAPMLDVToManipulatorPosition(_demoData.Home1),
                        MOVEMENT_SPEED
                    )
                );
                var home2 = CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator2.ManipulatorID,
                        _manipulator2.ConvertInsertionAPMLDVToManipulatorPosition(_demoData.Home2),
                        MOVEMENT_SPEED
                    )
                );
                var home3 = CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator3.ManipulatorID,
                        _manipulator3.ConvertInsertionAPMLDVToManipulatorPosition(_demoData.Home3),
                        MOVEMENT_SPEED
                    )
                );

                if (CommunicationManager.HasError((await home1).Error))
                {
                    break;
                }
                if (CommunicationManager.HasError((await home2).Error))
                {
                    break;
                }
                if (CommunicationManager.HasError((await home3).Error))
                {
                    break;
                }

                // Reference coordinate.
                _state.Stage = DemoStage.ReferenceCoordinate;

                if (!await _manipulator1.MoveBackToReferenceCoordinate())
                    break;
                var homePos1 = await CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator1.ManipulatorID,
                        _manipulator1.ConvertInsertionAPMLDVToManipulatorPosition(_demoData.Home1),
                        MOVEMENT_SPEED
                    )
                );
                if (CommunicationManager.HasError(homePos1.Error))
                {
                    break;
                }

                if (!await _manipulator2.MoveBackToReferenceCoordinate())
                    break;
                var homePos2 = await CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator2.ManipulatorID,
                        _manipulator2.ConvertInsertionAPMLDVToManipulatorPosition(_demoData.Home2),
                        MOVEMENT_SPEED
                    )
                );
                if (CommunicationManager.HasError(homePos2.Error))
                {
                    break;
                }

                if (!await _manipulator3.MoveBackToReferenceCoordinate())
                    break;
                var homePos3 = await CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator3.ManipulatorID,
                        _manipulator3.ConvertInsertionAPMLDVToManipulatorPosition(_demoData.Home3),
                        MOVEMENT_SPEED
                    )
                );
                if (CommunicationManager.HasError(homePos3.Error))
                {
                    break;
                }

                // Entry.
                _state.Stage = DemoStage.Entry;

                var entry1 = CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator1.ManipulatorID,
                        _manipulator1.ConvertInsertionAPMLDVToManipulatorPosition(
                            new Vector3(_demoData.Target1.x, _demoData.Target1.y, 3)
                        ),
                        MOVEMENT_SPEED
                    )
                );
                var entry2 = CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator2.ManipulatorID,
                        _manipulator2.ConvertInsertionAPMLDVToManipulatorPosition(
                            new Vector3(_demoData.Target2.x, _demoData.Target2.y, 3)
                        ),
                        MOVEMENT_SPEED
                    )
                );
                var entry3 = CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator3.ManipulatorID,
                        _manipulator3.ConvertInsertionAPMLDVToManipulatorPosition(
                            new Vector3(_demoData.Target3.x, _demoData.Target3.y, 3)
                        ),
                        MOVEMENT_SPEED
                    )
                );

                if (CommunicationManager.HasError((await entry1).Error))
                {
                    break;
                }
                if (CommunicationManager.HasError((await entry2).Error))
                {
                    break;
                }
                if (CommunicationManager.HasError((await entry3).Error))
                {
                    break;
                }

                // Dura.
                _state.Stage = DemoStage.Dura;

                var dura1 = CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator1.ManipulatorID,
                        _manipulator1.ConvertInsertionAPMLDVToManipulatorPosition(
                            _demoData.Target1
                        ),
                        MOVEMENT_SPEED
                    )
                );
                var dura2 = CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator2.ManipulatorID,
                        _manipulator2.ConvertInsertionAPMLDVToManipulatorPosition(
                            _demoData.Target2
                        ),
                        MOVEMENT_SPEED
                    )
                );
                var dura3 = CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator3.ManipulatorID,
                        _manipulator3.ConvertInsertionAPMLDVToManipulatorPosition(
                            _demoData.Target3
                        ),
                        MOVEMENT_SPEED
                    )
                );

                var dura1Position = await dura1;
                if (CommunicationManager.HasError(dura1Position.Error))
                {
                    break;
                }
                var dura2Position = await dura2;
                if (CommunicationManager.HasError(dura2Position.Error))
                {
                    break;
                }
                var dura3Position = await dura3;
                if (CommunicationManager.HasError(dura3Position.Error))
                {
                    break;
                }

                // Target.
                _state.Stage = DemoStage.Target;

                var target1 = CommunicationManager.Instance.SetDepth(
                    new SetDepthRequest(
                        _manipulator1.ManipulatorID,
                        dura1Position.Position.w + _demoData.Target1.w,
                        MOVEMENT_SPEED / 2
                    )
                );
                var target2 = CommunicationManager.Instance.SetDepth(
                    new SetDepthRequest(
                        _manipulator2.ManipulatorID,
                        dura2Position.Position.w + _demoData.Target2.w,
                        MOVEMENT_SPEED / 2
                    )
                );
                var target3 = CommunicationManager.Instance.SetDepth(
                    new SetDepthRequest(
                        _manipulator3.ManipulatorID,
                        dura3Position.Position.w + _demoData.Target3.w,
                        MOVEMENT_SPEED / 2
                    )
                );

                if (CommunicationManager.HasError((await target1).Error))
                {
                    break;
                }
                if (CommunicationManager.HasError((await target2).Error))
                {
                    break;
                }
                if (CommunicationManager.HasError((await target3).Error))
                {
                    break;
                }

                // Return to surface.
                dura1 = CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator1.ManipulatorID,
                        _manipulator1.ConvertInsertionAPMLDVToManipulatorPosition(
                            _demoData.Target1
                        ),
                        MOVEMENT_SPEED
                    )
                );
                dura2 = CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator2.ManipulatorID,
                        _manipulator2.ConvertInsertionAPMLDVToManipulatorPosition(
                            _demoData.Target2
                        ),
                        MOVEMENT_SPEED
                    )
                );
                dura3 = CommunicationManager.Instance.SetPosition(
                    new SetPositionRequest(
                        _manipulator3.ManipulatorID,
                        _manipulator3.ConvertInsertionAPMLDVToManipulatorPosition(
                            _demoData.Target3
                        ),
                        MOVEMENT_SPEED
                    )
                );

                if (CommunicationManager.HasError((await dura1).Error))
                {
                    break;
                }
                if (CommunicationManager.HasError((await dura2).Error))
                {
                    break;
                }
                if (CommunicationManager.HasError((await dura3).Error))
                {
                    break;
                }
            }
        }

        private async void OnExitDemoPressed()
        {
            // Reset camera position.
            _brainCameraController.SetZoom(5);

            // Reset manipulator references.
            _manipulator1 = null;
            _manipulator2 = null;
            _manipulator3 = null;

            // Stop Demo.
            _isDemoRunning = false;

            // Reset UI state.
            _state.Stage = DemoStage.Home;

            // Stop manipulators.
            await CommunicationManager.Instance.StopAll();

            // Close the UI.
            _ephysLinkSettings.StopAutomationDemo();
        }

        #endregion
    }
}
