using System.ComponentModel;
using Models;
using Models.Automation;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class EphysLinkViewModel
    {
        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _ephysLinkStateSubscription;

        #endregion
        #region Properties

        [ObservableProperty]
        private PlatformType _selectedPlatformType;

        #endregion

        public EphysLinkViewModel(StoreService storeService)
        {
            // Register services.
            _storeService = storeService;

            // Initialize properties from the store.
            var initialEphysLinkState = _storeService.Store.GetState<EphysLinkState>(
                SliceNames.EPHYS_LINK_SLICE
            );
            OnEphysLinkStateChanged(initialEphysLinkState);

            // Subscribe to state changes.
            _ephysLinkStateSubscription = storeService.Store.Subscribe(
                state => state.Get<EphysLinkState>(SliceNames.EPHYS_LINK_SLICE),
                OnEphysLinkStateChanged
            );
            PropertyChanged += OnPropertyChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnEphysLinkStateChanged(EphysLinkState ephysLinkState)
        {
            SelectedPlatformType = ephysLinkState.SelectedPlatformType;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName) { }
        }

        private void OnShuttingDown()
        {
            App.shuttingDown -= OnShuttingDown;
            _ephysLinkStateSubscription.Dispose();
        }

        #region Commands

        [ICommand]
        private void SetSelectedPlatformType(PlatformType platformType)
        {
            _storeService.Store.Dispatch(EphysLinkActions.SET_SELECTED_PLATFORM_TYPE, platformType);
        }

        #endregion
    }
}
