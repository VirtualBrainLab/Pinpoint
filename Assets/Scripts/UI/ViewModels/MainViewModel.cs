using System.ComponentModel;
using UI.Models;
using UI.Services;
using UI.Utils;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace UI.ViewModels
{
    /// <summary>
    /// ViewModel for the main view of the Pinpoint application.
    /// Handles state management, property binding, and commands for UI interaction.
    /// </summary>
    [ObservableObject]
    public partial class MainViewModel
    {
        #region Services

        private readonly IStoreService _storeService;
        private readonly IDisposableSubscription _subscription;

        #endregion

        #region Properties

        [ObservableProperty]
        private int _modeIndex;

        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(SidePanelLeftToggleText))]
        [AlsoNotifyChangeFor(nameof(SidePanelLeftPickingMode))]
        private DisplayStyle _isSidePanelLeftItemVisible;

        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(SidePanelRightToggleText))]
        [AlsoNotifyChangeFor(nameof(SidePanelRightPickingMode))]
        private DisplayStyle _isSidePanelRightItemVisible;

        #region Converted

        [CreateProperty]
        public PickingMode SidePanelLeftPickingMode =>
            SidePanelVisibleToPickingMode(IsSidePanelLeftItemVisible);

        [CreateProperty]
        public PickingMode SidePanelRightPickingMode =>
            SidePanelVisibleToPickingMode(IsSidePanelRightItemVisible);

        [CreateProperty]
        public string SidePanelLeftToggleText =>
            IsSidePanelLeftItemVisible == DisplayStyle.Flex ? "\u25C0" : "\u25B6";

        [CreateProperty]
        public string SidePanelRightToggleText =>
            IsSidePanelRightItemVisible == DisplayStyle.Flex ? "\u25B6" : "\u25C0";

        [CreateProperty]
        public Color ActiveProbeColor => ProbeManager.ActiveProbeManager?.Color ?? Color.gray;

        #endregion

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// Registers state, initializes properties from the store, and subscribes to state changes.
        /// </summary>
        /// <param name="storeService">The store service for state management.</param>
        public MainViewModel(IStoreService storeService)
        {
            // Register state.
            _storeService = storeService;
            var initialState = _storeService.Store.GetState<MainState>(SliceNames.MAIN_SLICE);
            OnStateChanged(initialState);

            // Subscribe to state changes.
            _subscription = _storeService.Store.Subscribe(
                state => state.Get<MainState>(SliceNames.MAIN_SLICE),
                OnStateChanged
            );
            PropertyChanged += OnPropertyChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnStateChanged(MainState state)
        {
            ModeIndex = MainModeToInt(state.Mode);
            IsSidePanelLeftItemVisible = SidePanelOpenToDisplayStyle(state.IsSidePanelLeftOpen);
            IsSidePanelRightItemVisible = SidePanelOpenToDisplayStyle(state.IsSidePanelRightOpen);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ModeIndex):
                    _storeService.Store.Dispatch(MainActions.SET_MODE, ModeIndex);
                    break;
            }
        }

        private void OnShuttingDown()
        {
            _storeService.Save();
            App.shuttingDown -= OnShuttingDown;
            _subscription.Dispose();
        }

        #region Commands

        [ICommand]
        private void ToggleSidePanelLeft()
        {
            _storeService.Store.Dispatch(MainActions.TOGGLE_SIDE_PANEL_LEFT);
        }

        [ICommand]
        private void ToggleSidePanelRight()
        {
            _storeService.Store.Dispatch(MainActions.TOGGLE_SIDE_PANEL_RIGHT);
        }

        #endregion

        #region Converters
        private static DisplayStyle SidePanelOpenToDisplayStyle(bool isOpen) =>
            isOpen ? DisplayStyle.Flex : DisplayStyle.None;

        private static PickingMode SidePanelVisibleToPickingMode(DisplayStyle displayStyle) =>
            displayStyle == DisplayStyle.Flex ? PickingMode.Position : PickingMode.Ignore;

        private static int MainModeToInt(MainModes mode) => (int)mode;
        #endregion
    }
}
