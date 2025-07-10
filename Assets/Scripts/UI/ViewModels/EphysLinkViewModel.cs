using System;
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

        [ObservableProperty]
        private int _newScalePathfinderMpmPort;

        [ObservableProperty]
        private string _customServerIpAddress;

        [ObservableProperty]
        private int _customServerPort;

        [ObservableProperty]
        private ConnectionState _connectionState;

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
            NewScalePathfinderMpmPort = ephysLinkState.NewScalePathfinderMpmPort;
            CustomServerIpAddress = ephysLinkState.CustomServerIpAddress;
            CustomServerPort = ephysLinkState.CustomServerPort;
            ConnectionState = ephysLinkState.ConnectionState;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(NewScalePathfinderMpmPort):
                    _storeService.Store.Dispatch(
                        EphysLinkActions.SET_NEW_SCALE_PATHFINDER_MPM_PORT,
                        NewScalePathfinderMpmPort
                    );
                    break;
                case nameof(CustomServerIpAddress):
                    // Reset to default if the IP address is empty.
                    if (string.IsNullOrWhiteSpace(CustomServerIpAddress))
                    {
                        CustomServerIpAddress = "localhost";
                    }
                    _storeService.Store.Dispatch(
                        EphysLinkActions.SET_CUSTOM_SERVER_IP_ADDRESS,
                        CustomServerIpAddress
                    );
                    break;
                case nameof(CustomServerPort):
                    _storeService.Store.Dispatch(
                        EphysLinkActions.SET_CUSTOM_SERVER_PORT,
                        CustomServerPort
                    );
                    break;
            }
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

        [ICommand]
        private void Connect()
        {
            switch (SelectedPlatformType)
            {
                case PlatformType.SensapexUmp:
                    break;
                case PlatformType.NewScalePathfinderMpm:
                    break;
                case PlatformType.Custom:
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}
