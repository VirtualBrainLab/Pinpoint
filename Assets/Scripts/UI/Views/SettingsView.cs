using UI.ViewModels;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class SettingsView
{
        public SettingsView(SettingsViewModel settingsViewModel)
        {
    PinpointApp.RootVisualElement.Q<TemplateContainer>("settings-view").dataSource =
   settingsViewModel;

    _ = PinpointApp.Services.GetRequiredService<ProbeView>();
          _ = PinpointApp.Services.GetRequiredService<EphysLinkView>();
         _ = PinpointApp.Services.GetRequiredService<RigView>();
         _ = PinpointApp.Services.GetRequiredService<AtlasSettingsView>();
         _ = PinpointApp.Services.GetRequiredService<GraphicsSettingsView>();

         var probeInspectorRoot = PinpointApp.RootVisualElement.Q<TemplateContainer>("probe-inspector-view");
    var zoomInButton = probeInspectorRoot?.Q<IconButton>("zoom-in-button");
            var zoomOutButton = probeInspectorRoot?.Q<IconButton>("zoom-out-button");

    if (zoomInButton != null)
         zoomInButton.clickable.clicked += settingsViewModel.ZoomInCommand.Execute;

            if (zoomOutButton != null)
         zoomOutButton.clickable.clicked += settingsViewModel.ZoomOutCommand.Execute;
        }
    }
}
