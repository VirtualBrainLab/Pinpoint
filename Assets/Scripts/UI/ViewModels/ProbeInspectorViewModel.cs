using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Models;
using Models.Scene;
using Models.Settings;
using Pinpoint.Probes;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using Utils.Types;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class ProbeInspectorViewModel
    {
        #region Constants

        private readonly Vector2 _pitchRange = new(0, 90);

        #endregion

        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;
        private readonly IDisposableSubscription _settingsStateSubscription;

        private string ActiveProbeName =>
            _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE).ActiveProbeName;

        #endregion

        #region Visualization Polling

        private CancellationTokenSource _visualizationPollingCts;
        private const int VISUALIZATION_POLLING_INTERVAL_MS = 16; // ~60 Hz
        private bool _isCurrentProbeVisualization;
        private ProbeController _currentProbeController;

        #endregion

        #region Properties

        [ObservableProperty]
        private Vector3 _position;

        [ObservableProperty]
        private Vector3 _angles;

        [ObservableProperty]
        private bool _locked;

        [ObservableProperty]
        private ProbeColor _probeColor;

        [ObservableProperty]
        private string _visualizingManipulatorId;

        [ObservableProperty]
        // ReSharper disable once InconsistentNaming
        private bool _convertAPML2Probe;

        #endregion

        public ProbeInspectorViewModel(StoreService storeService)
        {
            _storeService = storeService;

            _sceneStateSubscription = _storeService.Store.Subscribe(
                state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                OnSceneStateChanged,
                new SubscribeOptions<SceneState> { fireImmediately = true }
            );
            _settingsStateSubscription = _storeService.Store.Subscribe(
                state => state.Get<SettingsState>(SliceNames.SETTINGS_SLICE),
                OnSettingsStateChanged,
                new SubscribeOptions<SettingsState> { fireImmediately = true }
            );
            App.shuttingDown += OnShuttingDown;
        }

        private void OnSceneStateChanged(SceneState sceneState)
        {
            if (string.IsNullOrEmpty(sceneState.ActiveProbeName))
            {
                StopVisualizationPolling();
                return;
            }

            // Check if this probe is a visualization probe
            var isVisualizationProbe = sceneState.Manipulators.Exists(manipulator =>
                manipulator.VisualizationProbeName == sceneState.ActiveProbeName
            );

            // Get ProbeController reference
            ProbeController probeController = null;
            if (isVisualizationProbe)
            {
                var probeManager = ProbeManager.Instances.FirstOrDefault(m =>
                    m.name == sceneState.ActiveProbeName
                );
                probeController = probeManager?.ProbeController;

                // If we can't find the controller, fall back to state-based updates
                if (probeController == null)
                {
                    isVisualizationProbe = false;
                }
            }

            // Handle state transitions
            var wasVisualization = _isCurrentProbeVisualization;
            _isCurrentProbeVisualization = isVisualizationProbe;
            _currentProbeController = probeController;

            if (isVisualizationProbe && probeController != null)
            {
                // Start polling if transitioning TO visualization probe
                if (!wasVisualization)
                {
                    StartVisualizationPolling();
                }
                // Initial update from local fields
                UpdateFromProbeControllerFields(probeController);
            }
            else
            {
                // Stop polling if transitioning FROM visualization probe
                if (wasVisualization)
                {
                    StopVisualizationPolling();
                }
                // Update from state (normal behavior)
                UpdateFromSceneState(sceneState);
            }

            // Update properties that don't change based on source
            Locked = sceneState.ActiveProbeState.Locked;
            ProbeColor = sceneState.ActiveProbeState.Color;
            VisualizingManipulatorId =
                sceneState
                    .Manipulators.FirstOrDefault(state =>
                        state.VisualizationProbeName == sceneState.ActiveProbeName
                    )
                    ?.Id
                ?? string.Empty;
        }

        private void OnSettingsStateChanged(SettingsState settingsState)
        {
            ConvertAPML2Probe = settingsState.ConvertAPML2Probe;
        }

        private void UpdateFromProbeControllerFields(ProbeController controller)
        {
            var apmldv = controller.VisualizationLocalAPMLDV;
            var angles = controller.VisualizationLocalAngles;

            if (_convertAPML2Probe)
            {
                var cos = Mathf.Cos(-angles.x * Mathf.Deg2Rad);
                var sin = Mathf.Sin(-angles.x * Mathf.Deg2Rad);

                var xRot = apmldv.x * cos - apmldv.y * sin;
                var yRot = apmldv.x * sin + apmldv.y * cos;

                Position = new Vector3(xRot, yRot, apmldv.z);
            }
            else
            {
                Position = apmldv;
            }

            Angles = angles;
        }

        private void UpdateFromSceneState(SceneState sceneState)
        {
            var apmldv = sceneState.ActiveProbeState.APMLDV;
            var angles = sceneState.ActiveProbeState.Angles;

            if (_convertAPML2Probe)
            {
                var cos = Mathf.Cos(-angles.x * Mathf.Deg2Rad);
                var sin = Mathf.Sin(-angles.x * Mathf.Deg2Rad);

                var xRot = apmldv.x * cos - apmldv.y * sin;
                var yRot = apmldv.x * sin + apmldv.y * cos;

                Position = new Vector3(xRot, yRot, apmldv.z);
            }
            else
            {
                Position = apmldv;
            }

            Angles = angles;
        }

        private void StartVisualizationPolling()
        {
            StopVisualizationPolling(); // Ensure only one loop runs
            _visualizationPollingCts = new CancellationTokenSource();
            var token = _visualizationPollingCts.Token;
            _ = VisualizationPollingLoop(token);
        }

        private void StopVisualizationPolling()
        {
            if (_visualizationPollingCts == null)
                return;

            try
            {
                _visualizationPollingCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Ignore - already disposed
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Exception during visualization polling cancellation: {ex}");
            }

            _visualizationPollingCts?.Dispose();
            _visualizationPollingCts = null;
        }

        private async Task VisualizationPollingLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_currentProbeController != null && _isCurrentProbeVisualization)
                    {
                        var sceneState = _storeService.Store.GetState<SceneState>(
                            SliceNames.SCENE_SLICE
                        );
                        Debug.Log($"Update for visualization probe: {sceneState.ActiveProbeName}");

                        // Only update if this is still the active probe
                        if (sceneState.ActiveProbeName == _currentProbeController.ProbeManager.name)
                        {
                            UpdateFromProbeControllerFields(_currentProbeController);
                        }
                        else
                        {
                            // Active probe changed, stop polling
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Visualization polling error: {ex.Message}");
                }

                try
                {
                    await Task.Delay(VISUALIZATION_POLLING_INTERVAL_MS, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void OnShuttingDown()
        {
            StopVisualizationPolling();
            _sceneStateSubscription.Dispose();
            _settingsStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }

        #region Commands

        [ICommand]
        private void SetPosition(Vector3 position)
        {
            var apmldv = position;

            if (_convertAPML2Probe)
            {
                var sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
                var angles = sceneState.ActiveProbeState.Angles;

                var cos = Mathf.Cos(-angles.x * Mathf.Deg2Rad);
                var sin = Mathf.Sin(-angles.x * Mathf.Deg2Rad);

                var xRot = position.x * cos + position.y * sin;
                var yRot = -position.x * sin + position.y * cos;

                apmldv = new Vector3(xRot, yRot, position.z);
            }

            _storeService.Store.Dispatch(
                SceneActions.SET_PROBE_POSITION,
                (ActiveProbeName, apmldv)
            );
        }

        [ICommand]
        private void SetAngles(Vector3 angles)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_PROBE_ANGLES,
                (ActiveProbeName, angles, _pitchRange)
            );
        }

        [ICommand]
        private void LockProbe()
        {
            _storeService.Store.Dispatch(SceneActions.SET_PROBE_LOCKED, (ActiveProbeName, !Locked));
        }

        [ICommand]
        private void DuplicateProbe()
        {
            _storeService.Store.Dispatch(SceneActions.DUPLICATE_PROBE, ActiveProbeName);
        }

        [ICommand]
        private void MoveProbeToReferenceCoordinate()
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_PROBE_POSITION,
                (ActiveProbeName, Vector3.zero)
            );
        }

        [ICommand]
        private void MoveProbeToDura()
        {
            ProbeManager
                .Instances.First(manager => manager.name == ActiveProbeName)
                .DropProbeToBrainSurface();
        }

        [ICommand]
        private void InspectVisualizingManipulator()
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_ACTIVE_MANIPULATOR,
                VisualizingManipulatorId
            );
        }

        [ICommand]
        private void SetProbeColor(ProbeColor color)
        {
            _storeService.Store.Dispatch(SceneActions.SET_PROBE_COLOR, (ActiveProbeName, color));
        }

        #endregion
    }
}
