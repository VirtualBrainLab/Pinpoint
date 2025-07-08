using System.ComponentModel;
using Models;
using Models.Scene;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
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
        private MainMode _mainMode;

        [ObservableProperty]
        private bool _isLeftSidePanelOpen;

        [ObservableProperty]
        private bool _isRightSidePanelOpen;

        [ObservableProperty]
        private int _leftSidePanelTabIndex;

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
        public MainViewModel(StoreService storeService)
        {
            // Register services.
            _storeService = storeService;

            // Initialize properties from the store.
            var initialMainState = _storeService.Store.GetState<MainState>(SliceNames.MAIN_SLICE);
            OnMainStateChanged(initialMainState);
            var initialSceneState = _storeService.Store.GetState<SceneState>(
                SliceNames.SCENE_SLICE
            );
            OnSceneStateChanged(initialSceneState);

            // Subscribe to state changes.
            _mainStateSubscription = _storeService.Store.Subscribe(
                state => state.Get<MainState>(SliceNames.MAIN_SLICE),
                OnMainStateChanged
            );
            _sceneStateSubscription = storeService.Store.Subscribe(
                state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                OnSceneStateChanged
            );
            PropertyChanged += OnPropertyChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnMainStateChanged(MainState state)
        {
            MainMode = state.MainMode;
            IsLeftSidePanelOpen = state.IsLeftSidePanelOpen;
            IsRightSidePanelOpen = state.IsRightSidePanelOpen;
            LeftSidePanelTabIndex = state.LeftSidePanelTabIndex;
        }

        private void OnSceneStateChanged(SceneState state)
        {
            ActiveProbeName = state.ActiveProbeState?.Name ?? "No Active Probe";
            ActiveProbeColor = state.ActiveProbeState?.Color ?? Color.gray;
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
            _mainStateSubscription.Dispose();
            _sceneStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }

        #region Commands

        [ICommand]
        private void ToggleLeftSidePanel()
        {
            Debug.Log("Toggling left side panel");
            _storeService.Store.Dispatch(MainActions.TOGGLE_LEFT_SIDE_PANEL);
        }

        [ICommand]
        private void ToggleRightSidePanel()
        {
            _storeService.Store.Dispatch(MainActions.TOGGLE_RIGHT_SIDE_PANEL);
        }

        [ICommand]
        private void SetLeftSidePanelTabIndex(int index)
        {
            _storeService.Store.Dispatch(MainActions.SET_LEFT_SIDE_PANEL_TAB_INDEX, index);
        }

        #endregion
    }
}
