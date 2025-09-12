using UI.ViewModels;
using UnityEditor;
using UnityEngine.UIElements;
using Utils;
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

            DataTypeConverters.RegisterUnidirectionalConverterGroup<int, StyleEnum<DisplayStyle>>(
                "ManipulatorAxesCountToHandednessVisibility",
                (ref int axesCount) => axesCount == 4 ? DisplayStyle.Flex : DisplayStyle.None
            );
        }
    }
}
