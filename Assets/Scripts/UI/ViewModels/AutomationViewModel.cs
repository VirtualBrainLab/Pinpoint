using System;
using System.Numerics;
using UI.Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class AutomationViewModel
    {
        #region Services

        private readonly IStoreService _storeService;
        private readonly IDisposableSubscription _subscription;
        private readonly IProbeService _probeService;

        #endregion
        #region Properties

        [ObservableProperty]
        private Vector4 _referenceCoordinate;

        #endregion

        public AutomationViewModel(IStoreService storeService, IProbeService probeService)
        {
            // Register services.
            _storeService = storeService;
            _probeService = probeService;

            // Initialize properties from the store.

            // Subscribe to state changes.
            App.shuttingDown += OnShuttingDown;
        }

        private void OnShuttingDown()
        {
            App.shuttingDown -= OnShuttingDown;
            _subscription.Dispose();
        }
    }
}
