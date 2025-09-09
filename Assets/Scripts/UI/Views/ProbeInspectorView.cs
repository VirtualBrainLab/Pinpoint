using System;
using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;
using Utils.Types;
using Button = Unity.AppUI.UI.Button;
using FloatField = Unity.AppUI.UI.FloatField;
using Vector3Field = Unity.AppUI.UI.Vector3Field;
using Vector4Field = Unity.AppUI.UI.Vector4Field;

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

            var lockButton = root.Q<ActionButton>("probe-inspector__lock-button");
            var duplicateButton = root.Q<ActionButton>("probe-inspector__duplicate-button");
            var moveToReferenceCoordinateButton = root.Q<ActionButton>(
                "probe-inspector__move-to-reference-coordinate-button"
            );
            var moveToDuraButton = root.Q<ActionButton>("probe-inspector__move-to-dura-button");

            var probeColorButtons = root.Q<VisualElement>("probe-inspector__probe-color-buttons");

            // Register event handlers.
            lockButton.clickable.clicked += probeInspectorViewModel.LockProbeCommand.Execute;
            duplicateButton.clickable.clicked += probeInspectorViewModel
                .DuplicateProbeCommand
                .Execute;
            moveToReferenceCoordinateButton.clickable.clicked += probeInspectorViewModel
                .MoveProbeToReferenceCoordinateCommand
                .Execute;
            moveToDuraButton.clickable.clicked += probeInspectorViewModel
                .MoveProbeToDuraCommand
                .Execute;
            for (var i = 0; i < probeColorButtons.childCount; i++)
            {
                var indexToProbeColor = (ProbeColor)i;
                probeColorButtons.Query<Button>().AtIndex(i).clickable.clicked += () =>
                probeInspectorViewModel.SetProbeColorCommand.Execute(indexToProbeColor);
            }

            // Apply view customizations.
            positionField.Q<FloatField>("appui-vector4field__x-field").unit = "AP";
            positionField.Q<FloatField>("appui-vector4field__y-field").unit = "ML";
            positionField.Q<FloatField>("appui-vector4field__z-field").unit = "DV";
            positionField.Q<FloatField>("appui-vector4field__w-field").unit = "Depth";

            angleField.Q<FloatField>("appui-vector3field__x-field").unit = "Yaw";
            angleField.Q<FloatField>("appui-vector3field__y-field").unit = "Pitch";
            angleField.Q<FloatField>("appui-vector3field__z-field").unit = "Roll";
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void RegisterProbeInspectorViewConverters()
        {
            foreach (ProbeColor color in Enum.GetValues(typeof(ProbeColor)))
            {
                var converterName = $"ProbeColorTo{color}ButtonIcon";
                DataTypeConverters.RegisterUnidirectionalConverterGroup(
                    converterName,
                    (ref ProbeColor probeColor) => probeColor == color ? "check" : ""
                );
            }
        }
    }
}
