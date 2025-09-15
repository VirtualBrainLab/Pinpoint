using UI.ViewModels;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine.UIElements;
using Utils;
using Utils.Types;
using Button = Unity.AppUI.UI.Button;
using Toggle = Unity.AppUI.UI.Toggle;
#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace UI.Views
{
    /// <summary>
    ///     Represents the main view of the Pinpoint application UI.
    ///     Binds to the <see cref="MainViewModel" /> for property changes.
    /// </summary>
    public class MainView
    {
        #region Constants

        private const int LEFT_SIDE_PANEL_SPLITTER_INDEX = 0;
        private const int RIGHT_SIDE_PANEL_SPLITTER_INDEX = 1;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainView" /> class.
        ///     Sets up UI document, component references, and event bindings.
        /// </summary>
        /// <param name="mainViewModel">The view model to bind to.</param>
        public MainView(MainViewModel mainViewModel)
        {
            // Get root element.
            var root = PinpointApp.RootVisualElement;

            // Get view model and register property changes and bindings.
            root.dataSource = mainViewModel;

            // Register component references.
            var mainSplitView = root.Q<SplitView>("main-split-view");
            var automationToggle = root.Q<Toggle>("automation-toggle");
            var leftSidePanelCollapseButton = root.Q<Button>("left-side-panel__collapse-button");
            var rightSidePanelCollapseButton = root.Q<Button>("right-side-panel__collapse-button");
            var leftSidePanelTabs = root.Q<Tabs>("left-side-panel__tabs");

            // Initialize subviews.
            _ = PinpointApp.Services.GetRequiredService<SceneView>();
            _ = PinpointApp.Services.GetRequiredService<AtlasView>();

            _ = PinpointApp.Services.GetRequiredService<SettingsView>();

            _ = PinpointApp.Services.GetRequiredService<ProbeInspectorView>();
            _ = PinpointApp.Services.GetRequiredService<ManipulatorInspectorView>();

            // Register event handlers.
            automationToggle.RegisterValueChangedCallback(evt =>
            {
                mainViewModel.SetAutomationModeActiveCommand.Execute(evt.newValue);
            });
            leftSidePanelCollapseButton.clickable.clicked += () =>
            {
                mainSplitView.CollapseSplitter(
                    LEFT_SIDE_PANEL_SPLITTER_INDEX,
                    CollapseDirection.Backward
                );
                mainViewModel.SetMainSplitViewStateCommand.Execute(mainSplitView.SaveState());
            };
            rightSidePanelCollapseButton.clickable.clicked += () =>
            {
                mainSplitView.CollapseSplitter(
                    RIGHT_SIDE_PANEL_SPLITTER_INDEX,
                    CollapseDirection.Forward
                );
                mainViewModel.SetMainSplitViewStateCommand.Execute(mainSplitView.SaveState());
            };
            leftSidePanelTabs.RegisterValueChangedCallback(evt =>
                mainViewModel.SetLeftSidePanelTabIndexCommand.Execute(evt.newValue)
            );

            // Initialize view from view model state.
            mainSplitView.RestoreState(mainViewModel.MainSplitViewState);

#if !APP_UI
            // Hide the new UI if the directive is not enabled.
            Root.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
#endif
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void RegisterMainViewConverters()
        {
            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                InspectorDisplayType,
                StyleEnum<DisplayStyle>
            >(
                "InspectorDisplayTypeToProbeInspectorVisibility",
                (ref InspectorDisplayType displayType) =>
                    displayType == InspectorDisplayType.Probe
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                InspectorDisplayType,
                StyleEnum<DisplayStyle>
            >(
                "InspectorDisplayTypeToManipulatorInspectorVisibility",
                (ref InspectorDisplayType displayType) =>
                    displayType == InspectorDisplayType.Manipulator
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                InspectorDisplayType,
                StyleEnum<DisplayStyle>
            >(
                "InspectorDisplayTypeToAutomationInspectorVisibility",
                (ref InspectorDisplayType displayType) =>
                    displayType == InspectorDisplayType.Automation
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                InspectorDisplayType,
                StyleEnum<DisplayStyle>
            >(
                "InspectorDisplayTypeToNothingSelectedVisibility",
                (ref InspectorDisplayType displayType) =>
                    displayType == InspectorDisplayType.Nothing
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                InspectorDisplayType,
                StyleEnum<DisplayStyle>
            >(
                "InspectorDisplayTypeToInspectorScrollViewVisibility",
                (ref InspectorDisplayType displayType) =>
                    displayType == InspectorDisplayType.Nothing
                        ? DisplayStyle.None
                        : DisplayStyle.Flex
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "IsAutomationActiveToProbeInspectorEnabled",
                (ref bool isAutomationActive) => !isAutomationActive
            );
        }
    }
}
