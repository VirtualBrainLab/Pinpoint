using UI.ViewModels;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class ManipulatorInspectorView
    {
        public ManipulatorInspectorView(ManipulatorInspectorViewModel manipulatorInspectorViewModel)
        {
            // Get root and apply data source.
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>("manipulator-inspector-view");
            root.dataSource = manipulatorInspectorViewModel;
        }
    
    }
}
