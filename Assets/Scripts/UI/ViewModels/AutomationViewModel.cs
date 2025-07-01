using UI.Models.Automation;
using UI.Services;
using UI.Utils;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class AutomationViewModel
    {
        #region Services

        private readonly IStoreService _storeService;
        private readonly IDisposableSubscription _automationStateSubscription;
        private readonly IDisposableSubscription _probeAutomationStateSubscription;
        private readonly IProbeService _probeService;

        #endregion
        #region Properties

        [ObservableProperty]
        private bool _isAutomationEnabled;

        [ObservableProperty]
        private Vector4 _referenceCoordinate;

        #endregion

        public AutomationViewModel(IStoreService storeService, IProbeService probeService)
        {
            // Register services.
            _storeService = storeService;
            _probeService = probeService;

            // Initialize properties from the store.
            var initialAutomationState = _storeService.Store.GetState<AutomationState>(
                SliceNames.AUTOMATION_SLICE
            );
            OnAutomationStateChanged(initialAutomationState);
            OnExternalPropertiesChanged();

            // Subscribe to state changes.
            _automationStateSubscription = _storeService.Store.Subscribe(
                state => state.Get<AutomationState>(SliceNames.AUTOMATION_SLICE),
                OnAutomationStateChanged
            );
            probeService.OnPropertyChanged += OnExternalPropertiesChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnAutomationStateChanged(AutomationState state)
        {
            // Check if an active manipulator probe is selected.
            _isAutomationEnabled = state.ActiveProbeIndex > -1;

            // Exit if not enabled.
            if (!_isAutomationEnabled)
            {
                return;
            }
        }

        private void OnExternalPropertiesChanged()
        {
            // Update the reference coordinate from the automation state.
            ReferenceCoordinate = _probeService.ActiveProbeReferenceCoordinate;
        }

        private void OnShuttingDown()
        {
            _probeService.OnPropertyChanged -= OnExternalPropertiesChanged;
            App.shuttingDown -= OnShuttingDown;
            _automationStateSubscription.Dispose();
            _probeService.Dispose();
        }
    }
}
