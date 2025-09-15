using System;
using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine.UIElements;
using Utils;
using Utils.Types;
using Button = Unity.AppUI.UI.Button;
using FloatField = Unity.AppUI.UI.FloatField;
using Vector3Field = Unity.AppUI.UI.Vector3Field;
#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace UI.Views
{
    public class ProbeInspectorView
    {
        public ProbeInspectorView(ProbeInspectorViewModel probeInspectorViewModel)
        {
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>("probe-inspector-view");

            // Register view model and property changes.
            root.dataSource = probeInspectorViewModel;

            // Register component references.
            var positionField = root.Q<Vector3Field>("probe-inspector__position-field");
            var anglesField = root.Q<Vector3Field>("probe-inspector__angles-field");

            var lockButton = root.Q<ActionButton>("probe-inspector__lock-button");
            var duplicateButton = root.Q<ActionButton>("probe-inspector__duplicate-button");
            var moveToReferenceCoordinateButton = root.Q<ActionButton>(
                "probe-inspector__move-to-reference-coordinate-button"
            );
            var moveToDuraButton = root.Q<ActionButton>("probe-inspector__move-to-dura-button");
            var inspectManipulatorButton = root.Q<Button>(
                "probe-inspector__inspect-manipulator-button"
            );

            var probeColorButtons = root.Q<VisualElement>("probe-inspector__probe-color-buttons");

            // Register event handlers.
            positionField.RegisterValueChangedCallback(evt =>
            {
                probeInspectorViewModel.SetPositionCommand.Execute(evt.newValue);
            });
            anglesField.RegisterValueChangedCallback(evt =>
            {
                probeInspectorViewModel.SetAnglesCommand.Execute(evt.newValue);
            });
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
            inspectManipulatorButton.clickable.clicked += probeInspectorViewModel
                .InspectVisualizingManipulatorCommand
                .Execute;
            for (var i = 0; i < probeColorButtons.childCount; i++)
            {
                var indexToProbeColor = (ProbeColor)i;
                probeColorButtons.Query<Button>().AtIndex(i).clickable.clicked += () =>
                    probeInspectorViewModel.SetProbeColorCommand.Execute(indexToProbeColor);
            }

            // Apply view customizations.
            positionField.Q<FloatField>("appui-vector3field__x-field").unit = "AP";
            positionField.Q<FloatField>("appui-vector3field__y-field").unit = "ML";
            positionField.Q<FloatField>("appui-vector3field__z-field").unit = "DV";

            anglesField.Q<FloatField>("appui-vector3field__x-field").unit = "Yaw";
            anglesField.Q<FloatField>("appui-vector3field__y-field").unit = "Pitch";
            anglesField.Q<FloatField>("appui-vector3field__z-field").unit = "Roll";
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

            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "LockedToEnabled",
                (ref bool locked) => !locked
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "VisualizingManipulatorIdToPositionOrientationEnabled",
                (ref string visualizingManipulatorId) =>
                    string.IsNullOrEmpty(visualizingManipulatorId)
            );
            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                string,
                StyleEnum<DisplayStyle>
            >(
                "VisualizingManipulatorIdToProbeControlsVisibility",
                (ref string visualizingManipulatorId) =>
                    string.IsNullOrEmpty(visualizingManipulatorId)
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );
            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                string,
                StyleEnum<DisplayStyle>
            >(
                "VisualizingManipulatorIdToInspectManipulatorButtonVisibility",
                (ref string visualizingManipulatorId) =>
                    string.IsNullOrEmpty(visualizingManipulatorId)
                        ? DisplayStyle.None
                        : DisplayStyle.Flex
            );
        }
    }
}
