using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class ProbeInspectorViewModel
    {
        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;

        #endregion

        #region Properties

        

        #endregion

        public ProbeInspectorViewModel(StoreService storeService)
        {
            // Register services.
            _storeService = storeService;
        }
    }
}
