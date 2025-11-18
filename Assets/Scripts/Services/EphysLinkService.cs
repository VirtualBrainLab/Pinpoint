using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BestHTTP.SocketIO3;
using BrainAtlas;
using BrainAtlas.CoordinateSystems;
using KS.Diagnostics;
using Models;
using Models.Scene;
using Models.Settings;
using Pinpoint.CoordinateSystems;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using UnityEngine;
using Utils;
using Utils.Types;
using Action = System.Action;

namespace Services
{
    public class EphysLinkService
    {
        #region Constants

        private const string UNKNOWN_EVENT_RESPONSE = "{\"error\": \"Unknown event.\"}";

        // FIXME: This should go into some common constants file (along with copy in probe inspector view model).
        private readonly Vector2 _pitchRange = new(0, 90);

        // Visualization loop interval (ms)
        private const int VISUALIZATION_UPDATE_INTERVAL_MS = 10; // about 60 Hz
        #endregion

        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;

        #endregion

        #region Components

        private SocketManager _socketManager;
        private Socket _socket;
        private Process _ephysLinkProcess;

        public string SocketId => _socket.Id;

        #endregion

        #region Demo Loop

        private readonly HashSet<string> _runningDemoLoops = new();
        private const float DEMO_SPEED = 0.5f; // mm/s

        // Cancellation for the visualization update loop
        private CancellationTokenSource _visualizationLoopCts;

        #endregion

        public EphysLinkService(StoreService storeService)
        {
            // Register services.
            _storeService = storeService;

            // Subscribe to scene state changes and initialize properties.
            _sceneStateSubscription = _storeService.Store.Subscribe(
                state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                OnSceneStateChanged,
                new SubscribeOptions<SceneState> { fireImmediately = true }
            );
            App.shuttingDown += OnShuttingDown;
        }

        private async void OnSceneStateChanged(SceneState sceneState)
        {
            // Handle demo loop state changes.
            foreach (var manipulatorState in sceneState.Manipulators)
            {
                var isRunning = _runningDemoLoops.Contains(manipulatorState.Id);

                switch (manipulatorState.IsDemoRunning)
                {
                    // Start demo loop if requested and not already running.
                    case true when !isRunning:
                        _runningDemoLoops.Add(manipulatorState.Id);
                        _ = RunDemoLoop(manipulatorState.Id);
                        break;
                    // Stop demo loop if requested and currently running.
                    case false when isRunning:
                        await Stop(manipulatorState.Id);
                        _runningDemoLoops.Remove(manipulatorState.Id);
                        break;
                }
            }
        }

        private void OnShuttingDown()
        {
            _sceneStateSubscription.Dispose();
            StopVisualizationLoop();
            App.shuttingDown -= OnShuttingDown;
        }

        #region Connection Handling

        /// <summary>
        ///     Create a connection to the server.
        /// </summary>
        /// <param name="ip">IP address of the server.</param>
        /// <param name="port">Port of the server.</param>
        /// <param name="onConnected">Post successful connection behavior.</param>
        /// <param name="onError">Post error behavior with message.</param>
        public void ConnectToServer(
            string ip,
            int port,
            Action onConnected = null,
            System.Action<string> onError = null
        )
        {
            // Disconnect the old connection if needed.
            if (_socketManager != null && _socketManager.Socket.IsOpen)
                _socketManager.Close();

            // Ensure any previous loop is stopped.
            StopVisualizationLoop();

            // Create new connection.
            var options = new SocketOptions { Timeout = new TimeSpan(0, 0, 2) };

            // Try to open a connection.
            try
            {
                // Create a new socket.
                _socketManager = new SocketManager(new Uri($"http://{ip}:{port}"), options);
                _socket = _socketManager.Socket;

                // Local async handler to run after successful connection.
                async Task OnConnectedAsync()
                {
                    try
                    {
                        // Check version compatibility.
                        if (await IsVersionCompatible())
                        {
                            var platformInfoResponse = await GetPlatformInfo();
                            _storeService.Store.Dispatch(
                                SceneActions.SET_PLATFORM_INFO,
                                (platformInfoResponse.AxesCount, platformInfoResponse.Dimensions)
                            );
                            _storeService.Store.Dispatch(
                                SettingsActions.SET_EPHYS_LINK_CONNECTION_STATE,
                                (EphysLinkConnectionState.Connected, _socket.Id)
                            );
                            onConnected?.Invoke();

                            // Start visualization update loop until disconnect.
                            StartVisualizationLoop();
                        }
                        else
                        {
                            HandleError(GetOutdatedVersionErrorMessage());
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleError(
                            $"{GetErrorConnectingToServerMessage()} Caused exception: {ex.Message}"
                        );
                    }
                }

                // When the server disconnects, stop the visualization loop and update state.
                _socket.On(
                    "disconnect",
                    () =>
                    {
                        StopVisualizationLoop();
                        _storeService.Store.Dispatch(
                            SettingsActions.SET_EPHYS_LINK_CONNECTION_STATE,
                            (EphysLinkConnectionState.Disconnected, "")
                        );
                    }
                );

                // On successful connection, delegate to async task handler.
                _socket.Once(
                    "connect",
                    () =>
                    {
                        _ = OnConnectedAsync();
                    }
                );

                // On error.
                _socket.Once("error", () => HandleError(GetErrorConnectingToServerMessage()));

                // On timeout.
                _socket.Once("connect_timeout", () => HandleError(GetConnectionTimeoutMessage()));
            }
            catch (Exception e)
            {
                HandleError($"{GetErrorConnectingToServerMessage()} Caused exception: {e}");
            }

            return;

            string GetErrorConnectingToServerMessage()
            {
                return $"Error connecting to server at {ip}:{port}. Check server for details.";
            }

            string GetConnectionTimeoutMessage()
            {
                return $"Connection to server at {ip}:{port} timed out.";
            }

            string GetOutdatedVersionErrorMessage()
            {
                return $"Ephys Link is outdated. Please update to ≥{EphysLinkConstants.EphysLinkMinVersion} or later.";
            }

            void HandleError(string message)
            {
                Disconnect();
                onError?.Invoke(message);
            }
        }

        /// <summary>
        ///     Disconnect from the server and clean up resources.
        /// </summary>
        /// <param name="onDisconnected">Post disconnection behavior.</param>
        public void Disconnect(Action onDisconnected = null)
        {
            // Stop the visualization loop if running.
            StopVisualizationLoop();

            // Close socket connection.
            _socketManager?.Close();
            _socketManager = null;
            _socket = null;

            // Kill the Ephys Link process if it exists.
            _ephysLinkProcess?.Kill(true);
            _ephysLinkProcess?.Dispose();
            _ephysLinkProcess = null;

            // Update the store state to disconnected.
            _storeService.Store.Dispatch(
                SettingsActions.SET_EPHYS_LINK_CONNECTION_STATE,
                (EphysLinkConnectionState.Disconnected, "")
            );
            onDisconnected?.Invoke();
        }

        private async Awaitable<bool> IsVersionCompatible()
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

            // Check semantic version compatibility.
            return versionNumbers[0] == EphysLinkConstants.EPHYS_LINK_MIN_VERSION_MAJOR
                && versionNumbers[1] >= EphysLinkConstants.EPHYS_LINK_MIN_VERSION_MINOR
                && (
                    versionNumbers[1] > EphysLinkConstants.EPHYS_LINK_MIN_VERSION_MINOR
                    || versionNumbers[2] >= EphysLinkConstants.EPHYS_LINK_MIN_VERSION_PATCH
                );
        }

        public void Launch()
        {
            var settingsState = _storeService.Store.GetState<SettingsState>(
                SliceNames.SETTINGS_SLICE
            );

            // Create launch arguments.
            var args = "-i -t ";
            switch (settingsState.SelectedEphysLinkPlatformType)
            {
                case EphysLinkPlatformType.SensapexUmp:
                    args += "ump";
                    break;
                case EphysLinkPlatformType.NewScalePathfinderMpm:
                    args += $"pathfinder-mpm --mpm-port {settingsState.NewScalePathfinderMpmPort}";
                    break;
                case EphysLinkPlatformType.Custom:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Launch process.
            _ephysLinkProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = EphysLinkConstants.EphysLinkExePath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    CreateNoWindow = false,
                },
            };
            _ephysLinkProcess.Start();
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
            return !string.IsNullOrEmpty(data) && !data.Equals(UNKNOWN_EVENT_RESPONSE);
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

        #region Visualization control

        // Starts the continuous visualization update loop until the socket disconnects or service disconnects.
        private void StartVisualizationLoop()
        {
            StopVisualizationLoop(); // ensure only one loop runs
            _visualizationLoopCts = new CancellationTokenSource();
            var token = _visualizationLoopCts.Token;
            _ = VisualizationUpdateLoop(token);
        }

        // Cancels and disposes the visualization update loop.
        private void StopVisualizationLoop()
        {
            if (_visualizationLoopCts == null)
                return;
            try
            {
                _visualizationLoopCts.Cancel();
            }
            catch (ObjectDisposedException)
            { /* ignore disposed */
            }
            catch (Exception ex)
            {
                Debug.Log($"Ignored exception during visualization loop cancellation: {ex}");
            }
            _visualizationLoopCts.Dispose();
            _visualizationLoopCts = null;
        }

        // The loop body calling UpdateVisualizationProbePosition at a fixed interval.
        private async Task VisualizationUpdateLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var sceneState = _storeService.Store.GetState<SceneState>(
                        SliceNames.SCENE_SLICE
                    );
                    await UpdateVisualizationProbePosition(sceneState);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Visualization update loop error: {ex.Message}");
                }

                // Update delay.
                try
                {
                    await Task.Delay(VISUALIZATION_UPDATE_INTERVAL_MS, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task UpdateVisualizationProbePosition(SceneState sceneState)
        {
            foreach (
                var manipulatorState in sceneState.Manipulators.Where(state =>
                    !string.IsNullOrEmpty(state.VisualizationProbeName)
                )
            )
            {
                // Skip if the probe or manager couldn't be found.
                var visualizationProbeManager = ProbeManager.Instances.FirstOrDefault(manager =>
                    manager.name == manipulatorState.VisualizationProbeName
                );
                if (
                    sceneState.Probes.FirstOrDefault(state =>
                        state.Name == manipulatorState.VisualizationProbeName
                    ) == null
                    || visualizationProbeManager == null
                )
                    continue;

                // Get the current position of the manipulator.
                var positionResponse = await GetPosition(manipulatorState.Id);
                if (HasError(positionResponse.Error))
                    continue;

                // Apply reference coordinate offset.
                var referenceCoordinateAdjustedManipulatorPosition =
                    positionResponse.Position - manipulatorState.ReferenceCoordinateOffset;

                // Create the appropriate manipulator transform.
                CoordinateTransform transform = sceneState.NumberOfAxesOnManipulator switch
                {
                    3 => new ThreeAxisLeftHandedTransform(
                        manipulatorState.Angles.x,
                        manipulatorState.Angles.y
                    ),
                    4 => manipulatorState.Handedness switch
                    {
                        ManipulatorHandedness.Left => new FourAxisLeftHandedManipulatorTransform(
                            manipulatorState.Angles.x
                        ),
                        ManipulatorHandedness.Right => new FourAxisRightHandedManipulatorTransform(
                            manipulatorState.Angles.x
                        ),
                        _ => throw new ArgumentOutOfRangeException(),
                    },
                    _ => throw new ArgumentOutOfRangeException(),
                };

                // Convert to coordinate space.
                var manipulatorSpacePosition = transform.T2U(
                    referenceCoordinateAdjustedManipulatorPosition
                );

                // Dura offset adjustment.
                var duraOffsetAdjustment = float.IsNaN(manipulatorState.DuraOffset)
                    ? 0
                    : manipulatorState.DuraOffset;

                // Apply depth adjustment to manipulator position for non-3 axis manipulators.
                if (sceneState.NumberOfAxesOnManipulator == 4)
                    referenceCoordinateAdjustedManipulatorPosition.w += duraOffsetAdjustment;

                // Convert to world space.
                var referenceCoordinateAdjustedWorldPosition =
                    sceneState.ManipulatorCoordinateSpace.Space2World(manipulatorSpacePosition);

                // Change axes to match probe.
                var transformedAPMLDV = BrainAtlasManager.World2T_Vector(
                    referenceCoordinateAdjustedWorldPosition
                );

                // Cancel update if the manipulator's position did not change by a lot.
                var probeState = sceneState.Probes.FirstOrDefault(state =>
                    state.Name == manipulatorState.VisualizationProbeName
                );
                if (
                    probeState == null
                    || Vector3.SqrMagnitude(transformedAPMLDV - probeState.APMLDV) < 0.0001f
                )
                    continue;

                // Get the current forward vector of the probe.
                var forwardT = BrainAtlasManager.ActiveAtlasTransform.U2T_Vector(
                    BrainAtlasManager.ActiveReferenceAtlas.World2Atlas_Vector(
                        visualizationProbeManager.transform.forward
                    )
                );

                // Get the ProbeController for this visualization probe
                var probeController = visualizationProbeManager.ProbeController;
                if (probeController == null)
                    continue;

                var depth = sceneState.NumberOfAxesOnManipulator switch
                {
                    3 => duraOffsetAdjustment,
                    4 => referenceCoordinateAdjustedManipulatorPosition.w,
                    _ => throw new ValueOutOfRangeException(
                        "Number of axes on manipulator is invalid."
                    ),
                };

                // Write position data directly to ProbeController's local fields
                probeController.VisualizationLocalAPMLDV = transformedAPMLDV;
                probeController.VisualizationLocalDepth = depth;
                probeController.VisualizationLocalAngles = manipulatorState.Angles;
                probeController.VisualizationLocalForwardT = forwardT;

                // Mark probe as visualization probe to apply new data in Update().
                probeController.IsVisualizationProbe = true;
            }
        }

        public async Task SetManipulatorReferenceCoordinateToCurrentPosition(string manipulatorId)
        {
            var currentPositionResponse = await GetPosition(manipulatorId);
            if (HasError(currentPositionResponse.Error))
                return;
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_REFERENCE_COORDINATE_OFFSET,
                (manipulatorId, currentPositionResponse.Position)
            );
        }

        public async Task SetManipulatorDuraOffsetToCurrentDepth(string manipulatorId)
        {
            var currentPositionResponse = await GetPosition(manipulatorId);
            if (HasError(currentPositionResponse.Error))
                return;
            // TODO: Add check to ensure there is enough space for exit margin.

            // Get manipulator depth, atlas coordinate, and offset delta at dura.
            var duraDepth = currentPositionResponse.Position.w;
            Vector3 duraCoordinate;
            float duraOffsetDelta;

            // Get the visualization probe manager.
            var currentSceneState = _storeService.Store.GetState<SceneState>(
                SliceNames.SCENE_SLICE
            );
            var visualizationProbeManager = ProbeManager.Instances.FirstOrDefault(manager =>
                manager.name == currentSceneState.ActiveManipulatorState.VisualizationProbeName
            );
            var visualizationProbeState = currentSceneState.Probes.FirstOrDefault(state =>
                state.Name == currentSceneState.ActiveManipulatorState.VisualizationProbeName
            );

            Debug.Log("Collecting info");

            // Exit if we cannot find the visualization probe manager or state.
            if (visualizationProbeManager == null || visualizationProbeState == null)
                return;

            Debug.Log("Found viz states and manager");

            if (visualizationProbeManager.IsProbeInBrain())
            {
                Debug.Log("Recalculate to the surface");
                // Just calculate the distance from the probe tip position to the brain surface
                duraCoordinate = visualizationProbeManager
                    .GetSurfaceCoordinateT()
                    .surfaceCoordinateT;
                duraOffsetDelta = -visualizationProbeManager.GetSurfaceCoordinateT().depthT;
            }
            else
            {
                Debug.Log("Ourselves");
                // We need to calculate the surface coordinate ourselves
                var (brainSurfaceCoordinateIdx, _) =
                    visualizationProbeManager.CalculateEntryCoordinate();

                // Exit if not in brain.
                if (float.IsNaN(brainSurfaceCoordinateIdx.x))
                    return;

                duraCoordinate = BrainAtlasManager.ActiveAtlasTransform.U2T(
                    BrainAtlasManager.ActiveReferenceAtlas.World2Atlas(
                        BrainAtlasManager.ActiveReferenceAtlas.AtlasIdx2World(
                            brainSurfaceCoordinateIdx
                        )
                    )
                );

                duraOffsetDelta = Vector3.Distance(duraCoordinate, visualizationProbeState.APMLDV);
            }

            // Log the dura offset recalculation.
            _storeService.Store.Dispatch(
                SceneActions.SET_DURA_OFFSET,
                (manipulatorId, duraDepth, duraCoordinate, duraOffsetDelta)
            );
        }

        public async Task SetManipulatorDemoHomeCoordinateToCurrentPosition(string manipulatorId)
        {
            var currentPositionResponse = await GetPosition(manipulatorId);
            if (HasError(currentPositionResponse.Error))
                return;
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_DEMO_HOME_COORDINATE,
                (manipulatorId, currentPositionResponse.Position)
            );
        }

        public async Task SetManipulatorDemoTargetCoordinateToCurrentPosition(string manipulatorId)
        {
            var currentPositionResponse = await GetPosition(manipulatorId);
            if (HasError(currentPositionResponse.Error))
                return;
            _storeService.Store.Dispatch(
                SceneActions.SET_MANIPULATOR_DEMO_TARGET_COORDINATE,
                (manipulatorId, currentPositionResponse.Position)
            );
        }

        /// <summary>
        ///     Run the demo loop for a manipulator.
        ///     Sequence: Home -> Target (X,Z only) -> Target (Y,W only) -> Back to Target (X,Z only) -> repeat indefinitely
        ///     until IsDemoRunning is set to false or an error occurs.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to run demo loop for.</param>
        private async Task RunDemoLoop(string manipulatorId)
        {
            while (true)
            {
                // Get current manipulator state.
                var sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
                var manipulatorState = sceneState.Manipulators.FirstOrDefault(m =>
                    m.Id == manipulatorId
                );

                // Exit if manipulator not found.
                if (manipulatorState == null)
                {
                    _runningDemoLoops.Remove(manipulatorId);
                    return;
                }

                // Exit if demo is no longer running.
                if (!manipulatorState.IsDemoRunning)
                {
                    _runningDemoLoops.Remove(manipulatorId);
                    return;
                }

                // Step 1: Move to home position.
                var homeRequest = new SetPositionRequest(
                    manipulatorId,
                    manipulatorState.DemoHomeCoordinate,
                    DEMO_SPEED
                );
                var homeResponse = await SetPosition(homeRequest);

                // Exit on error (including stop request).
                if (HasError(homeResponse.Error))
                {
                    _runningDemoLoops.Remove(manipulatorId);
                    return;
                }

                // Refresh state and check if demo is still running.
                sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
                manipulatorState = sceneState.Manipulators.FirstOrDefault(m =>
                    m.Id == manipulatorId
                );
                if (manipulatorState is not { IsDemoRunning: true })
                {
                    _runningDemoLoops.Remove(manipulatorId);
                    return;
                }

                // Step 2: Move to target position on X and Z axes only (keep Y and W from home).
                var intermediatePosition = new Vector4(
                    manipulatorState.DemoTargetCoordinate.x,
                    manipulatorState.DemoHomeCoordinate.y,
                    manipulatorState.DemoTargetCoordinate.z,
                    manipulatorState.DemoHomeCoordinate.w
                );
                var intermediateRequest = new SetPositionRequest(
                    manipulatorId,
                    intermediatePosition,
                    DEMO_SPEED
                );
                var intermediateResponse = await SetPosition(intermediateRequest);

                // Exit on error (including stop request).
                if (HasError(intermediateResponse.Error))
                {
                    _runningDemoLoops.Remove(manipulatorId);
                    return;
                }

                // Refresh state and check if demo is still running.
                sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
                manipulatorState = sceneState.Manipulators.FirstOrDefault(m =>
                    m.Id == manipulatorId
                );
                if (manipulatorState is not { IsDemoRunning: true })
                {
                    _runningDemoLoops.Remove(manipulatorId);
                    return;
                }

                // Step 3: Move to target position on Y and W axes only (complete movement to target).
                var targetRequest = new SetPositionRequest(
                    manipulatorId,
                    manipulatorState.DemoTargetCoordinate,
                    DEMO_SPEED
                );
                var targetResponse = await SetPosition(targetRequest);

                // Exit on error (including stop request).
                if (HasError(targetResponse.Error))
                {
                    _runningDemoLoops.Remove(manipulatorId);
                    return;
                }

                // Refresh state and check if demo is still running.
                sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
                manipulatorState = sceneState.Manipulators.FirstOrDefault(m =>
                    m.Id == manipulatorId
                );
                if (manipulatorState is not { IsDemoRunning: true })
                {
                    _runningDemoLoops.Remove(manipulatorId);
                    return;
                }

                // Step 4: Move back to intermediate position (X,Z only, keeping Y,W from home).
                var returnToIntermediatePosition = new Vector4(
                    manipulatorState.DemoTargetCoordinate.x,
                    manipulatorState.DemoHomeCoordinate.y,
                    manipulatorState.DemoTargetCoordinate.z,
                    manipulatorState.DemoHomeCoordinate.w
                );
                var returnToIntermediateRequest = new SetPositionRequest(
                    manipulatorId,
                    returnToIntermediatePosition,
                    DEMO_SPEED
                );
                var returnToIntermediateResponse = await SetPosition(returnToIntermediateRequest);

                // Exit on error (including stop request).
                if (HasError(returnToIntermediateResponse.Error))
                {
                    _runningDemoLoops.Remove(manipulatorId);
                    return;
                }

                // Refresh state and check if demo is still running before next iteration.
                sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
                manipulatorState = sceneState.Manipulators.FirstOrDefault(m =>
                    m.Id == manipulatorId
                );
                if (manipulatorState is { IsDemoRunning: true })
                {
                    // Loop continues to next iteration (back to home).
                    continue;
                }

                _runningDemoLoops.Remove(manipulatorId);
                return;
            }
        }

        #endregion
    }
}
