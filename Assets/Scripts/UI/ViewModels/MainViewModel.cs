using UI.Models;
using UI.Services;
using UI.Utils;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using Unity.Properties;
using UnityEngine.UIElements;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class MainViewModel
    {
        #region Services

        private readonly IStoreService _storeService;
        private readonly IDisposableSubscription _subscription;

        #endregion

        #region Properties

        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(IsSidePanelLeftItemVisible))]
        [AlsoNotifyChangeFor(nameof(SidePanelLeftToggleText))]
        private bool _isSidePanelLeftOpen;

        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(IsSidePanelRightItemVisible))]
        [AlsoNotifyChangeFor(nameof(SidePanelRightToggleText))]
        private bool _isSidePanelRightOpen;

        #region Converted

        [CreateProperty(ReadOnly = true)]
        public DisplayStyle IsSidePanelLeftItemVisible =>
            IsSidePanelLeftOpen ? DisplayStyle.Flex : DisplayStyle.None;

        [CreateProperty(ReadOnly = true)]
        public string SidePanelLeftToggleText => IsSidePanelLeftOpen ? "\u25C0" : "\u25B6";

        [CreateProperty(ReadOnly = true)]
        public DisplayStyle IsSidePanelRightItemVisible =>
            IsSidePanelRightOpen ? DisplayStyle.Flex : DisplayStyle.None;

        [CreateProperty(ReadOnly = true)]
        public string SidePanelRightToggleText => IsSidePanelRightOpen ? "\u25B6" : "\u25C0";

        #endregion

        #endregion

        public MainViewModel(IStoreService storeService)
        {
            // Register state.
            _storeService = storeService;

            _isSidePanelLeftOpen = _storeService
                .Store.GetState<MainState>(SliceNames.MAIN_SLICE)
                .IsSidePanelLeftOpen;
            _isSidePanelRightOpen = _storeService
                .Store.GetState<MainState>(SliceNames.MAIN_SLICE)
                .IsSidePanelRightOpen;

            // Subscribe to state changes.
            _subscription = _storeService.Store.Subscribe(
                state => state.Get<MainState>(SliceNames.MAIN_SLICE),
                OnStateChanged
            );
        }

        private void OnStateChanged(MainState state)
        {
            // Update the view model properties based on the state.
            IsSidePanelLeftOpen = state.IsSidePanelLeftOpen;
            IsSidePanelRightOpen = state.IsSidePanelRightOpen;
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
    }
}
