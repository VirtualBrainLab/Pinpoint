using System.ComponentModel;
using Models;
using Models.Settings;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class SettingsViewModel
    {
        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _settingsStateSubscription;

        #endregion

        #region Properties

        [ObservableProperty]
        private int _tabIndex;

        [ObservableProperty]
        private int _inPlaneZoom;

        #endregion

        public SettingsViewModel(StoreService storeService)
        {
            // Register services.
            _storeService = storeService;

            // Subscribe to settings state changes and initialize properties.
            _settingsStateSubscription = _storeService.Store.Subscribe(
                state => state.Get<SettingsState>(SliceNames.SETTINGS_SLICE),
                OnSettingsStateChanged,
                new SubscribeOptions<SettingsState> { fireImmediately = true }
            );
            PropertyChanged += OnPropertyChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnSettingsStateChanged(SettingsState state)
        {
            TabIndex = state.TabIndex;
            InPlaneZoom = state.inPlaneZoom;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TabIndex):
                    _storeService.Store.Dispatch(SettingsActions.SET_TAB_INDEX, TabIndex);
                    break;
                case nameof(InPlaneZoom):
                    _storeService.Store.Dispatch(SettingsActions.SET_IN_PLANE_ZOOM, InPlaneZoom);
                    break;
            }
        }

        private void OnShuttingDown()
        {
            _settingsStateSubscription.Dispose();
            PropertyChanged -= OnPropertyChanged;
            App.shuttingDown -= OnShuttingDown;
        }

        #region Commands

        [ICommand]
        private void ZoomIn()
        {
            InPlaneZoom++;
        }

        [ICommand]
        private void ZoomOut()
        {
            InPlaneZoom--;
        }

        #endregion
    }
}
