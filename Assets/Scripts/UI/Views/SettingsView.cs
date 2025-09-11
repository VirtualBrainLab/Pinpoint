using UI.ViewModels;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class SettingsView
    {
        public SettingsView(TemplateContainer root, SettingsViewModel settingsViewModel)
        {
            root.dataSource = settingsViewModel;
        }
    }
}
