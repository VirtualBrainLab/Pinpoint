using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;
using Button = Unity.AppUI.UI.Button;
#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace UI.Views
{
    /// <summary>
    /// Represents the main view of the Pinpoint application UI.
    /// Binds to the <see cref="MainViewModel"/> for property changes.
    /// </summary>
    public class MainView
    {
        #region Constants

        private const int LEFT_SIDE_PANEL_SPLITTER_INDEX = 0;
        private const int RIGHT_SIDE_PANEL_SPLITTER_INDEX = 1;

        #endregion
        #region Component References

        public VisualElement Root { get; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MainView"/> class.
        /// Sets up UI document, component references, and event bindings.
        /// </summary>
        /// <param name="mainViewModel">The view model to bind to.</param>
        /// <param name="ephysLinkViewModel">Ephys Link view model to pass to the automation view.</param>
        /// <param name="sceneViewModel">Scene view model to pass to the scene hierarchy view.</param>
        /// <param name="atlasViewModel">Atlas view model to pass to the atlas view.</param>
        /// <param name="probeInspectorViewModel">Probe inspector view model to pass to the probe inspector view.</param>
        /// <param name="automationViewModel">Automation view model to pass to the automation view.</param>
        public MainView(
            MainViewModel mainViewModel,
            EphysLinkViewModel ephysLinkViewModel,
            SceneViewModel sceneViewModel,
            AtlasViewModel atlasViewModel,
            ProbeInspectorViewModel probeInspectorViewModel,
            AutomationViewModel automationViewModel
        )
        {
            // Get root element.
            Root = PinpointApp.Current.rootVisualElement;

            // Get view model and register property changes and bindings.
            Root.dataSource = mainViewModel;

            // Register component references.
            var mainSplitView = Root.Q<SplitView>("main-split-view");
            var leftSidePanelCollapseButton = Root.Q<Button>("left-side-panel__collapse-button");
            var rightSidePanelCollapseButton = Root.Q<Button>("right-side-panel__collapse-button");
            var leftSidePanelTabs = Root.Q<Tabs>("left-side-panel__tabs");

            // Initialize subviews.
            _ = new SceneView(Root.Q<TemplateContainer>("scene-view"), sceneViewModel);
            _ = new AtlasView(Root.Q<TemplateContainer>("atlas-view"), atlasViewModel);
            _ = new ProbeInspectorView(
                Root.Q<TemplateContainer>("probe-inspector-view"),
                probeInspectorViewModel
            );
            _ = new AutomationView(
                Root.Q<TemplateContainer>("automation-view"),
                automationViewModel,
                ephysLinkViewModel
            );

            // Register event handlers.
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
            DataTypeConverters.RegisterUnidirectionalConverterGroup<bool, StyleEnum<DisplayStyle>>(
                "BooleanToInspectorVisibility",
                (ref bool isAutomationActive) =>
                    isAutomationActive ? DisplayStyle.None : DisplayStyle.Flex
            );
        }
    }
}
