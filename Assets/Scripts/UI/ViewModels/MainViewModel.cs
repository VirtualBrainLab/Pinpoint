using System.ComponentModel;
using Services;
using UI.Models;
using UI.Utils;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class MainViewModel
    {
        #region Services

        private readonly IStoreService _storeService;
        private readonly IDisposableSubscription _subscription;
        private readonly IProbeService _probeService;

        #endregion

        #region Properties

        [ObservableProperty]
        private MainMode _mainMode;

        [ObservableProperty]
        private bool _isLeftSidePanelOpen;

        [ObservableProperty]
        private bool _isRightSidePanelOpen;

        [ObservableProperty]
        private Color _activeProbeColor;

        [ObservableProperty]
        private string _activeProbeName;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// Registers state, initializes properties from the store, and subscribes to state changes.
        /// </summary>
        /// <param name="storeService">The store service for state management.</param>
        /// <param name="probeService">The probe service for getting probe info.</param>
        public MainViewModel(IStoreService storeService, IProbeService probeService)
        {
            // Register services.
            _storeService = storeService;
            _probeService = probeService;

            // Initialize properties from the store.
            var initialState = _storeService.Store.GetState<MainState>(SliceNames.MAIN_SLICE);
            OnStateChanged(initialState);
            OnExternalPropertiesChanged();

            // Subscribe to state changes.
            _subscription = _storeService.Store.Subscribe(
                state => state.Get<MainState>(SliceNames.MAIN_SLICE),
                OnStateChanged
            );
            probeService.OnPropertyChanged += OnExternalPropertiesChanged;
            PropertyChanged += OnPropertyChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnStateChanged(MainState state)
        {
            MainMode = state.MainMode;
            IsLeftSidePanelOpen = state.IsLeftSidePanelOpen;
            IsRightSidePanelOpen = state.IsRightSidePanelOpen;
        }

        private void OnExternalPropertiesChanged()
        {
            ActiveProbeColor = _probeService.ActiveProbeColor;
            ActiveProbeName = _probeService.ActiveProbeName;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainMode):
                    _storeService.Store.Dispatch(MainActions.SET_MODE, MainMode);
                    break;
            }
        }

        private void OnShuttingDown()
        {
            _storeService.Save();
            _probeService.OnPropertyChanged -= OnExternalPropertiesChanged;
            App.shuttingDown -= OnShuttingDown;
            _subscription.Dispose();
            _probeService.Dispose();
        }

        #region Commands

        [ICommand]
        private void ToggleLeftSidePanel()
        {
            _storeService.Store.Dispatch(MainActions.TOGGLE_LEFT_SIDE_PANEL);
        }

        [ICommand]
        private void ToggleRightSidePanel()
        {
            _storeService.Store.Dispatch(MainActions.TOGGLE_RIGHT_SIDE_PANEL);
        }

        #endregion
    }
}
