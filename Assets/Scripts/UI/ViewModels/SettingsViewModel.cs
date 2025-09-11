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
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TabIndex):
                    _storeService.Store.Dispatch(SettingsActions.SET_TAB_INDEX, TabIndex);
                    break;
            }
        }

        private void OnShuttingDown()
        {
            _settingsStateSubscription.Dispose();
            PropertyChanged -= OnPropertyChanged;
            App.shuttingDown -= OnShuttingDown;
        }
    }
}
