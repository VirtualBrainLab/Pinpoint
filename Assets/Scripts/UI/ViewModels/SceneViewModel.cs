using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using Models.Scene;
using Models.Settings;
using Pinpoint.CoordinateSystems;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using Utils.Types;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class SceneViewModel
    {
        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;
        private readonly IDisposableSubscription _settingsStateSubscription;

        private readonly EphysLinkService _ephysLinkService;

        #endregion

        #region Properties

        [ObservableProperty]
        private int _selectedProbeIndex = -1;

        [ObservableProperty]
        private List<ProbeListItemViewModel> _probeListItemViewModels = new();

        [ObservableProperty]
        private int _selectedManipulatorIndex = -1;

        [ObservableProperty]
        private List<string> _manipulatorIds = new();

        [ObservableProperty]
        private EphysLinkConnectionState _ephysLinkConnectionState;

        #endregion

        #region Private Fields

        private List<ProbeState> _previousProbeStates = new();
        private string _previousActiveProbeName = string.Empty;

        #endregion

        public SceneViewModel(StoreService storeService, EphysLinkService ephysLinkService)
        {
            _storeService = storeService;
            _ephysLinkService = ephysLinkService;

            // Subscribe to state changes and initialize properties.
            _sceneStateSubscription = storeService.Store.Subscribe(
              state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                   OnSceneStateChanged,
                     new SubscribeOptions<SceneState> { fireImmediately = true }
          );
            _settingsStateSubscription = storeService.Store.Subscribe(
              state => state.Get<SettingsState>(SliceNames.SETTINGS_SLICE),
            OnSettingsStateChanged,
                    new SubscribeOptions<SettingsState> { fireImmediately = true }
                    );

            App.shuttingDown += OnShuttingDown;
        }

        private void OnSceneStateChanged(SceneState state)
        {
            // Update probe selection.
            SelectedProbeIndex = state.ActiveProbeIndex;

            // Map probes to view models.
            ProbeListItemViewModels = state
                       .Probes.Select(probeState => new ProbeListItemViewModel(probeState, _storeService))
                    .ToList();

            // Update manipulator selection.
            SelectedManipulatorIndex = state.ActiveManipulatorIndex;

            // Map manipulators to IDs.
            ManipulatorIds = state
            .Manipulators.Select(manipulatorState => manipulatorState.Id)
                .ToList();

            // Handle active probe changes - this bridges the gap between Redux state and ProbeManager GameObjects
            HandleActiveProbeChange(state.ActiveProbeName);

            // Check for probe position or angle changes and trigger collision detection
            CheckForProbeMovement(state.Probes);
        }

        /// <summary>
        /// Bridge between Redux state and ProbeManager GameObjects.
        /// When the active probe name changes in state, call SetActive() on the appropriate ProbeManager instances.
        /// </summary>
        private void HandleActiveProbeChange(string newActiveProbeName)
        {
            // Check if the active probe actually changed
            if (_previousActiveProbeName == newActiveProbeName)
                return;

            // Deactivate the previous active probe
            if (!string.IsNullOrEmpty(_previousActiveProbeName))
            {
                var previousProbe = ProbeManager.Instances.FirstOrDefault(pm => pm.name == _previousActiveProbeName);
                if (previousProbe != null)
                {
                    previousProbe.SetActive(false);
                }
            }

            // Activate the new active probe
            if (!string.IsNullOrEmpty(newActiveProbeName))
            {
                var newProbe = ProbeManager.Instances.FirstOrDefault(pm => pm.name == newActiveProbeName);
                if (newProbe != null)
                {
                    newProbe.SetActive(true);

                    // Also update the static ActiveProbeManager reference (for backward compatibility with old code)
                    ProbeManager.ActiveProbeManager = newProbe;
                }
                else
                {
                    // If the probe doesn't exist yet, it might be instantiated later
                    // The ProbeManager.OnProbeStateChanged will handle the initial setup
                    ProbeManager.ActiveProbeManager = null;
                }
            }
            else
            {
                // No active probe
                ProbeManager.ActiveProbeManager = null;
            }

            // Update the cached value
            _previousActiveProbeName = newActiveProbeName;
        }

        /// <summary>
        /// Check if any probe positions or angles have changed and trigger collision detection if so
        /// </summary>
        private void CheckForProbeMovement(List<ProbeState> currentProbeStates)
        {
            // If this is the first time or probe count changed, just update the cache
            if (_previousProbeStates.Count != currentProbeStates.Count)
            {
                _previousProbeStates = currentProbeStates.Select(p => p).ToList();
                ColliderManager.CheckForCollisions();
                return;
            }

            // Check if any probe's position or angles have changed
            bool probesMoved = false;
            for (int i = 0; i < currentProbeStates.Count; i++)
            {
                var current = currentProbeStates[i];
                var previous = _previousProbeStates.FirstOrDefault(p => p.Name == current.Name);

                if (previous != null)
                {
                    // Check if position or angles changed
                    if (current.APMLDV != previous.APMLDV || current.Angles != previous.Angles)
                    {
                        probesMoved = true;
                        break;
                    }
                }
            }

            // Update the cache
            _previousProbeStates = currentProbeStates.Select(p => p).ToList();

            // If probes moved, trigger collision detection
            if (probesMoved)
            {
                ColliderManager.CheckForCollisions();
            }
        }

        private async void OnSettingsStateChanged(SettingsState state)
        {
            EphysLinkConnectionState = state.EphysLinkConnectionState;
            switch (state.EphysLinkConnectionState)
            {
                case EphysLinkConnectionState.Connected:
                    {
                        // Get manipulators from server.
                        var manipulatorsResponse = await _ephysLinkService.GetManipulators();

                        // Cancel if there was an error.
                        if (!string.IsNullOrEmpty(manipulatorsResponse.Error))
                            return;

                        // Get current scene state.
                        var sceneState = _storeService.Store.GetState<SceneState>(
                        SliceNames.SCENE_SLICE
                             );

                        // If there is a mismatch in the IDs in the scene state and the server, create a fresh list of manipulators with default values.
                        if (
                       !sceneState
                            .Manipulators.Select(manipulatorState => manipulatorState.Id)
                .SequenceEqual(manipulatorsResponse.Manipulators)
                   )
                        {
                            var newManipulators = manipulatorsResponse
                             .Manipulators.Select(manipulatorId => new ManipulatorState
                             {
                                 Id = manipulatorId,
                             })
                                  .ToList();
                            _storeService.Store.Dispatch(
                       SceneActions.SET_MANIPULATORS,
                     newManipulators
                           );
                        }
                        break;
                    }
                case EphysLinkConnectionState.Disconnected:
                    _storeService.Store.Dispatch(SceneActions.REMOVE_ALL_VISUALIZATION_PROBES);
                    break;
                case EphysLinkConnectionState.Connecting:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnShuttingDown()
        {
            _sceneStateSubscription.Dispose();
            _settingsStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }

        #region Commands

        [ICommand]
        private void AddProbe(ProbeType probeType)
        {
            _storeService.Store.Dispatch(SceneActions.ADD_PROBE, probeType);
        }

        [ICommand]
        private void SetActiveProbe(int index)
        {
            var selectedProbeName = index < 0 ? "" : ProbeListItemViewModels[index].Name;
            _storeService.Store.Dispatch(SceneActions.SET_ACTIVE_PROBE, selectedProbeName);
        }

        [ICommand]
        private void SetActiveManipulator(int index)
        {
            var selectedManipulatorId = index < 0 ? "" : ManipulatorIds[index];
            _storeService.Store.Dispatch(
         SceneActions.SET_ACTIVE_MANIPULATOR,
              selectedManipulatorId
                 );
        }

        #endregion
    }
}
