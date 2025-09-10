using System.ComponentModel;
using Models;
using Models.Scene;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using Unity.AppUI.UI;
using UnityEngine;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class MainViewModel
    {
        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _mainStateSubscription;
        private readonly IDisposableSubscription _sceneStateSubscription;

        #endregion

        #region Properties

        [ObservableProperty]
        private bool _isAutomationModeActive;

        [ObservableProperty]
        private SplitView.State _mainSplitViewState;

        [ObservableProperty]
        private int _leftSidePanelTabIndex;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// Registers state, initializes properties from the store, and subscribes to state changes.
        /// </summary>
        /// <param name="storeService">The store service for state management.</param>
        /// <param name="probeService">The probe service for getting probe info.</param>
        public MainViewModel(StoreService storeService)
        {
            // Register services.
            _storeService = storeService;

            // Subscribe to state changes and initialize properties.
            _mainStateSubscription = _storeService.Store.Subscribe(
                state => state.Get<MainState>(SliceNames.MAIN_SLICE),
                OnMainStateChanged,
                new SubscribeOptions<MainState> { fireImmediately = true }
            );
            PropertyChanged += OnPropertyChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnMainStateChanged(MainState state)
        {
            IsAutomationModeActive = state.IsAutomationModeActive;
            MainSplitViewState = state.MainSplitViewState;
            LeftSidePanelTabIndex = state.LeftSidePanelTabIndex;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IsAutomationModeActive):
                    _storeService.Store.Dispatch(
                        MainActions.SET_IS_AUTOMATION_MODE_ACTIVE,
                        IsAutomationModeActive
                    );
                    break;
            }
        }

        private void OnShuttingDown()
        {
            _storeService.Save();
            _mainStateSubscription.Dispose();
            _sceneStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }

        #region Commands

        [ICommand]
        private void SetMainSplitViewState(SplitView.State state)
        {
            _storeService.Store.Dispatch(MainActions.SET_MAIN_SPLIT_VIEW_STATE, state);
        }

        [ICommand]
        private void SetLeftSidePanelTabIndex(int index)
        {
            _storeService.Store.Dispatch(MainActions.SET_LEFT_SIDE_PANEL_TAB_INDEX, index);
        }

        #endregion
    }
}
