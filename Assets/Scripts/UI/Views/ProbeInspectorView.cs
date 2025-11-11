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
        private Vector3Field _positionField;
        private ProbeInspectorViewModel _probeInspectorViewModel;

        public ProbeInspectorView(ProbeInspectorViewModel probeInspectorViewModel)
        {
            _probeInspectorViewModel = probeInspectorViewModel;

            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>("probe-inspector-view");

            // Register view model and property changes.
            root.dataSource = probeInspectorViewModel;

            // Register component references.
            _positionField = root.Q<Vector3Field>("probe-inspector__position-field");
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
            _positionField.RegisterValueChangedCallback(evt =>
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
            anglesField.Q<FloatField>("appui-vector3field__x-field").unit = "Yaw";
            anglesField.Q<FloatField>("appui-vector3field__y-field").unit = "Pitch";
            anglesField.Q<FloatField>("appui-vector3field__z-field").unit = "Roll";

            UpdatePositionFieldUnits(probeInspectorViewModel.ConvertAPML2Probe);

            probeInspectorViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(ProbeInspectorViewModel.ConvertAPML2Probe))
                {
                    UpdatePositionFieldUnits(probeInspectorViewModel.ConvertAPML2Probe);
                }
            };
        }

        private void UpdatePositionFieldUnits(bool convertAPML2Probe)
        {
            if (convertAPML2Probe)
            {
                _positionField.Q<FloatField>("appui-vector3field__x-field").unit = "Forward";
                _positionField.Q<FloatField>("appui-vector3field__y-field").unit = "Right";
                _positionField.Q<FloatField>("appui-vector3field__z-field").unit = "DV";
            }
            else
            {
                _positionField.Q<FloatField>("appui-vector3field__x-field").unit = "AP";
                _positionField.Q<FloatField>("appui-vector3field__y-field").unit = "ML";
                _positionField.Q<FloatField>("appui-vector3field__z-field").unit = "DV";
            }
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

            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "VisualizingManipulatorIdToInspectManipulatorButtonTitle",
                (ref string visualizingManipulatorId) =>
                    string.IsNullOrEmpty(visualizingManipulatorId)
                        ? ""
                        : $"Inspect {visualizingManipulatorId}"
            );
        }
    }
}
