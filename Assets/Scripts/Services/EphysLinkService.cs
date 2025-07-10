using System;
using System.Globalization;
using System.IO;
using System.Linq;
using BestHTTP.SocketIO3;
using Models;
using Models.Automation;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using Action = System.Action;

namespace Services
{
    public class EphysLinkService
    {
        #region Constants

        private const string UNKOWN_EVENT_RESPONSE = "{\"error\": \"Unknown event.\"}";

        #endregion

        #region Services

        [Service]
        private StoreService _storeService;

        #endregion

        #region Components

        private SocketManager _socketManager;
        private Socket _socket;

        #endregion

        #region Connection Handling


        /// <summary>
        ///     Create a connection to the server.
        /// </summary>
        /// <param name="ip">IP address of the server</param>
        /// <param name="port">Port of the server</param>
        /// <param name="onConnected">Post successful connection behavior.</param>
        /// <param name="onError">Post error behavior with message.</param>
        public void ConnectToServer(
            string ip,
            int port,
            Action onConnected = null,
            System.Action<string> onError = null
        )
        {
            // Disconnect the old connection if needed
            if (_socketManager != null && _socketManager.Socket.IsOpen)
                _socketManager.Close();

            // Create new connection
            var options = new SocketOptions { Timeout = new TimeSpan(0, 0, 2) };

            // Try to open a connection
            try
            {
                // Create a new socket
                _socketManager = new SocketManager(new Uri($"http://{ip}:{port}"), options);
                _socket = _socketManager.Socket;

                // On successful connection
                _socket.Once(
                    "connect",
                    () =>
                    {
                        _storeService.Store.Dispatch(EphysLinkActions.SET_IS_CONNECTED, true);
                        onConnected?.Invoke();
                    }
                );

                // On error
                _socket.Once("error", () => HandleError(GetErrorConnectingToServerMessage()));

                // On timeout
                _socket.Once("connect_timeout", () => HandleError(GetConnectionTimeoutMessage()));
            }
            catch (Exception e)
            {
                HandleError($"{GetErrorConnectingToServerMessage()} Caused exception: {e}");
            }

            return;

            string GetErrorConnectingToServerMessage() =>
                $"Error connecting to server at {ip}:{port}. Check server for details.";
            string GetConnectionTimeoutMessage() =>
                $"Connection to server at {ip}:{port} timed out.";

            void HandleError(string message)
            {
                HandleDisconnect();
                onError?.Invoke(message);
            }
        }

        /// <summary>
        /// Disconnect from the server and clean up resources.
        /// </summary>
        /// <param name="onDisconnected">Post disconnection behavior.</param>
        public void HandleDisconnect(Action onDisconnected = null)
        {
            _storeService.Store.Dispatch(EphysLinkActions.SET_IS_CONNECTED, false);
            _socketManager?.Close();
            _socketManager = null;
            _socket = null;
            onDisconnected?.Invoke();
        }

        public async Awaitable<bool> IsVersionCompatible()
        {
            // Get version string from the server.
            var versionResponse = await GetVersion();

            // Extract the version number from the response.
            var versionNumbers = versionResponse
                .Split(".")
                .Select(values => values.TakeWhile(char.IsDigit).ToArray())
                .TakeWhile(numbers => numbers.Length > 0)
                .Select(nonEmpty => int.Parse(new string(nonEmpty)))
                .ToArray();

            // Read the minimum version from the store.
            var ephysLinkMinVersion = _storeService
                .Store.GetState<EphysLinkState>(SliceNames.EPHYS_LINK_SLICE)
                .EphysLinkMinVersion;

            // Check semantic version compatibility.
            return versionNumbers[0] == ephysLinkMinVersion[0]
                && versionNumbers[1] >= ephysLinkMinVersion[1]
                && (
                    versionNumbers[1] > ephysLinkMinVersion[1]
                    || versionNumbers[2] >= ephysLinkMinVersion[2]
                );
        }

        #endregion

        #region Event Handlers

        /// <summary>
        ///     Get Ephys Link version.
        /// </summary>
        /// <returns>Version number.</returns>
        private async Awaitable<string> GetVersion()
        {
            return await EmitAndGetStringResponse<object>("get_version", null);
        }

        /// <summary>
        ///     Get the platform info.
        /// </summary>
        /// <returns>Platform info.</returns>
        public async Awaitable<PlatformInfo> GetPlatformInfo()
        {
            return await EmitAndGetResponse<PlatformInfo, object>("get_platform_info", null);
        }

        /// <summary>
        ///     Get connected manipulators and some basic information about them.
        /// </summary>
        /// <returns>Manipulators and their information.</returns>
        public async Awaitable<GetManipulatorsResponse> GetManipulators()
        {
            return await EmitAndGetResponse<GetManipulatorsResponse, object>(
                "get_manipulators",
                null
            );
        }

        /// <summary>
        ///     Request the current position of a manipulator (mm).
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to get teh position of.</param>
        /// <returns><see cref="PositionalResponse" /> with manipulator's position.</returns>
        public async Awaitable<PositionalResponse> GetPosition(string manipulatorId)
        {
            return await EmitAndGetResponse<PositionalResponse, string>(
                "get_position",
                manipulatorId
            );
        }

        /// <summary>
        ///     Request the current angles of a manipulator.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to get the position of</param>
        /// <returns><see cref="AngularResponse" /> with manipulator's angles.</returns>
        public async Awaitable<AngularResponse> GetAngles(string manipulatorId)
        {
            return await EmitAndGetResponse<AngularResponse, string>("get_angles", manipulatorId);
        }

        /// <summary>
        ///     Request the number of shanks on a manipulator.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to get the shank count of.</param>
        /// <returns><see cref="ShankCountResponse" /> with the number of shanks.</returns>
        public async Awaitable<ShankCountResponse> GetShankCount(string manipulatorId)
        {
            return await EmitAndGetResponse<ShankCountResponse, string>(
                "get_shank_count",
                manipulatorId
            );
        }

        /// <summary>
        ///     Request a manipulator be moved to a specific position.
        /// </summary>
        /// <param name="request">Goto position request object</param>
        /// <returns><see cref="PositionalResponse" /> with the manipulator's new position.</returns>
        public async Awaitable<PositionalResponse> SetPosition(SetPositionRequest request)
        {
            return await EmitAndGetResponse<PositionalResponse, SetPositionRequest>(
                "set_position",
                request
            );
        }

        /// <summary>
        ///     Request a manipulator drive down to a specific depth.
        /// </summary>
        /// <param name="request">Drive to depth request</param>
        /// <returns><see cref="SetDepthResponse" /> with the manipulator's new depth.</returns>
        public async Awaitable<SetDepthResponse> SetDepth(SetDepthRequest request)
        {
            return await EmitAndGetResponse<SetDepthResponse, SetDepthRequest>(
                "set_depth",
                request
            );
        }

        /// <summary>
        ///     Set the inside brain state of a manipulator.
        /// </summary>
        /// <param name="request">Set inside brain request.</param>
        /// <returns><see cref="BooleanStateResponse" /> with the manipulator's new inside brain state.</returns>
        public async Awaitable<BooleanStateResponse> SetInsideBrain(SetInsideBrainRequest request)
        {
            return await EmitAndGetResponse<BooleanStateResponse, SetInsideBrainRequest>(
                "set_inside_brain",
                request
            );
        }

        /// <summary>
        ///     Request a manipulator stops moving.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to stop</param>
        /// <returns>Empty string if successful, error message if failed.</returns>
        public async Awaitable<string> Stop(string manipulatorId)
        {
            return await EmitAndGetStringResponse("stop", manipulatorId);
        }

        /// <summary>
        ///     Request all manipulators to stop.
        /// </summary>
        /// <returns>Empty string if successful, error message if failed.</returns>
        public async Awaitable<string> StopAll()
        {
            return await EmitAndGetStringResponse<object>("stop_all", null);
        }

        #endregion


        #region Helper Functions

        /// <summary>
        ///     Quick error handler to log the error string if it exists.
        /// </summary>
        /// <param name="error">Error response to check.</param>
        /// <returns>True if there was an error, false otherwise.</returns>
        public static bool HasError(string error)
        {
            // Shortcut exit if there was no error.
            if (string.IsNullOrEmpty(error))
                return false;

            // Log the error.
            Debug.LogError(error);
            OutputLog.Log(
                new[]
                {
                    "ephys_link",
                    DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    $"ERROR: {error}",
                }
            );

            // Return true to indicate an error.
            return true;
        }

        /// <summary>
        ///     Generic function to emit and event and get a response from the server.
        /// </summary>
        /// <param name="eventName">Event to emit to.</param>
        /// <param name="requestParameter">Parameter to send with the event.</param>
        /// <typeparam name="T">Expected (parsed) response type.</typeparam>
        /// <typeparam name="TR">Type of the request parameter.</typeparam>
        /// <returns>Response from server. Parsed to <see cref="T" /> if it's not a string.</returns>
        /// <exception cref="InvalidDataException">Invalid response from server (empty or unknown).</exception>
        private async Awaitable<T> EmitAndGetResponse<T, TR>(string eventName, TR requestParameter)
        {
            // Query server and capture response.
            var dataCompletionSource = new AwaitableCompletionSource<string>();
            _socketManager
                .Socket.ExpectAcknowledgement<string>(data => dataCompletionSource.SetResult(data))
                .Emit(
                    eventName,
                    typeof(TR) == typeof(string) ? requestParameter : ToJson(requestParameter)
                );

            // Wait for data.
            var data = await dataCompletionSource.Awaitable;

            // Return data if it exists. Parse if return type is not string.
            if (DataKnownAndNotEmpty(data))
                return FromJson<T>(data);

            // Throw exception if data is empty.
            throw new InvalidDataException($"{eventName} invalid response: {data}");
        }

        /// <summary>
        ///     Emit an event and get a string response from the server.
        /// </summary>
        /// <param name="eventName">Event to emit to.</param>
        /// <param name="requestParameter">Parameter to send with the event.</param>
        /// <typeparam name="TR">Type of the request parameter.</typeparam>
        /// <returns>Response from server as a string.</returns>
        private async Awaitable<string> EmitAndGetStringResponse<TR>(
            string eventName,
            TR requestParameter
        )
        {
            // Query server and capture response.
            var dataCompletionSource = new AwaitableCompletionSource<string>();
            _socketManager
                .Socket.ExpectAcknowledgement<string>(data => dataCompletionSource.SetResult(data))
                .Emit(
                    eventName,
                    typeof(TR) == typeof(string) ? requestParameter : ToJson(requestParameter)
                );

            // Wait for data.
            var data = await dataCompletionSource.Awaitable;

            // Return data.
            return data;
        }

        /// <summary>
        ///     Check if data is not empty and is not the "unkown event" error.
        /// </summary>
        /// <param name="data">Data to check.</param>
        /// <returns>True if data is not empty and not the "unkown event" error, false otherwise.</returns>
        private static bool DataKnownAndNotEmpty(string data)
        {
            return !string.IsNullOrEmpty(data) && !data.Equals(UNKOWN_EVENT_RESPONSE);
        }

        /// <summary>
        ///     Parse a JSON string into a data object.
        /// </summary>
        /// <param name="json">JSON string to parse.</param>
        /// <typeparam name="T">Type of the data object.</typeparam>
        /// <returns>Parsed data object.</returns>
        private static T FromJson<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }

        /// <summary>
        ///     Convert a data object into a JSON string.
        /// </summary>
        /// <param name="data">Data object to convert.</param>
        /// <typeparam name="T">Type of the data object.</typeparam>
        /// <returns>JSON string.</returns>
        private static string ToJson<T>(T data)
        {
            return JsonUtility.ToJson(data);
        }

        #endregion
    }
}
