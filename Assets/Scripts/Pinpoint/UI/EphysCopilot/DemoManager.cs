using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EphysLink;
using Pinpoint.Probes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pinpoint.UI.EphysCopilot
{
    public class DemoManager : MonoBehaviour
    {
        #region Constants

        private const float MOVEMENT_SPEED = 1f;

        #endregion
        #region Components

        // Existing UI for toggling.

        [SerializeField]
        private GameObject _canvasGameObject;

        private readonly HashSet<GameObject> _existingUIGameObjects = new();

        // Demo UI.


        // Camera.

        [SerializeField]
        private BrainCameraController _mainCamera;

        // Manipulators.
        private readonly Dictionary<string, ManipulatorBehaviorController> _manipulators = new();
        private readonly Dictionary<string, ProbeManager> _probeManagers = new();

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

        private DemoData _demoData;

        private readonly Dictionary<string, Vector4> _convertedHomePositions = new();
        private readonly Dictionary<string, Vector4> _convertedBregmaCoordinates = new();
        private readonly Dictionary<string, Vector4> _convertedInsertionCoordinates = new();
        private Dictionary<string, Vector4> _convertedDuraCoordinates = new();
        private readonly Dictionary<string, Vector4> _convertedTargetCoordinates = new();

        #endregion


        #region Unity

        private void Update()
        {
            _mainCamera.transform.Rotate(0, 5 * Time.deltaTime, 0);
        }

        #endregion

        #region UI Functions

        public void StartDemo()
        {
            // Hide existing UI and show demo UI.
            for (var i = 0; i < _canvasGameObject.transform.childCount; i++)
            {
                var child = _canvasGameObject.transform.GetChild(i).gameObject;

                // Ignore the demo UI.
                if (child.name == "CopilotDemo")
                    continue;
                // Ignore inactive UI.
                if (!child.activeSelf)
                {
                    continue;
                }

                // Hide the UI.
                child.SetActive(false);

                // Add to the list of existing UI.
                _existingUIGameObjects.Add(child);
            }
            gameObject.SetActive(true);

            // Setup camera.
            _mainCamera.SetZoom(10);
            _mainCamera.transform.rotation = Quaternion.Euler(180, -180, -180);

            // Read demo data.
            _demoData = JsonUtility.FromJson<DemoData>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "DemoData.json"))
            );

            // Get manipulators.
            foreach (
                var probeManager in ProbeManager.Instances.Where(probeManager =>
                    probeManager.IsEphysLinkControlled
                )
            )
            {
                _manipulators.Add(
                    probeManager.ManipulatorBehaviorController.ManipulatorID,
                    probeManager.ManipulatorBehaviorController
                );
                _probeManagers.Add(
                    probeManager.ManipulatorBehaviorController.ManipulatorID,
                    probeManager
                );
            }

            // Compute Manipulator 1 positions.
            if (_manipulators.ContainsKey(_demoData.Id1))
            {
                _convertedHomePositions.Add(
                    _demoData.Id1,
                    _manipulators[_demoData.Id1]
                        .ConvertInsertionAPMLDVToManipulatorPosition(_demoData.Home1)
                );
                _convertedBregmaCoordinates.Add(
                    _demoData.Id1,
                    _manipulators[_demoData.Id1]
                        .ConvertInsertionAPMLDVToManipulatorPosition(Vector3.zero)
                );
                _convertedInsertionCoordinates.Add(
                    _demoData.Id1,
                    _manipulators[_demoData.Id1]
                        .ConvertInsertionAPMLDVToManipulatorPosition(
                            new Vector3(_demoData.Target1.x, _demoData.Target1.y, 3)
                        )
                );
                _convertedDuraCoordinates.Add(
                    _demoData.Id1,
                    _manipulators[_demoData.Id1]
                        .ConvertInsertionAPMLDVToManipulatorPosition(
                            new Vector3(
                                _demoData.Target1.x,
                                _demoData.Target1.y,
                                _demoData.Target1.z
                            )
                        )
                );
            }

            // Compute Manipulator 2 positions.
            if (_manipulators.ContainsKey(_demoData.Id2))
            {
                _convertedHomePositions.Add(
                    _demoData.Id2,
                    _manipulators[_demoData.Id2]
                        .ConvertInsertionAPMLDVToManipulatorPosition(_demoData.Home2)
                );
                _convertedBregmaCoordinates.Add(
                    _demoData.Id2,
                    _manipulators[_demoData.Id2]
                        .ConvertInsertionAPMLDVToManipulatorPosition(Vector3.zero)
                );
                _convertedInsertionCoordinates.Add(
                    _demoData.Id2,
                    _manipulators[_demoData.Id2]
                        .ConvertInsertionAPMLDVToManipulatorPosition(
                            new Vector3(_demoData.Target2.x, _demoData.Target2.y, 3)
                        )
                );
                _convertedDuraCoordinates.Add(
                    _demoData.Id2,
                    _manipulators[_demoData.Id2]
                        .ConvertInsertionAPMLDVToManipulatorPosition(
                            new Vector3(
                                _demoData.Target2.x,
                                _demoData.Target2.y,
                                _demoData.Target2.z
                            )
                        )
                );
            }

            // Compute Manipulator 3 positions.
            if (_manipulators.ContainsKey(_demoData.Id3))
            {
                _convertedHomePositions.Add(
                    _demoData.Id3,
                    _manipulators[_demoData.Id3]
                        .ConvertInsertionAPMLDVToManipulatorPosition(_demoData.Home3)
                );
                _convertedBregmaCoordinates.Add(
                    _demoData.Id3,
                    _manipulators[_demoData.Id3]
                        .ConvertInsertionAPMLDVToManipulatorPosition(Vector3.zero)
                );
                _convertedInsertionCoordinates.Add(
                    _demoData.Id3,
                    _manipulators[_demoData.Id3]
                        .ConvertInsertionAPMLDVToManipulatorPosition(
                            new Vector3(_demoData.Target3.x, _demoData.Target3.y, 3)
                        )
                );
                _convertedDuraCoordinates.Add(
                    _demoData.Id3,
                    _manipulators[_demoData.Id3]
                        .ConvertInsertionAPMLDVToManipulatorPosition(
                            new Vector3(
                                _demoData.Target3.x,
                                _demoData.Target3.y,
                                _demoData.Target3.z
                            )
                        )
                );
            }

            // Start demo.
            MoveToHome();
        }

        public void StopDemo()
        {
            // Show existing UI and hide demo UI.
            foreach (var existingUIGameObject in _existingUIGameObjects)
            {
                existingUIGameObject.SetActive(true);
            }
            gameObject.SetActive(false);
            _existingUIGameObjects.Clear();

            // Reset camera.
            _mainCamera.SetZoom(5);
        }

        #endregion

        #region Demo Functions

        public void MoveToHome()
        {
            CommunicationManager.Instance.SetCanWrite(
                new CanWriteRequest(_demoData.Id1, true, 1),
                _ =>
                {
                    CommunicationManager.Instance.GotoPos(
                        new GotoPositionRequest(
                            _demoData.Id1,
                            _convertedHomePositions[_demoData.Id1],
                            MOVEMENT_SPEED
                        ),
                        _ =>
                        {
                            CommunicationManager.Instance.GotoPos(
                                new GotoPositionRequest(
                                    _demoData.Id1,
                                    _convertedBregmaCoordinates[_demoData.Id1],
                                    MOVEMENT_SPEED
                                ),
                                _ =>
                                {
                                    CommunicationManager.Instance.GotoPos(
                                        new GotoPositionRequest(
                                            _demoData.Id1,
                                            _convertedInsertionCoordinates[_demoData.Id1],
                                            MOVEMENT_SPEED
                                        ),
                                        _ =>
                                        {
                                            CommunicationManager.Instance.GotoPos(
                                                new GotoPositionRequest(
                                                    _demoData.Id1,
                                                    _convertedDuraCoordinates[_demoData.Id1],
                                                    MOVEMENT_SPEED
                                                ),
                                                pos =>
                                                {
                                                    CommunicationManager.Instance.DriveToDepth(
                                                        new DriveToDepthRequest(
                                                            _demoData.Id1,
                                                            pos.w + _demoData.Target1.w,
                                                            MOVEMENT_SPEED
                                                        ),
                                                        null,
                                                        null
                                                    );
                                                }
                                            );
                                        }
                                    );
                                }
                            );
                        }
                    );
                }
            );
        }

        #endregion
    }
}
