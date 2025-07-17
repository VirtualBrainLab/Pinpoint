using UI.Utils;
using Unity.AppUI.MVVM;
using UnityEngine;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class ProbeListItemViewModel
    {
        #region Properties

        [ObservableProperty]
        private PinpointColor _color;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private bool _hidden;

        #endregion

        public ProbeListItemViewModel(PinpointColor color, string name, bool hidden)
        {
            _color = color;
            _name = name;
            _hidden = hidden;
        }
    }
}
