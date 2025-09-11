using Services;
using Unity.AppUI.MVVM;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class SettingsViewModel
    {
        #region Services

        private readonly StoreService _storeService;

        #endregion

        #region Properties

        [ObservableProperty]
        private int _tabIndex;

        #endregion
    }
}
