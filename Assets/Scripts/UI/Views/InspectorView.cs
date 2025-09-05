using UI.ViewModels;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class InspectorView
    {
        public InspectorView(TemplateContainer root, InspectorViewModel inspectorViewModel)
        {
            // Register view model and property changes.
            root.dataSource = inspectorViewModel;
            
        }
    }
}
