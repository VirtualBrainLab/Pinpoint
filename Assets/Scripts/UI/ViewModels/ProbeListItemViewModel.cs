using System.Linq;
using Models;
using Models.Scene;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using Utils.Types;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class ProbeListItemViewModel
    {
        private readonly StoreService _storeService;

        #region Properties

        private readonly ProbeState _probeState;

        [ObservableProperty]
        private bool _isVisualizationProbe;

        [ObservableProperty]
        private ProbeColor _color;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private bool _hidden;

        #endregion

        public ProbeListItemViewModel(ProbeState probeState, StoreService storeService)
        {
            _storeService = storeService;
            _probeState = probeState;

            IsVisualizationProbe = storeService
                .Store.GetState<SceneState>(SliceNames.SCENE_SLICE)
                .Manipulators.Exists(state => state.VisualizationProbeName == probeState.Name);
            Color = _probeState.Color;
            Name = _probeState.Name;
            Hidden = _probeState.ProbeDisplayType == ProbeDisplayType.Line;
        }

        #region Commands

        [ICommand]
        private void RemoveProbe()
        {
            _storeService.Store.Dispatch(SceneActions.REMOVE_PROBE, _probeState.Name);
        }

        #endregion
    }
}
