using System.Collections.Generic;
using Unity.AppUI.MVVM;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class SceneViewModel
    {
        #region Properties

        [ObservableProperty]
        private List<ManipulatorListItemViewModel> _manipulatorListItems;

        #endregion
    }
}
