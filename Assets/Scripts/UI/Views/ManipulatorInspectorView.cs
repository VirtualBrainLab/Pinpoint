using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine.UIElements;
using Utils;
using Utils.Types;
using Button = Unity.AppUI.UI.Button;
using FloatField = Unity.AppUI.UI.FloatField;
using MenuItem = Unity.AppUI.UI.MenuItem;
using Toggle = Unity.AppUI.UI.Toggle;
using Vector3Field = Unity.AppUI.UI.Vector3Field;
using Vector4Field = Unity.AppUI.UI.Vector4Field;
#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace UI.Views
{
    public class ManipulatorInspectorView
    {
        public ManipulatorInspectorView(ManipulatorInspectorViewModel manipulatorInspectorViewModel)
        {
            // Get root and apply data source.
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>(
                "manipulator-inspector-view"
            );
            root.dataSource = manipulatorInspectorViewModel;

            // Register component references.
            var addNeuropixels10 = root.Q<MenuItem>(
                "manipulator-inspector__add-visualization-probe-menu__neuropixels__1-0"
            );
            var addNeuropixels20 = root.Q<MenuItem>(
                "manipulator-inspector__add-visualization-probe-menu__neuropixels__2-0"
            );
            var addNeuropixels204Shank = root.Q<MenuItem>(
                "manipulator-inspector__add-visualization-probe-menu__neuropixels__2-0-4-shank"
            );
            var addNeuropixels2X24 = root.Q<MenuItem>(
                "manipulator-inspector__add-visualization-probe-menu__neuropixels__2x-2-4"
            );
            var addPipette25Um = root.Q<MenuItem>(
                "manipulator-inspector__add-visualization-probe-menu__pipette__25um"
            );
            var addPipette50Um = root.Q<MenuItem>(
                "manipulator-inspector__add-visualization-probe-menu__pipette__50um"
            );
            var addPipette100Um = root.Q<MenuItem>(
                "manipulator-inspector__add-visualization-probe-menu__pipette__100um"
            );
            var addPipette200Um = root.Q<MenuItem>(
                "manipulator-inspector__add-visualization-probe-menu__pipette__200um"
            );
            var addUcla128K = root.Q<MenuItem>(
                "manipulator-inspector__add-visualization-probe-menu__ucla__128k"
            );
            var addUcla256F = root.Q<MenuItem>(
                "manipulator-inspector__add-visualization-probe-menu__ucla__256f"
            );
            var visualizationProbeInspectButton = root.Q<Button>(
                "manipulator-inspector__visualization-probe-inspect-button"
            );
            var anglesField = root.Q<Vector3Field>("manipulator-inspector__angles-field");
            var handednessButtonLeft = root.Q<ActionButton>(
                "manipulator-inspector__handedness-button--left"
            );
            var handednessButtonRight = root.Q<ActionButton>(
                "manipulator-inspector__handedness-button--right"
            );
            var referenceCoordinateOffsetField = root.Q<Vector4Field>(
                "manipulator-inspector__reference-coordinate-offset-field"
            );
            var setReferenceCoordinateOffsetButton = root.Q<Button>(
                "manipulator-inspector__set-reference-coordinate-offset-button"
            );
            var duraOffsetField = root.Q<FloatField>("manipulator-inspector__dura-offset-field");
            var recalculateDuraOffsetButton = root.Q<Button>(
                "manipulator-inspector__recalculate-dura-offset-button"
            );
            var manualControlToggle = root.Q<Toggle>(
                "manipulator-inspector__manual-control-toggle"
            );

            // Register event handlers.
            addNeuropixels10.clickable.clicked += () =>
                manipulatorInspectorViewModel.AddVisualizationProbeCommand.Execute(
                    ProbeType.Neuropixels1
                );
            addNeuropixels20.clickable.clicked += () =>
                manipulatorInspectorViewModel.AddVisualizationProbeCommand.Execute(
                    ProbeType.Neuropixels21
                );
            addNeuropixels204Shank.clickable.clicked += () =>
                manipulatorInspectorViewModel.AddVisualizationProbeCommand.Execute(
                    ProbeType.Neuropixels24
                );
            addNeuropixels2X24.clickable.clicked += () =>
                manipulatorInspectorViewModel.AddVisualizationProbeCommand.Execute(
                    ProbeType.Neuropixels24x2
                );
            addPipette25Um.clickable.clicked += () =>
                manipulatorInspectorViewModel.AddVisualizationProbeCommand.Execute(
                    ProbeType.Pipette25
                );
            addPipette50Um.clickable.clicked += () =>
                manipulatorInspectorViewModel.AddVisualizationProbeCommand.Execute(
                    ProbeType.Pipette50
                );
            addPipette100Um.clickable.clicked += () =>
                manipulatorInspectorViewModel.AddVisualizationProbeCommand.Execute(
                    ProbeType.Pipette100
                );
            addPipette200Um.clickable.clicked += () =>
                manipulatorInspectorViewModel.AddVisualizationProbeCommand.Execute(
                    ProbeType.Pipette200
                );
            addUcla128K.clickable.clicked += () =>
                manipulatorInspectorViewModel.AddVisualizationProbeCommand.Execute(
                    ProbeType.UCLA128K
                );
            addUcla256F.clickable.clicked += () =>
                manipulatorInspectorViewModel.AddVisualizationProbeCommand.Execute(
                    ProbeType.UCLA256F
                );
            visualizationProbeInspectButton.clickable.clicked += manipulatorInspectorViewModel
                .InspectVisualizationProbeCommand
                .Execute;
            anglesField.RegisterValueChangedCallback(evt =>
            {
                manipulatorInspectorViewModel.SetAnglesCommand.Execute(evt.newValue);
            });
            handednessButtonLeft.clickable.clicked += () =>
            {
                manipulatorInspectorViewModel.SetHandednessCommand.Execute(
                    ManipulatorHandedness.Left
                );
            };
            handednessButtonRight.clickable.clicked += () =>
            {
                manipulatorInspectorViewModel.SetHandednessCommand.Execute(
                    ManipulatorHandedness.Right
                );
            };
            referenceCoordinateOffsetField.RegisterValueChangedCallback(evt =>
            {
                manipulatorInspectorViewModel.SetReferenceCoordinateOffsetCommand.Execute(
                    evt.newValue
                );
            });
            setReferenceCoordinateOffsetButton.clickable.clicked += () =>
            {
                manipulatorInspectorViewModel.UseCurrentPositionForReferenceCoordinateOffsetCommand.Execute();
            };
            duraOffsetField.RegisterValueChangedCallback(evt =>
            {
                manipulatorInspectorViewModel.SetDuraOffsetCommand.Execute(evt.newValue);
            });
            recalculateDuraOffsetButton.clickable.clicked += () =>
            {
                manipulatorInspectorViewModel.RecalculateDuraOffsetCommand.Execute();
            };
            manualControlToggle.RegisterValueChangedCallback(evt =>
            {
                manipulatorInspectorViewModel.SetManualControlEnabledCommand.Execute(evt.newValue);
            });

            // Customize field units.
            anglesField.Q<FloatField>("appui-vector3field__x-field").unit = "Yaw";
            anglesField.Q<FloatField>("appui-vector3field__y-field").unit = "Pitch";
            anglesField.Q<FloatField>("appui-vector3field__z-field").unit = "Roll";

            referenceCoordinateOffsetField.Q<FloatField>("appui-vector4field__w-field").unit = "D";
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void RegisterMainViewConverters()
        {
            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                string,
                StyleEnum<DisplayStyle>
            >(
                "VisualizationProbeNameToAddButtonVisibility",
                (ref string probeName) =>
                    string.IsNullOrEmpty(probeName) ? DisplayStyle.Flex : DisplayStyle.None
            );
            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                string,
                StyleEnum<DisplayStyle>
            >(
                "VisualizationProbeNameToInspectButtonVisibility",
                (ref string probeName) =>
                    string.IsNullOrEmpty(probeName) ? DisplayStyle.None : DisplayStyle.Flex
            );
            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "VisualizationProbeNameToCalibrationControlsEnabled",
                (ref string probeName) => !string.IsNullOrEmpty(probeName)
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<int, StyleEnum<DisplayStyle>>(
                "ManipulatorAxesCountToHandednessVisibility",
                (ref int axesCount) => axesCount == 4 ? DisplayStyle.Flex : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "ManipulatorHandednessToLeftButtonSelected",
                (ref ManipulatorHandedness handedness) => handedness == ManipulatorHandedness.Left
            );
            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "ManipulatorHandednessToRightButtonSelected",
                (ref ManipulatorHandedness handedness) => handedness == ManipulatorHandedness.Right
            );
        }
    }
}
