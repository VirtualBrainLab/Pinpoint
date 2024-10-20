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

        private static readonly int[] EPHYS_LINK_MIN_VERSION = { 2, 0, 0 };

        public static readonly string EPHYS_LINK_MIN_VERSION_STRING =
            $"≥ v{string.Join(".", EPHYS_LINK_MIN_VERSION)}";

        private const string UNKOWN_EVENT = "{\"error\": \"Unknown event.\"}";

        /// <summary>
        ///     The current state of the connection to Ephys Link.
        /// </summary>
        public bool IsConnected { get; private set; }

        public bool IsEphysLinkCompatible { get; set; }

        #endregion


        #region Unity

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else if (Instance != this)
            {
                Destroy(this);
            }
        }

        #endregion

        #region Connection Handler

        public void ServerSettingsLoaded()
        {
            // Automatically connect if the server credentials are possible
            if (
                !IsConnected
                && !string.IsNullOrEmpty(Settings.EphysLinkServerIp)
                && Settings.EphysLinkServerPort >= 1025
            )
                ConnectToServer(
                    Settings.EphysLinkServerIp,
                    Settings.EphysLinkServerPort,
                    () =>
                    {
                        // Verify Ephys Link version
                        VerifyVersion(
                            () => IsEphysLinkCompatible = true,
                            () => Instance.DisconnectFromServer()
                        );
                    }
                );
        }

        /// <summary>
        ///     Create a connection to the server.
        /// </summary>
        /// <param name="ip">IP address of the server</param>
        /// <param name="port">Port of the server</param>
        /// <param name="onConnected">Callback function to handle a successful connection</param>
        /// <param name="onError"></param>
        public void ConnectToServer(
            string ip,
            int port,
            Action onConnected = null,
            Action<string> onError = null
        )
        {
            // Disconnect the old connection if needed
            if (_connectionManager != null && _connectionManager.Socket.IsOpen)
                _connectionManager.Close();

            // Create new connection
            var options = new SocketOptions { Timeout = new TimeSpan(0, 0, 2) };

            // Try to open a connection
            try
            {
                // Create a new socket
                _connectionManager = new SocketManager(new Uri($"http://{ip}:{port}"), options);
                _socket = _connectionManager.Socket;

                // On successful connection
                _socket.Once(
                    "connect",
                    () =>
                    {
                        Debug.Log($"Connected to Ephys Link server at {ip}:{port}");
                        IsConnected = true;

                        // Save settings
                        Settings.EphysLinkServerIp = ip;
                        Settings.EphysLinkServerPort = port;

                        // Invoke connected callback
                        onConnected?.Invoke();
                    }
                );

                // On error
                _socket.Once(
                    "error",
                    () =>
                    {
                        var connectionErrorMessage =
                            $"Error connecting to server at {ip}:{port}. Check server for details.";
                        Debug.LogWarning(connectionErrorMessage);
                        IsConnected = false;
                        _connectionManager.Close();
                        _connectionManager = null;
                        _socket = null;
                        onError?.Invoke(connectionErrorMessage);
                    }
                );

                // On timeout
                _socket.Once(
                    "connect_timeout",
                    () =>
                    {
                        var connectionTimeoutMessage =
                            $"Connection to server at {ip}:{port} timed out";
                        Debug.LogWarning(connectionTimeoutMessage);
                        IsConnected = false;
                        _connectionManager.Close();
                        _connectionManager = null;
                        _socket = null;
                        onError?.Invoke(connectionTimeoutMessage);
                    }
                );
            }
            catch (Exception e)
            {
                // On socket generation error
                var connectionErrorMessage =
                    "Error connecting to server at {ip}:{port}. Check server for details.";
                Debug.LogWarning(connectionErrorMessage);
                Debug.LogWarning("Exception: " + e);
                IsConnected = false;
                _connectionManager = null;
                _socket = null;
                onError?.Invoke(connectionErrorMessage);
            }
        }

        public void ConnectToProxy(
            string proxyAddress,
            string pinpointID,
            Action onConnected = null,
            Action<string> onError = null
        )
        {
            // Disconnect the old connection if needed
            if (_connectionManager != null && _connectionManager.Socket.IsOpen)
                _connectionManager.Close();

            // Create new connection
            var options = new SocketOptions { Timeout = new TimeSpan(0, 0, 2) };

            // Try to open a connection
            try
            {
                // Create a new socket
                _connectionManager = new SocketManager(
                    new Uri($"http://{proxyAddress}:3000"),
                    options
                );
                _socket = _connectionManager.Socket;

                // On successful connection
                _socket.Once(
                    "connect",
                    () =>
                    {
                        Debug.Log($"Connected to proxy server at {proxyAddress}:3000");
                        IsConnected = true;

                        // Save settings and clear Server IP (don't allow auto reconnect)
                        Settings.EphysLinkServerIp = "";
                        Settings.EphysLinkProxyAddress = proxyAddress;
                    }
                );

                _socket.Once(
                    "get_pinpoint_id",
                    () =>
                    {
                        _socket.EmitAck(ToJson(new PinpointIdResponse(pinpointID, true)));
                        onConnected?.Invoke();
                    }
                );

                // On error
                _socket.Once(
                    "error",
                    () =>
                    {
                        var connectionErrorMessage =
                            $"Error connecting to proxy at {proxyAddress}:3000. Check proxy for details.";
                        Debug.LogWarning(connectionErrorMessage);
                        IsConnected = false;
                        _connectionManager.Close();
                        _connectionManager = null;
                        _socket = null;
                        onError?.Invoke(connectionErrorMessage);
                    }
                );

                // On timeout
                _socket.Once(
                    "connect_timeout",
                    () =>
                    {
                        var connectionTimeoutMessage =
                            $"Connection to proxy at {proxyAddress}:3000 timed out";
                        Debug.LogWarning(connectionTimeoutMessage);
                        IsConnected = false;
                        _connectionManager.Close();
                        _connectionManager = null;
                        _socket = null;
                        onError?.Invoke(connectionTimeoutMessage);
                    }
                );
            }
            catch (Exception e)
            {
                // On socket generation error
                var connectionErrorMessage =
                    $"Error connecting to proxy at {proxyAddress}:3000. Check proxy for details.";
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
            GetVersion(
                versionString =>
                {
                    var versionNumbers = versionString
                        .Split(".")
                        .Select(values => values.TakeWhile(char.IsDigit).ToArray())
                        .TakeWhile(numbers => numbers.Length > 0)
                        .Select(nonEmpty => int.Parse(new string(nonEmpty)))
                        .ToArray();
                    print(versionNumbers[0] + "." + versionNumbers[1] + "." + versionNumbers[2]);

                    // Fail if major version mismatch (breaking changes).
                    if (versionNumbers[0] != EPHYS_LINK_MIN_VERSION[0])
                    {
                        onFailure.Invoke();
                        return;
                    }

                    // Fail if minor version is too small (missing features).
                    if (versionNumbers[1] < EPHYS_LINK_MIN_VERSION[1])
                    {
                        onFailure.Invoke();
                        return;
                    }

                    // Fail if patch version is too small and minor version is not greater (bug fixes).
                    if (
                        versionNumbers[1] == EPHYS_LINK_MIN_VERSION[1]
                        && versionNumbers[2] < EPHYS_LINK_MIN_VERSION[2]
                    )
                    {
                        onFailure.Invoke();
                        return;
                    }

                    // Passed checks.
                    onSuccess.Invoke();
                },
                onFailure.Invoke
            );
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
            _connectionManager
                .Socket.ExpectAcknowledgement<string>(data =>
                {
                    if (DataKnownAndNotEmpty(data))
                        onSuccessCallback?.Invoke(data);
                    else
                        onErrorCallback?.Invoke();
                })
                .Emit("get_version");
        }

        /// <summary>
        ///     Get the platform type.
        /// </summary>
        /// <param name="onSuccessCallback">Callback function to handle incoming platform type info.</param>
        /// <param name="onErrorCallback">Callback function to handle errors.</param>
        public void GetPlatformType(Action<string> onSuccessCallback, Action onErrorCallback = null)
        {
            _connectionManager
                .Socket.ExpectAcknowledgement<string>(data =>
                {
                    if (DataKnownAndNotEmpty(data))
                        onSuccessCallback?.Invoke(data);
                    else
                        onErrorCallback?.Invoke();
                })
                .Emit("get_platform_type");
        }

        /// <summary>
        ///     Get manipulators event sender.
        /// </summary>
        /// <param name="onSuccessCallback">Callback function to handle incoming manipulator ID's</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void GetManipulators(
            Action<GetManipulatorsResponse> onSuccessCallback,
            Action<string> onErrorCallback = null
        )
        {
            _connectionManager
                .Socket.ExpectAcknowledgement<string>(data =>
                {
                    if (DataKnownAndNotEmpty(data))
                    {
                        var parsedData = ParseJson<GetManipulatorsResponse>(data);

                        if (string.IsNullOrEmpty(parsedData.Error))
                            onSuccessCallback?.Invoke(parsedData);
                        else
                            onErrorCallback?.Invoke(parsedData.Error);
                    }
                    else
                    {
                        onErrorCallback?.Invoke($"get_manipulators invalid response: {data}");
                    }
                })
                .Emit("get_manipulators");
        }

        /// <summary>
        ///     Request the current position of a manipulator (mm).
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to get the position of</param>
        /// <param name="onSuccessCallback">Callback function to pass manipulator position to</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void GetPosition(
            string manipulatorId,
            Action<Vector4> onSuccessCallback,
            Action<string> onErrorCallback = null
        )
        {
            _connectionManager
                .Socket.ExpectAcknowledgement<string>(data =>
                {
                    if (DataKnownAndNotEmpty(data))
                    {
                        var parsedData = ParseJson<PositionalResponse>(data);

                        if (string.IsNullOrEmpty(parsedData.Error))
                            onSuccessCallback?.Invoke(parsedData.Position);
                        else
                            onErrorCallback?.Invoke(parsedData.Error);
                    }
                    else
                    {
                        onErrorCallback?.Invoke($"get_pos invalid response: {data}");
                    }
                })
                .Emit("get_position", manipulatorId);
        }

        /// <summary>
        ///     Request the current angles of a manipulator.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to get the position of</param>
        /// <param name="onSuccessCallback">Callback function to pass manipulator angles to</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void GetAngles(
            string manipulatorId,
            Action<Vector3> onSuccessCallback,
            Action<string> onErrorCallback = null
        )
        {
            _connectionManager
                .Socket.ExpectAcknowledgement<string>(data =>
                {
                    if (DataKnownAndNotEmpty(data))
                    {
                        var parsedData = ParseJson<AngularResponse>(data);
                        if (string.IsNullOrEmpty(parsedData.Error))
                            onSuccessCallback?.Invoke(parsedData.Angles);
                        else
                            onErrorCallback?.Invoke(parsedData.Error);
                    }
                    else
                    {
                        onErrorCallback?.Invoke($"get_angles invalid response: {data}");
                    }
                })
                .Emit("get_angles", manipulatorId);
        }

        public void GetShankCount(
            string manipulatorId,
            Action<int> onSuccessCallback,
            Action<string> onErrorCallback = null
        )
        {
            _connectionManager
                .Socket.ExpectAcknowledgement<string>(data =>
                {
                    if (DataKnownAndNotEmpty(data))
                    {
                        var parsedData = ParseJson<ShankCountResponse>(data);
                        if (string.IsNullOrEmpty(parsedData.Error))
                            onSuccessCallback?.Invoke(parsedData.ShankCount);
                        else
                            onErrorCallback?.Invoke(parsedData.Error);
                    }
                    else
                    {
                        onErrorCallback?.Invoke($"get_shank_count invalid response: {data}");
                    }
                })
                .Emit("get_shank_count", manipulatorId);
        }

        /// <summary>
        ///     Request a manipulator be moved to a specific position.
        /// </summary>
        /// <remarks>Position is defined by a Vector4</remarks>
        /// <param name="request">Goto position request object</param>
        /// <param name="onSuccessCallback">Callback function to handle successful manipulator movement</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void SetPosition(
            SetPositionRequest request,
            Action<Vector4> onSuccessCallback,
            Action<string> onErrorCallback = null
        )
        {
            _connectionManager
                .Socket.ExpectAcknowledgement<string>(data =>
                {
                    if (DataKnownAndNotEmpty(data))
                    {
                        var parsedData = ParseJson<PositionalResponse>(data);
                        if (string.IsNullOrEmpty(parsedData.Error))
                            onSuccessCallback?.Invoke(parsedData.Position);
                        else
                            onErrorCallback?.Invoke(parsedData.Error);
                    }
                    else
                    {
                        onErrorCallback?.Invoke($"goto_pos invalid response: {data}");
                    }
                })
                .Emit("set_position", ToJson(request));
        }

        /// <summary>
        ///     Request a manipulator drive down to a specific depth.
        /// </summary>
        /// <param name="request">Drive to depth request</param>
        /// <param name="onSuccessCallback">Callback function to handle successful manipulator movement</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void SetDepth(
            SetDepthRequest request,
            Action<float> onSuccessCallback,
            Action<string> onErrorCallback
        )
        {
            _connectionManager
                .Socket.ExpectAcknowledgement<string>(data =>
                {
                    if (DataKnownAndNotEmpty(data))
                    {
                        var parsedData = ParseJson<SetDepthResponse>(data);
                        if (string.IsNullOrEmpty(parsedData.Error))
                            onSuccessCallback?.Invoke(parsedData.Depth);
                        else
                            onErrorCallback?.Invoke(parsedData.Error);
                    }
                    else
                    {
                        onErrorCallback?.Invoke($"drive_to_depth invalid response: {data}");
                    }
                })
                .Emit("set_depth", ToJson(request));
        }

        /// <summary>
        ///     Set the inside brain state of a manipulator.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="onSuccessCallback">Callback function to handle setting inside_brain state successfully</param>
        /// <param name="onErrorCallback">Callback function to handle errors</param>
        public void SetInsideBrain(
            SetInsideBrainRequest request,
            Action<bool> onSuccessCallback,
            Action<string> onErrorCallback = null
        )
        {
            _connectionManager
                .Socket.ExpectAcknowledgement<string>(data =>
                {
                    if (DataKnownAndNotEmpty(data))
                    {
                        var parsedData = ParseJson<BooleanStateResponse>(data);
                        if (string.IsNullOrEmpty(parsedData.Error))
                            onSuccessCallback?.Invoke(parsedData.State);
                        else
                            onErrorCallback?.Invoke(parsedData.Error);
                    }
                    else
                    {
                        onErrorCallback?.Invoke($"set_inside_brain invalid response: {data}");
                    }
                })
                .Emit("set_inside_brain", ToJson(request));
        }

        /// <summary>
        /// Request a manipulator stops moving.
        /// </summary>
        /// <param name="manipulatorId"></param>
        /// <param name="onSuccessCallback"></param>
        /// <param name="onErrorCallback"></param>
        public void Stop(
            string manipulatorId,
            Action onSuccessCallback,
            Action<string> onErrorCallback
        )
        {
            _connectionManager
                .Socket.ExpectAcknowledgement<string>(data =>
                {
                    if (DataKnownAndNotEmpty(data))
                    {
                        // Non-empty response means error.
                        onErrorCallback?.Invoke(data);
                    }
                    else
                    {
                        // Empty response means success.
                        onSuccessCallback?.Invoke();
                    }
                })
                .Emit("stop", manipulatorId);
        }

        /// <summary>
        ///     Request all movement to stop.
        /// </summary>
        /// <param name="onSuccessCallback">Handle successful stop.</param>
        /// <param name="onErrorCallback">Handle failed stops.</param>
        public void StopAll(Action onSuccessCallback, Action<string> onErrorCallback)
        {
            _connectionManager
                .Socket.ExpectAcknowledgement<string>(data =>
                {
                    if (DataKnownAndNotEmpty(data))
                    {
                        // Non-empty response means error.
                        onErrorCallback?.Invoke(data);
                    }
                    else
                    {
                        // Empty response means success.
                        onSuccessCallback?.Invoke();
                    }
                })
                .Emit("stop_all");
        }

        #endregion

        #region Helper functions

        private static bool DataKnownAndNotEmpty(string data)
        {
            return !string.IsNullOrEmpty(data) && !data.Equals(UNKOWN_EVENT);
        }

        private static T ParseJson<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }

        private static string ToJson<T>(T data)
        {
            return JsonUtility.ToJson(data);
        }

        #endregion
    }
}
