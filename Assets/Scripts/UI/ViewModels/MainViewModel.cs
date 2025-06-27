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

        #region Converted

        [CreateProperty(ReadOnly = true)]
        public DisplayStyle IsSidePanelLeftItemVisible =>
            IsSidePanelLeftOpen ? DisplayStyle.Flex : DisplayStyle.None;

        [CreateProperty(ReadOnly = true)]
        public string SidePanelLeftToggleText => IsSidePanelLeftOpen ? "◀" : "▶";

        #endregion

        #endregion

        public MainViewModel(IStoreService storeService)
        {
            // Register state.
            _storeService = storeService;

            _isSidePanelLeftOpen = _storeService
                .Store.GetState<MainState>(SliceNames.MAIN_SLICE)
                .IsSidePanelLeftOpen;

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
        }

        #region Commands

        [ICommand]
        private void ToggleSidePanelLeft()
        {
            _storeService.Store.Dispatch(MainActions.TOGGLE_SIDE_PANEL_LEFT);
        }

        #endregion
    }
}
