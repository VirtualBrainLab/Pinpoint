using System;
using System.ComponentModel;
using Models;
using Models.Automation;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using Utils.Types;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class EphysLinkViewModel
    {
        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _ephysLinkStateSubscription;

        [Service]
        private readonly EphysLinkService _ephysLinkService;

        #endregion

        #region Properties

        [ObservableProperty]
        private EphysLinkPlatformType _selectedPlatformType;

        [ObservableProperty]
        private int _newScalePathfinderMpmPort;

        [ObservableProperty]
        private string _customServerIpAddress;

        [ObservableProperty]
        private int _customServerPort;

        [ObservableProperty]
        private EphysLinkConnectionState _ephysLinkConnectionState;

        #endregion

        public EphysLinkViewModel(StoreService storeService)
        {
            // Register services.
            _storeService = storeService;

            // Subscribe to state changes and initialize properties.
            _ephysLinkStateSubscription = storeService.Store.Subscribe(
                state => state.Get<EphysLinkState>(SliceNames.EPHYS_LINK_SLICE),
                OnEphysLinkStateChanged,
                new SubscribeOptions<EphysLinkState> { fireImmediately = true }
            );
            PropertyChanged += OnPropertyChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnEphysLinkStateChanged(EphysLinkState ephysLinkState)
        {
            SelectedPlatformType = ephysLinkState.SelectedEphysLinkPlatformType;
            NewScalePathfinderMpmPort = ephysLinkState.NewScalePathfinderMpmPort;
            CustomServerIpAddress = ephysLinkState.CustomServerIpAddress;
            CustomServerPort = ephysLinkState.CustomServerPort;
            EphysLinkConnectionState = ephysLinkState.ConnectionState;
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
                        CustomServerIpAddress = "localhost";
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
            // Trigger a disconnect.
            _ephysLinkService.Disconnect();

            // Unsubscribe from events and dispose of subscriptions.
            App.shuttingDown -= OnShuttingDown;
            _ephysLinkStateSubscription.Dispose();
        }

        #region Commands

        [ICommand]
        private void SetSelectedPlatformType(EphysLinkPlatformType platformType)
        {
            _storeService.Store.Dispatch(
                EphysLinkActions.SET_SELECTED_EPHYS_LINK_PLATFORM_TYPE,
                platformType
            );
        }

        [ICommand]
        private void Connect()
        {
            // Move to connecting state.
            _storeService.Store.Dispatch(
                EphysLinkActions.SET_CONNECTION_STATE,
                EphysLinkConnectionState.Connecting
            );

            // Get the current state from the store.
            var ephysLinkState = _storeService.Store.GetState<EphysLinkState>(
                SliceNames.EPHYS_LINK_SLICE
            );

            // Connect based on the selected platform type.
            switch (SelectedPlatformType)
            {
                case EphysLinkPlatformType.SensapexUmp:
                case EphysLinkPlatformType.NewScalePathfinderMpm:
                    _ephysLinkService.Launch();
                    ConnectAttempt();
                    break;
                case EphysLinkPlatformType.Custom:
                    _ephysLinkService.ConnectToServer(
                        ephysLinkState.CustomServerIpAddress,
                        ephysLinkState.CustomServerPort,
                        null,
                        errorMessage =>
                        {
                            var alertDialog = new AlertDialog
                            {
                                title = "Failed to Connect to Custom Server",
                                description = errorMessage,
                                variant = AlertSemantic.Error,
                            };
                            alertDialog.SetCancelAction(0, "OK");
                            var presentationModal = Modal.Build(
                                PinpointApp.Current.rootVisualElement,
                                alertDialog
                            );
                            presentationModal.Show();
                        }
                    );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return;

            void ConnectAttempt(int attempts = 0)
            {
                _ephysLinkService.ConnectToServer(
                    "localhost",
                    3000,
                    null,
                    errorMessage =>
                    {
                        if (attempts > 10)
                        {
                            var alertDialog = new AlertDialog
                            {
                                title = "Failed to Connect to Launched Server",
                                description = errorMessage,
                                variant = AlertSemantic.Error,
                            };
                            alertDialog.SetCancelAction(0, "OK");
                            var presentationModal = Modal.Build(
                                PinpointApp.Current.rootVisualElement,
                                alertDialog
                            );
                            presentationModal.Show();
                        }
                        else
                        {
                            // Move back to connecting state.
                            _storeService.Store.Dispatch(
                                EphysLinkActions.SET_CONNECTION_STATE,
                                EphysLinkConnectionState.Connecting
                            );

                            ConnectAttempt(attempts + 1);
                        }
                    }
                );
            }
        }

        [ICommand]
        private void Disconnect()
        {
            var alertDialog = new AlertDialog
            {
                title = "Disconnect from Ephys Link?",
                description = "All incomplete movements will be canceled.",
                variant = AlertSemantic.Destructive,
            };
            alertDialog.SetPrimaryAction(1, "Disconnect", () => _ephysLinkService.Disconnect());
            alertDialog.SetCancelAction(0, "Cancel");
            var presentationModal = Modal.Build(PinpointApp.Current.rootVisualElement, alertDialog);
            presentationModal.Show();
        }

        #endregion
    }
}
