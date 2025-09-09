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
            
            // Register event handlers.
            
            // Apply view customizations.
            positionField.Q<FloatField>("appui-vector4field__x-field").label = "AP";
            positionField.Q<FloatField>("appui-vector4field__y-field").label = "ML";
            positionField.Q<FloatField>("appui-vector4field__z-field").label = "DV";
            positionField.Q<FloatField>("appui-vector4field__w-field").label = "Depth";
            
            angleField.Q<FloatField>("appui-vector3field__x-field").label = "Yaw";
            angleField.Q<FloatField>("appui-vector3field__y-field").label = "Pitch";
            angleField.Q<FloatField>("appui-vector3field__z-field").label = "Roll";
        }
    }
}
