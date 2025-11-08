using System;
using System.ComponentModel;
using Models;
using Models.Settings;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class ProbeViewModel
    {
        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _settingsStateSubscription;

        #endregion

        #region Properties

        [ObservableProperty]
        private bool _detectCollisions;

        [ObservableProperty]
        private bool _convertAPML2Probe;

        [ObservableProperty]
        private string _angleConvention;

        [ObservableProperty]
        private bool _axisControl;

        [ObservableProperty]
        private int _probeSpeed;

        [ObservableProperty]
        private bool _probePrevNextEnabled;

        #endregion

        public ProbeViewModel(StoreService storeService)
        {
            // Register services.
            _storeService = storeService;

            // Subscribe to state changes and initialize properties.
            _settingsStateSubscription = storeService.Store.Subscribe(
                state => state.Get<SettingsState>(SliceNames.SETTINGS_SLICE),
                OnSettingsStateChanged,
                new SubscribeOptions<SettingsState> { fireImmediately = true }
            );
            PropertyChanged += OnPropertyChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnSettingsStateChanged(SettingsState state)
        {
            DetectCollisions = state.DetectCollisions;
            ConvertAPML2Probe = state.ConvertAPML2Probe;
            AngleConvention = state.AngleConvention;
            AxisControl = state.AxisControl;
            ProbeSpeed = state.ProbeSpeed;
            ProbePrevNextEnabled = state.ProbePrevNextEnabled;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DetectCollisions):
                    _storeService.Store.Dispatch(SettingsActions.SET_DETECT_COLLISIONS, DetectCollisions);
                    break;
                case nameof(ConvertAPML2Probe):
                    // TODO: Dispatch action to update state
                    // _storeService.Store.Dispatch(SettingsActions.SET_CONVERT_APML2PROBE, ConvertAPML2Probe);
                    break;
                case nameof(AngleConvention):
                    // TODO: Dispatch action to update state
                    // _storeService.Store.Dispatch(SettingsActions.SET_ANGLE_CONVENTION, AngleConvention);
                    break;
                case nameof(AxisControl):
                    // TODO: Dispatch action to update state
                    // _storeService.Store.Dispatch(SettingsActions.SET_AXIS_CONTROL, AxisControl);
                    break;
                case nameof(ProbeSpeed):
                    // TODO: Dispatch action to update state
                    // _storeService.Store.Dispatch(SettingsActions.SET_PROBE_SPEED, ProbeSpeed);
                    break;
                case nameof(ProbePrevNextEnabled):
                    // TODO: Dispatch action to update state
                    // _storeService.Store.Dispatch(SettingsActions.SET_PROBE_PREV_NEXT_ENABLED, ProbePrevNextEnabled);
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
