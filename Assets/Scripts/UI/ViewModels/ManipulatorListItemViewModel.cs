using System.Collections.Generic;
using Models.Scene;
using Unity.AppUI.MVVM;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class ManipulatorListItemViewModel
    {
        #region Properties

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private List<ProbeState> _visualizationProbeOptions;

        [ObservableProperty]
        private int _selectedProbeIndex;

        [ObservableProperty]
        private bool _isLeftHanded;

        #endregion

        public ManipulatorListItemViewModel(string name)
        {
            _name = name;
        }
    }
}
