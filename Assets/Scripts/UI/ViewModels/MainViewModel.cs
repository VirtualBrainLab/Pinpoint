using System.ComponentModel;
using System.Timers;
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
        private readonly Timer _externalPropertyTimer;

        #endregion

        #region Properties

        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(InspectorPanelDisplayStyle))]
        [AlsoNotifyChangeFor(nameof(AutomationPanelDisplayStyle))]
        [AlsoNotifyChangeFor(nameof(ManualControlPanelDisplayStyle))]
        private int _modeIndex;

        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(SidePanelLeftToggleText))]
        [AlsoNotifyChangeFor(nameof(SidePanelLeftPickingMode))]
        private DisplayStyle _sidePanelLeftItemDisplayStyle;

        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(SidePanelRightToggleText))]
        [AlsoNotifyChangeFor(nameof(SidePanelRightPickingMode))]
        private DisplayStyle _sidePanelRightItemDisplayStyle;

        [ObservableProperty]
        private Color _activeProbeColor;

        [ObservableProperty]
        private string _activeProbeName;

        #region Converted

        [CreateProperty]
        public PickingMode SidePanelLeftPickingMode =>
            SidePanelVisibleToPickingMode(_sidePanelLeftItemDisplayStyle);

        [CreateProperty]
        public PickingMode SidePanelRightPickingMode =>
            SidePanelVisibleToPickingMode(SidePanelRightItemDisplayStyle);

        [CreateProperty]
        public string SidePanelLeftToggleText =>
            _sidePanelLeftItemDisplayStyle == DisplayStyle.Flex ? "\u25C0" : "\u25B6";

        [CreateProperty]
        public string SidePanelRightToggleText =>
            SidePanelRightItemDisplayStyle == DisplayStyle.Flex ? "\u25B6" : "\u25C0";

        [CreateProperty]
        public DisplayStyle InspectorPanelDisplayStyle =>
            ModeIndex < MainModeToInt(MainModes.Automation) ? DisplayStyle.Flex : DisplayStyle.None;

        [CreateProperty]
        public DisplayStyle AutomationPanelDisplayStyle =>
            ModeIndex > MainModeToInt(MainModes.Visualization)
                ? DisplayStyle.Flex
                : DisplayStyle.None;

        [CreateProperty]
        public DisplayStyle ManualControlPanelDisplayStyle =>
            ModeIndex > MainModeToInt(MainModes.Planning) ? DisplayStyle.Flex : DisplayStyle.None;

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
            _externalPropertyTimer = new Timer(200);
            _externalPropertyTimer.Elapsed += UpdateExternalProperties;
            _externalPropertyTimer.AutoReset = true;
            _externalPropertyTimer.Start();
            App.shuttingDown += OnShuttingDown;
        }

        private void OnStateChanged(MainState state)
        {
            ModeIndex = MainModeToInt(state.Mode);
            SidePanelLeftItemDisplayStyle = SidePanelOpenToDisplayStyle(state.IsSidePanelLeftOpen);
            SidePanelRightItemDisplayStyle = SidePanelOpenToDisplayStyle(state.IsSidePanelRightOpen);
        }

        private void UpdateExternalProperties(object sender, ElapsedEventArgs e)
        {
            ActiveProbeColor = ProbeManager.ActiveProbeManager?.Color ?? Color.gray;
            ActiveProbeName = ProbeManager.ActiveProbeManager?.OverrideName ?? "No Active Probe";
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
            _externalPropertyTimer.Stop();
            _externalPropertyTimer.Dispose();
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
