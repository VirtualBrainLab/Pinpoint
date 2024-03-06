using System;
using System.Linq;
using BestHTTP.SocketIO3;
using UnityEngine;

namespace EphysLink
{
    /// <summary>
    ///     WebSocket connection manager between the Trajectory Planner and a running Ephys Link server.
    /// </summary>
    public class CommunicationManager : MonoBehaviour
    {
        #region Components

        public static CommunicationManager Instance;
        private SocketManager _connectionManager;
        private Socket _socket;

        #endregion

        #region Properties

        private static readonly int[] EPHYS_LINK_MIN_VERSION = { 1, 2, 8 };

        public static readonly string EPHYS_LINK_MIN_VERSION_STRING = "≥ v" + string.Join(".", EPHYS_LINK_MIN_VERSION);

        private const string UNKOWN_EVENT = "UNKNOWN_EVENT";

        /// <summary>
        ///     The current state of the connection to Ephys Link.
        /// </summary>
        public bool IsConnected { get; private set; }

        public bool IsEphysLinkCompatible { get; set; }

        #endregion


        #region Unity

        private void Awake()
        {
            if (Instance != null) Debug.LogError("Make sure there is only one CommunicationManager in the scene!");
            Instance = this;
        }

        #endregion

        #region Connection Handler

        public void ServerSettingsLoaded()
        {
            // Automatically connect if the server credentials are possible
            if (!IsConnected && Settings.EphysLinkServerIp != "" && Settings.EphysLinkServerPort >= 1025)
                ConnectToServer(Settings.EphysLinkServerIp, Settings.EphysLinkServerPort, () =>
                {
                    // Verify Ephys Link version
                    VerifyVersion(() => IsEphysLinkCompatible = true, () => Instance.DisconnectFromServer());
                });
        }

        /// <summary>
        ///     Create a connection to the server.
        /// </summary>
        /// <param name="ip">IP address of the server</param>
        /// <param name="port">Port of the server</param>
        /// <param name="onConnected">Callback function to handle a successful connection</param>
        /// <param name="onError"></param>
        public void ConnectToServer(string ip, int port, Action onConnected = null,
            Action<string> onError = null)
        {
            // Disconnect the old connection if needed
            if (_connectionManager != null && _connectionManager.Socket.IsOpen) _connectionManager.Close();

            // Create new connection
            var options = new SocketOptions
            {
                Timeout = new TimeSpan(0, 0, 2)
            };


            // Try to open a connection
            try
            {
                // Create a new socket
                _connectionManager = new SocketManager(new Uri("http://" + ip + ":" + port), options);
                _socket = _connectionManager.Socket;

                // On successful connection
                _socket.Once("connect", () =>
                {
                    Debug.Log("Connected to WebSocket server at " + ip + ":" + port);
                    IsConnected = true;

                    // Save settings
                    Settings.EphysLinkServerIp = ip;
                    Settings.EphysLinkServerPort = port;

                    onConnected?.Invoke();
                });

                // On error
                _socket.Once("error", () =>
                {
                    var connectionErrorMessage =
                        "Error connecting to server at " + ip + ":" + port + ". Check server for details.";
                    Debug.LogWarning(connectionErrorMessage);
                    IsConnected = false;
                    _connectionManager.Close();
                    _connectionManager = null;
                    _socket = null;
                    onError?.Invoke(connectionErrorMessage);
                });

                // On timeout
                _socket.Once("connect_timeout", () =>
                {
                    var connectionTimeoutMessage = "Connection to server at " + ip + ":" + port + " timed out";
                    Debug.LogWarning(connectionTimeoutMessage);
                    IsConnected = false;
                    _connectionManager.Close();
                    _connectionManager = null;
                    _socket = null;
                    onError?.Invoke(connectionTimeoutMessage);
                });
            }
            catch (Exception e)
            {
                // On socket generation error
                var connectionErrorMessage =
                    "Error connecting to server at " + ip + ":" + port + ". Check server for details.";
                Debug.LogWarning(connectionErrorMessage);
                Debug.LogWarning("Exception: " + e);
                IsConnected = false;
                _connectionManager = null;
                _socket = null;
                onError?.Invoke(connectionErrorMessage);
            }
        }

        /// <summary>
        ///     Disconnect client from WebSocket server.
        /// </summary>
        /// <param name="onDisconnected">Callback function to handle post disconnection behavior</param>
        public void DisconnectFromServer(Action onDisconnected = null)
        {
            _connectionManager.Close();
            IsConnected = false;
            onDisconnected?.Invoke();
        }

        public void VerifyVersion(Action onSuccess, Action onFailure)
        {
            GetVersion(versionString =>
            {
                var versionNumbers = versionString.Split(".").Select(values =>
                        values.TakeWhile(char.IsDigit).ToArray()).TakeWhile(numbers => numbers.Length > 0)
                    .Select(nonEmpty => int.Parse(new string(nonEmpty))).ToArray();

                // Fail if major version mismatch (breaking changes).
                if (versionNumbers[0] != EPHYS_LINK_MIN_VERSION[0])
                {
                    onFailure.Invoke();
                    return;
                }

                // Fail if minor version is too small (missing features).
                if (versionNumbers[1] < EPHYS_LINK_MIN_VERSION[0])
                {
                    onFailure.Invoke();
                    return;
                }

                // Fail if patch version is too small and minor version is not greater (bug fixes).
                if (versionNumbers[1] == EPHYS_LINK_MIN_VERSION[1] &&
                    versionNumbers[2] < EPHYS_LINK_MIN_VERSION[2])
                {
                    onFailure.Invoke();
                    return;
                }

                // Passed checks.
                onSuccess.Invoke();
            }, onFailure.Invoke);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        ///     Get Ephys Link version.
        /// </summary>
        /// <param name="onSuccessCallback">Returns the version number in the format "x.y.z"</param>
        /// <param name="onErrorCallback">If the version number is empty or failed to return</param>
        private void GetVersion(Action<string> onSuccessCallback, Action onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(data =>
            {
                if (DataKnownAndNotEmpty(data))
                    onSuccessCallback?.Invoke(data);
                else
                    onErrorCallback?.Invoke();
            }).Emit("get_version");
        }

        /// <summary>
        ///     Get manipulators event sender.
        /// </summary>
        /// <param name="onSuccessCallback">Callback function to handle incoming manipulator ID's</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void GetManipulators(Action<string[], int, float[]> onSuccessCallback,
            Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(data =>
            {
                if (DataKnownAndNotEmpty(data))
                {
                    var parsedData = GetManipulatorsCallbackParameters.FromJson(data);
                    if (parsedData.error == "")
                        onSuccessCallback?.Invoke(parsedData.manipulators, parsedData.num_axes,
                            parsedData.dimensions);
                    else
                        onErrorCallback?.Invoke(parsedData.error);
                }
                else
                {
                    onErrorCallback?.Invoke("get_manipulators invalid response: " + data);
                }
            }).Emit("get_manipulators");
        }

        /// <summary>
        ///     Register a manipulator with the server.
        /// </summary>
        /// <param name="manipulatorId">The ID of the manipulator to register</param>
        /// <param name="onSuccessCallback">Callback function to handle a successful registration</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void RegisterManipulator(string manipulatorId, Action onSuccessCallback = null,
            Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(error =>
            {
                if (error == "")
                    onSuccessCallback?.Invoke();
                else
                    onErrorCallback?.Invoke("register_manipulators invalid response: " + error);
            }).Emit("register_manipulator", manipulatorId);
        }

        /// <summary>
        ///     Unregister a manipulator with the server.
        /// </summary>
        /// <param name="manipulatorId">The ID of the manipulator to unregister</param>
        /// <param name="onSuccessCallback">Callback function to handle a successful un-registration</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void UnregisterManipulator(string manipulatorId, Action onSuccessCallback = null,
            Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(error =>
            {
                if (error == "")
                    onSuccessCallback?.Invoke();
                else
                    onErrorCallback?.Invoke("unregister_manipulator invalid response: " + error);
            }).Emit("unregister_manipulator", manipulatorId);
        }

        /// <summary>
        ///     Request the current position of a manipulator.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to get the position of</param>
        /// <param name="onSuccessCallback">Callback function to pass manipulator position to</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void GetPos(string manipulatorId, Action<Vector4> onSuccessCallback,
            Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(data =>
            {
                if (DataKnownAndNotEmpty(data))
                {
                    var parsedData = PositionalCallbackParameters.FromJson(data);
                    if (parsedData.error == "")
                        try
                        {
                            onSuccessCallback?.Invoke(new Vector4(parsedData.position[0], parsedData.position[1],
                                parsedData.position[2], parsedData.position[3]));
                        }
                        catch (Exception e)
                        {
                            onErrorCallback?.Invoke(e.ToString());
                        }
                    else
                        onErrorCallback?.Invoke(parsedData.error);
                }
                else
                {
                    onErrorCallback?.Invoke("get_pos invalid response: " + data);
                }
            }).Emit("get_pos", manipulatorId);
        }

        /// <summary>
        ///     Request the current angles of a manipulator.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to get the position of</param>
        /// <param name="onSuccessCallback">Callback function to pass manipulator angles to</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void GetAngles(string manipulatorId, Action<Vector3> onSuccessCallback,
            Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(data =>
            {
                if (DataKnownAndNotEmpty(data))
                {
                    var parsedData = AngularCallbackParameters.FromJson(data);
                    if (parsedData.error == "")
                        try
                        {
                            onSuccessCallback?.Invoke(new Vector3(parsedData.angles[0], parsedData.angles[1],
                                parsedData.angles[2]));
                        }
                        catch (Exception e)
                        {
                            onErrorCallback?.Invoke(e.ToString());
                        }
                    else
                        onErrorCallback?.Invoke(parsedData.error);
                }
                else
                {
                    onErrorCallback?.Invoke("get_angles invalid response: " + data);
                }
            }).Emit("get_angles", manipulatorId);
        }

        public void GetShankCount(string manipulatorId, Action<int> onSuccessCallback,
            Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(data =>
            {
                if (DataKnownAndNotEmpty(data))
                {
                    var parsedData = ShankCountCallbackParameters.FromJson(data);
                    if (parsedData.error == "")
                        try
                        {
                            onSuccessCallback?.Invoke(parsedData.shank_count);
                        }
                        catch (Exception e)
                        {
                            onErrorCallback?.Invoke(e.ToString());
                        }
                    else
                        onErrorCallback?.Invoke(parsedData.error);
                }
                else
                {
                    onErrorCallback?.Invoke("get_shank_count invalid response: " + data);
                }
            }).Emit("get_shank_count", manipulatorId);
        }

        /// <summary>
        ///     Request a manipulator be moved to a specific position.
        /// </summary>
        /// <remarks>Position is defined by a Vector4</remarks>
        /// <param name="manipulatorId">ID of the manipulator to be moved</param>
        /// <param name="pos">Position in mm of the manipulator</param>
        /// <param name="speed">How fast to move the manipulator (in mm/s)</param>
        /// <param name="onSuccessCallback">Callback function to handle successful manipulator movement</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void GotoPos(string manipulatorId, Vector4 pos, float speed, Action<Vector4> onSuccessCallback,
            Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(data =>
            {
                if (DataKnownAndNotEmpty(data))
                {
                    var parsedData = PositionalCallbackParameters.FromJson(data);
                    if (parsedData.error == "")
                        try
                        {
                            onSuccessCallback?.Invoke(new Vector4(parsedData.position[0], parsedData.position[1],
                                parsedData.position[2], parsedData.position[3]));
                        }
                        catch (Exception e)
                        {
                            onErrorCallback?.Invoke(e.ToString());
                        }
                    else
                        onErrorCallback?.Invoke(parsedData.error);
                }
                else
                {
                    onErrorCallback?.Invoke("goto_pos invalid response: " + data);
                }
            }).Emit("goto_pos", new GotoPositionInputDataFormat(manipulatorId, pos, speed).ToJson());
        }

        /// <summary>
        ///     Request a manipulator drive down to a specific depth.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to move</param>
        /// <param name="depth">Depth in mm of the manipulator (in needle coordinates)</param>
        /// <param name="speed">How fast to drive the manipulator (in mm/s)</param>
        /// <param name="onSuccessCallback">Callback function to handle successful manipulator movement</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void DriveToDepth(string manipulatorId, float depth, float speed, Action<float> onSuccessCallback,
            Action<string> onErrorCallback)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(data =>
            {
                if (DataKnownAndNotEmpty(data))
                {
                    var parsedData = DriveToDepthCallbackParameters.FromJson(data);
                    if (parsedData.error == "")
                        try
                        {
                            onSuccessCallback?.Invoke(parsedData.depth);
                        }
                        catch (Exception e)
                        {
                            onErrorCallback?.Invoke(e.ToString());
                        }
                    else
                        onErrorCallback?.Invoke(parsedData.error);
                }
                else
                {
                    onErrorCallback?.Invoke("drive_to_depth invalid response: " + data);
                }
            }).Emit("drive_to_depth", new DriveToDepthInputDataFormat(manipulatorId, depth, speed).ToJson());
        }

        /// <summary>
        ///     Set the inside brain state of a manipulator.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to set the state of</param>
        /// <param name="inside">State to set to</param>
        /// <param name="onSuccessCallback">Callback function to handle setting inside_brain state successfully</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void SetInsideBrain(string manipulatorId, bool inside, Action<bool> onSuccessCallback,
            Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(data =>
            {
                if (DataKnownAndNotEmpty(data))
                {
                    var parsedData = StateCallbackParameters.FromJson(data);
                    if (parsedData.error == "")
                        onSuccessCallback?.Invoke(parsedData.state);
                    else
                        onErrorCallback?.Invoke(parsedData.error);
                }
                else
                {
                    onErrorCallback?.Invoke("set_inside_brain invalid response: " + data);
                }
            }).Emit("set_inside_brain", new InsideBrainInputDataFormat(manipulatorId, inside).ToJson());
        }

        /// <summary>
        ///     Request a manipulator to be calibrated.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to be calibrated</param>
        /// <param name="onSuccessCallback">Callback function to handle a successful calibration</param>
        /// <param name="onErrorCallback">Callback function to handle an unsuccessful calibration</param>
        public void Calibrate(string manipulatorId, Action onSuccessCallback, Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(error =>
            {
                if (error == "")
                    onSuccessCallback?.Invoke();
                else
                    onErrorCallback?.Invoke(error);
            }).Emit("calibrate", manipulatorId);
        }

        /// <summary>
        ///     Bypass calibration requirement of a manipulator.
        /// </summary>
        /// <remarks>This method should only be used for testing and NEVER in production</remarks>
        /// <param name="manipulatorId">ID of the manipulator to bypass calibration</param>
        /// <param name="onSuccessCallback">Callback function to handle a successful calibration bypass</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void BypassCalibration(string manipulatorId, Action onSuccessCallback,
            Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(error =>
            {
                if (error == "")
                    onSuccessCallback?.Invoke();
                else
                    onErrorCallback?.Invoke(error);
            }).Emit("bypass_calibration", manipulatorId);
        }

        /// <summary>
        ///     Request a write lease for a manipulator.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to allow writing</param>
        /// <param name="canWrite">Write state to set the manipulator to</param>
        /// <param name="hours">How many hours a manipulator may have a write lease</param>
        /// <param name="onSuccessCallback">Callback function to handle successfully setting can_write state</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void SetCanWrite(string manipulatorId, bool canWrite, float hours, Action<bool> onSuccessCallback,
            Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(data =>
            {
                if (DataKnownAndNotEmpty(data))
                {
                    var parsedData = StateCallbackParameters.FromJson(data);
                    if (parsedData.error == "")
                        onSuccessCallback?.Invoke(parsedData.state);
                    else
                        onErrorCallback?.Invoke(parsedData.error);
                }
                else
                {
                    onErrorCallback?.Invoke("set_can_write invalid response: " + data);
                }
            }).Emit("set_can_write", new CanWriteInputDataFormat(manipulatorId, canWrite, hours).ToJson());
        }

        /// <summary>
        ///     Request all movement to stop.
        /// </summary>
        /// <param name="callback">Callback function to handle stop result</param>
        public void Stop(Action<bool> callback)
        {
            _connectionManager.Socket.ExpectAcknowledgement(callback).Emit("stop");
        }

        #endregion

        #region Helper functions

        private static bool DataKnownAndNotEmpty(string data)
        {
            return data is not ("" or UNKOWN_EVENT);
        }

        #endregion
    }
}