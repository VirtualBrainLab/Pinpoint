using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using Models.Scene;
using Models.Settings;
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
        private List<ManipulatorListItemViewModel> _manipulatorListItemViewModels = new();

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
            // Map probes to view models.
            ProbeListItemViewModels = state
                .Probes.Select(probeState => new ProbeListItemViewModel(probeState, _storeService))
                .ToList();

            // Update panel selection based on active probe.
            SelectedProbeIndex = state.ActiveProbeIndex;
        }

        private async void OnSettingsStateChanged(SettingsState state)
        {
            switch (state.ConnectionState)
            {
                case EphysLinkConnectionState.Connected:
                {
                    // Get manipulators from server.
                    var manipulatorsResponse = await _ephysLinkService.GetManipulators();

                    // Cancel if there was an error.
                    if (!string.IsNullOrEmpty(manipulatorsResponse.Error))
                        return;

                    // Map manipulators to view models.
                    ManipulatorListItemViewModels = manipulatorsResponse
                        .Manipulators.Select(manipulatorId => new ManipulatorListItemViewModel(
                            manipulatorId
                        ))
                        .ToList();
                    break;
                }
                case EphysLinkConnectionState.Disconnected:
                    ManipulatorListItemViewModels = new List<ManipulatorListItemViewModel>();
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
            var selectedProbeName =
                index < 0
                    ? ""
                    : _storeService
                        .Store.GetState<SceneState>(SliceNames.SCENE_SLICE)
                        .Probes[index]
                        .Name;
            _storeService.Store.Dispatch(SceneActions.SET_ACTIVE_PROBE, selectedProbeName);
        }
        #endregion
    }
}
