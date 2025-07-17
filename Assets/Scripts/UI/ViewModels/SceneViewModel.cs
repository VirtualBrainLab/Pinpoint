using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using Models.Automation;
using Models.Scene;
using Services;
using UI.Utils;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class SceneViewModel
    {
        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;
        private readonly IDisposableSubscription _ephysLinkStateSubscription;

        private readonly EphysLinkService _ephysLinkService;

        #endregion
        #region Properties

        [ObservableProperty]
        private List<ProbeListItemViewModel> _probeListItemViewModels = new();

        [ObservableProperty]
        private List<ManipulatorListItemViewModel> _manipulatorListItemViewModels = new();

        #endregion

        public SceneViewModel(StoreService storeService, EphysLinkService ephysLinkService)
        {
            _storeService = storeService;
            _ephysLinkService = ephysLinkService;

            // Initialize properties.
            var initialSceneState = _storeService.Store.GetState<SceneState>(
                SliceNames.SCENE_SLICE
            );
            var initialEphysLinkState = _storeService.Store.GetState<EphysLinkState>(
                SliceNames.EPHYS_LINK_SLICE
            );
            OnSceneStateChanged(initialSceneState);
            OnEphysLinkStateChanged(initialEphysLinkState);

            // Subscribe to state changes.
            _sceneStateSubscription = storeService.Store.Subscribe(
                state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                OnSceneStateChanged
            );
            _ephysLinkStateSubscription = storeService.Store.Subscribe(
                state => state.Get<EphysLinkState>(SliceNames.EPHYS_LINK_SLICE),
                OnEphysLinkStateChanged
            );

            App.shuttingDown += OnShuttingDown;
        }

        private void OnSceneStateChanged(SceneState state)
        {
            // Map probes to view models.
            ProbeListItemViewModels = state
                .Probes.Select(probe => new ProbeListItemViewModel(
                    PinpointColor.DarkBlue,
                    probe.UUID[..8],
                    false
                ))
                .ToList();
        }

        private async void OnEphysLinkStateChanged(EphysLinkState state)
        {
            switch (state.ConnectionState)
            {
                case ConnectionState.Connected:
                {
                    // Get manipulators from server.
                    var manipulatorsResponse = await _ephysLinkService.GetManipulators();

                    // Cancel if there was an error.
                    if (!string.IsNullOrEmpty(manipulatorsResponse.Error))
                    {
                        return;
                    }

                    // Map manipulators to view models.
                    ManipulatorListItemViewModels = manipulatorsResponse
                        .Manipulators.Select(manipulatorId => new ManipulatorListItemViewModel(
                            manipulatorId
                        ))
                        .ToList();
                    break;
                }
                case ConnectionState.Disconnected:
                    ManipulatorListItemViewModels = new List<ManipulatorListItemViewModel>();
                    break;
                case ConnectionState.Connecting:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnShuttingDown()
        {
            _sceneStateSubscription.Dispose();
            _ephysLinkStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }
    }
}
