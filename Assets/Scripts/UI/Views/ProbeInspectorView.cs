using UI.ViewModels;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;
using Vector4Field = Unity.AppUI.UI.Vector4Field;
using Vector3Field = Unity.AppUI.UI.Vector3Field;
using FloatField = Unity.AppUI.UI.FloatField;

namespace UI.Views
{
    public class ProbeInspectorView
    {
        public ProbeInspectorView(
            TemplateContainer root,
            ProbeInspectorViewModel probeInspectorViewModel
        )
        {
            // Register view model and property changes.
            root.dataSource = probeInspectorViewModel;

            // Register component references.
            var positionField = root.Q<Vector4Field>("probe-inspector__position-field");
            var angleField = root.Q<Vector3Field>("probe-inspector__angle-field");

            var lockButton = root.Q<Button>("probe-inspector__lock-button");
            var duplicateButton = root.Q<Button>("probe-inspector__duplicate-button");
            var moveToReferenceCoordinateButton = root.Q<Button>(
                "probe-inspector__move-to-reference-coordinate-button"
            );
            var moveToDuraButton = root.Q<Button>("probe-inspector__move-to-dura-button");

            // Register event handlers.

            // Apply view customizations.
            positionField.Q<FloatField>("appui-vector4field__x-field").unit = "AP";
            positionField.Q<FloatField>("appui-vector4field__y-field").unit = "ML";
            positionField.Q<FloatField>("appui-vector4field__z-field").unit = "DV";
            positionField.Q<FloatField>("appui-vector4field__w-field").unit = "Depth";
            
            angleField.Q<FloatField>("appui-vector3field__x-field").unit = "Yaw";
            angleField.Q<FloatField>("appui-vector3field__y-field").unit = "Pitch";
            angleField.Q<FloatField>("appui-vector3field__z-field").unit = "Roll";
        }
    }
}
