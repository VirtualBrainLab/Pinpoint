using UI.ViewModels;
using Unity.AppUI.MVVM;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class SettingsView
    {
        public SettingsView(SettingsViewModel settingsViewModel)
        {
            PinpointApp.RootVisualElement.Q<TemplateContainer>("settings-view").dataSource =
                settingsViewModel;

            // Initialize subviews.
            _ = PinpointApp.Services.GetRequiredService<ProbeView>();
            _ = PinpointApp.Services.GetRequiredService<EphysLinkView>();
            _ = PinpointApp.Services.GetRequiredService<RigView>();
        }
    }
}
