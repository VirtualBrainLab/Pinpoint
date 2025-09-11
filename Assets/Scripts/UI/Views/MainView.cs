using UI.ViewModels;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using UnityEditor;
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
        public MainView(MainViewModel mainViewModel)
        {
            // Get root element.
            Root = PinpointApp.RootVisualElement;

            // Get view model and register property changes and bindings.
            Root.dataSource = mainViewModel;

            // Register component references.
            var mainSplitView = Root.Q<SplitView>("main-split-view");
            var leftSidePanelCollapseButton = Root.Q<Button>("left-side-panel__collapse-button");
            var rightSidePanelCollapseButton = Root.Q<Button>("right-side-panel__collapse-button");
            var leftSidePanelTabs = Root.Q<Tabs>("left-side-panel__tabs");

            // Initialize subviews.
            _ = PinpointApp.Services.GetRequiredService<SceneView>();
            _ = PinpointApp.Services.GetRequiredService<AtlasView>();
            _ = PinpointApp.Services.GetRequiredService<SettingsView>();
            _ = PinpointApp.Services.GetRequiredService<ProbeInspectorView>();

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
