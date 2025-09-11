using UI.ViewModels;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class SettingsView
    {
        public SettingsView(SettingsViewModel settingsViewModel)
        {
            PinpointApp.RootVisualElement.Q<TemplateContainer>("settings-view").dataSource =
                settingsViewModel;
        }
    }
}
