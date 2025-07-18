using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine.UIElements;
using Utils.Types;
using Button = Unity.AppUI.UI.Button;

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
        /// <param name="automationViewModel">Automation view model to pass to the automation view.</param>
        /// <param name="ephysLinkViewModel">Ephys Link view model to pass to the automation view.</param>
        /// <param name="sceneViewModel">Scene view model to pass to the scene hierarchy view.</param>
        /// <param name="atlasViewModel">Atlas view model to pass to the atlas view.</param>
        public MainView(
            MainViewModel mainViewModel,
            AutomationViewModel automationViewModel,
            EphysLinkViewModel ephysLinkViewModel,
            SceneViewModel sceneViewModel,
            AtlasViewModel atlasViewModel
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
            _ = new AutomationView(
                Root.Q<TemplateContainer>("automation-view"),
                automationViewModel,
                ephysLinkViewModel
            );
            _ = new AtlasView(Root.Q<VisualElement>("atlas-view"), atlasViewModel);

            // Register event handlers.
            leftSidePanelCollapseButton.clickable.clicked += () =>
            {
                mainSplitView.CollapseSplitter(
                    LEFT_SIDE_PANEL_SPLITTER_INDEX,
                    CollapseDirection.Backward
                );
            };
            rightSidePanelCollapseButton.clickable.clicked += () =>
            {
                mainSplitView.CollapseSplitter(
                    RIGHT_SIDE_PANEL_SPLITTER_INDEX,
                    CollapseDirection.Forward
                );
            };
            leftSidePanelTabs.RegisterValueChangedCallback(evt =>
                mainViewModel.SetLeftSidePanelTabIndexCommand.Execute(evt.newValue)
            );
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
