using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using Models.Automation;
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
        private readonly IDisposableSubscription _ephysLinkStateSubscription;

        private readonly EphysLinkService _ephysLinkService;

        #endregion

        #region Properties

        [ObservableProperty]
        private List<ManipulatorListItemViewModel> _manipulatorListItemViewModels = new();

        #endregion

        public SceneViewModel(StoreService storeService, EphysLinkService ephysLinkService)
        {
            _storeService = storeService;
            _ephysLinkService = ephysLinkService;

            // Subscribe to state changes and initialize properties.
            _ephysLinkStateSubscription = storeService.Store.Subscribe(
                state => state.Get<EphysLinkState>(SliceNames.EPHYS_LINK_SLICE),
                OnEphysLinkStateChanged,
                new SubscribeOptions<EphysLinkState> { fireImmediately = true }
            );

            _storeService.Store.GetState<EphysLinkState>(SliceNames.EPHYS_LINK_SLICE);
        }

        private async void OnEphysLinkStateChanged(EphysLinkState state)
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
    }
}
