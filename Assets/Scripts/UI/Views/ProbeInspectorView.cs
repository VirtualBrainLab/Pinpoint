using UnityEngine.UIElements;

namespace UI.Views
{
    public class ProbeInspectorView
    {
        public ProbeInspectorView(TemplateContainer root)
        {
            // Register view model and property changes.
            // root.dataSource = inspectorViewModel;

            // Register component references.
            var positionField = root.Q<Vector4Field>("probe-inspector--position-field");
            var angleField = root.Q<Vector4Field>("probe-inspector--angle-field");
            
            var lockButton = root.Q<Button>("probe-inspector--lock-button");
            var duplicateButton = root.Q<Button>("probe-inspector--duplicate-button");
            var moveToReferenceCoordinateButton = root.Q<Button>("probe-inspector--move-to-reference-coordinate-button");
            var moveToDuraButton = root.Q<Button>("probe-inspector--move-to-dura-button");
        }
    }
}
