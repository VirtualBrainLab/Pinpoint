using System;
using BestHTTP.SocketIO3;
using UnityEngine;

namespace EphysLink
{
    /// <summary>
    ///     WebSocket connection manager between the Trajectory Planner and a running Ephys Link server.
    /// </summary>
    public class CommunicationManager : MonoBehaviour
    {
        #region Unity

        /// <summary>
        ///     Attach to events for automatic connection.
        /// </summary>
        private void Awake()
        {
            Settings.EphysLinkServerSettingsLoadedEvent.AddListener(() =>
            {
                // Automatically connect if the server credentials are possible
                if (Settings.EphysLinkServerIp != "" && Settings.EphysLinkServerPort >= 1025)
                    ConnectToServer(Settings.EphysLinkServerIp, Settings.EphysLinkServerPort);
            });
        }

        #endregion

        #region Variables

        #region Components

        private SocketManager _connectionManager;
        private Socket _socket;

        #endregion

        #region Properties

        /// <summary>
        ///     The current state of the connection to Ephys Link.
        /// </summary>
        public bool IsConnected { get; private set; }

        #endregion

        #endregion

        #region Connection Handler

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

        #endregion

        #region Event Handlers

        /// <summary>
        ///     Get manipulators event sender.
        /// </summary>
        /// <param name="onSuccessCallback">Callback function to handle incoming manipulator ID's</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void GetManipulators(Action<string[]> onSuccessCallback, Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<GetManipulatorsCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    onSuccessCallback(data.manipulators);
                }
                else
                {
                    onErrorCallback?.Invoke(data.error);
                    Debug.LogError(data.error);
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
                {
                    onSuccessCallback?.Invoke();
                }
                else
                {
                    onErrorCallback?.Invoke(error);
                    Debug.LogWarning(error);
                }
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
                {
                    onSuccessCallback?.Invoke();
                }
                else
                {
                    onErrorCallback?.Invoke(error);
                    Debug.LogWarning(error);
                }
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
            _connectionManager.Socket.ExpectAcknowledgement<PositionalCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    onSuccessCallback(new Vector4(data.position[0], data.position[1], data.position[2],
                        data.position[3]));
                }
                else
                {
                    onErrorCallback?.Invoke(data.error);
                    Debug.LogWarning(data.error);
                }
            }).Emit("get_pos", manipulatorId);
        }

        /// <summary>
        ///     Request a manipulator be moved to a specific position.
        /// </summary>
        /// <remarks>Position is defined by a Vector4</remarks>
        /// <param name="manipulatorId">ID of the manipulator to be moved</param>
        /// <param name="pos">Position in μm of the manipulator (in needle coordinates)</param>
        /// <param name="speed">How fast to move the manipulator (in μm/s)</param>
        /// <param name="onSuccessCallback">Callback function to handle successful manipulator movement</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void GotoPos(string manipulatorId, Vector4 pos, int speed, Action<Vector4> onSuccessCallback,
            Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<PositionalCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    onSuccessCallback(new Vector4(data.position[0], data.position[1], data.position[2],
                        data.position[3]));
                }
                else
                {
                    onErrorCallback?.Invoke(data.error);
                    Debug.LogWarning(data.error);
                }
            }).Emit("goto_pos", new GotoPositionInputDataFormat(manipulatorId, pos, speed));
        }

        /// <summary>
        ///     Request a manipulator be moved to a specific position defined by an array of 4 floats.
        /// </summary>
        /// <remarks>Position is defined by an array of 4 floats</remarks>
        /// <param name="manipulatorId">ID of the manipulator to be moved</param>
        /// <param name="pos">Position in μm of the manipulator (in needle coordinates)</param>
        /// <param name="speed">How fast to move the manipulator (in μm/s)</param>
        /// <param name="onSuccessCallback">Callback function to handle successful manipulator movement</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        /// <exception cref="ArgumentException">If the given position is not in an array of 4 floats</exception>
        public void GotoPos(string manipulatorId, float[] pos, int speed, Action<Vector4> onSuccessCallback,
            Action<string> onErrorCallback = null)
        {
            if (pos.Length != 4) throw new ArgumentException("Position array must be of length 4");

            GotoPos(manipulatorId, new Vector4(pos[0], pos[1], pos[2], pos[3]), speed, onSuccessCallback,
                onErrorCallback);
        }

        /// <summary>
        ///     Request a manipulator drive down to a specific depth.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to move</param>
        /// <param name="depth">Depth in μm of the manipulator (in needle coordinates)</param>
        /// <param name="speed">How fast to drive the manipulator (in μm/s)</param>
        /// <param name="onSuccessCallback">Callback function to handle successful manipulator movement</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void DriveToDepth(string manipulatorId, float depth, int speed, Action<float> onSuccessCallback,
            Action<string> onErrorCallback)
        {
            _connectionManager.Socket.ExpectAcknowledgement<DriveToDepthCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    onSuccessCallback(data.depth);
                }
                else
                {
                    onErrorCallback?.Invoke(data.error);
                    Debug.LogWarning(data.error);
                }
            }).Emit("drive_to_depth", new DriveToDepthInputDataFormat(manipulatorId, depth, speed));
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
            _connectionManager.Socket.ExpectAcknowledgement<StateCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    onSuccessCallback(data.state);
                }
                else
                {
                    onErrorCallback?.Invoke(data.error);
                    Debug.LogWarning(data.error);
                }
            }).Emit("set_inside_brain", new InsideBrainInputDataFormat(manipulatorId, inside));
        }

        /// <summary>
        ///     Request a manipulator to be calibrated.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to be calibrated</param>
        /// <param name="onSuccessCallback">Callback function to handle a successful calibration</param>
        /// <param name="onErrorCallback">Callback function to handle an unsuccessful calibration</param>
        public void Calibrate(string manipulatorId, Action onSuccessCallback, Action<string> onErrorCallback = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<string>(errorMessage =>
            {
                if (errorMessage == "")
                {
                    onSuccessCallback();
                }
                else
                {
                    onErrorCallback?.Invoke(errorMessage);
                    Debug.LogWarning(errorMessage);
                }
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
                {
                    onSuccessCallback();
                }
                else
                {
                    onErrorCallback?.Invoke(error);
                    Debug.LogWarning(error);
                }
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
            _connectionManager.Socket.ExpectAcknowledgement<StateCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    onSuccessCallback(data.state);
                }
                else
                {
                    onErrorCallback?.Invoke(data.error);
                    Debug.LogWarning(data.error);
                }
            }).Emit("set_can_write", new CanWriteInputDataFormat(manipulatorId, canWrite, hours));
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
    }
}