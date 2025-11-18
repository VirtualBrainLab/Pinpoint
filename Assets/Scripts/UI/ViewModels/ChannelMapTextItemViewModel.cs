using Models;
using Models.Scene;
using Services;
using Unity.AppUI.MVVM;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class ChannelMapTextItemViewModel
    {
    private readonly StoreService _storeService;
        private readonly ProbeState _probeState;

        [ObservableProperty]
 private string _name;

        [ObservableProperty]
        private float _positionPerc;

    public ChannelMapTextItemViewModel(ProbeState probeState, StoreService storeService)
        {
         _storeService = storeService;
            _probeState = probeState;
        }
    }
}
