using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using Utils.Types;

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

        [ObservableProperty]
        private Vector4 _position;

        [ObservableProperty]
        private Vector3 _angles;

        [ObservableProperty]
        private ProbeColor _probeColor;

        #endregion

        public ProbeInspectorViewModel(StoreService storeService)
        {
            // Register services.
            _storeService = storeService;
        }
    }
}
