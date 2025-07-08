using System.ComponentModel;
using Models;
using UI.Utils;
using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;

namespace UI.Views
{
    /// <summary>
    /// Represents the main view of the Pinpoint application UI.
    /// Binds to the <see cref="MainViewModel"/> for property changes.
    /// </summary>
    public class MainView
    {
        #region Component References

        public VisualElement Root { get; }

        private readonly VisualElement _leftSidePanel;
        private readonly VisualElement _rightSidePanel;

        private readonly Button _leftSidePanelToggle;
        private readonly Button _rightSidePanelToggle;

        private readonly Tabs _leftSidePanelTabs;

        #endregion

        private readonly MainViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainView"/> class.
        /// Sets up UI document, component references, and event bindings.
        /// </summary>
        /// <param name="mainViewModel">The view model to bind to.</param>
        /// <param name="automationViewModel">Automation view model to pass to the automation view.</param>
        /// <param name="atlasViewModel">Atlas view model to pass to the atlas view</param>
        public MainView(
            MainViewModel mainViewModel,
            AutomationViewModel automationViewModel,
            AtlasViewModel atlasViewModel
        )
        {
            // Get root element.
            Root = PinpointApp.Current.rootVisualElement;

            // Get view model and register property changes and bindings.
            _viewModel = mainViewModel;
            _viewModel.PropertyChanged += OnPropertyChanged;
            Root.dataSource = _viewModel;

            // Register component references.
            _leftSidePanel = Root.Q<VisualElement>("left-side-panel");
            _rightSidePanel = Root.Q<VisualElement>("right-side-panel");
            _leftSidePanelToggle = _leftSidePanel.Q<Button>("left-side-panel__toggle");
            _rightSidePanelToggle = _rightSidePanel.Q<Button>("right-side-panel__toggle");
            _leftSidePanelTabs = _leftSidePanel.Q<Tabs>("left-side-panel__tabs");

            // Initialize subviews.
            _ = new AutomationView(Root.Q<VisualElement>("automation-view"), automationViewModel);
            _ = new AtlasView(Root.Q<VisualElement>("atlas-view"), atlasViewModel);

            // Register callbacks.
            _leftSidePanelToggle.clickable.clicked += _viewModel.ToggleLeftSidePanelCommand.Execute;
            _rightSidePanelToggle.clickable.clicked += _viewModel
                .ToggleRightSidePanelCommand
                .Execute;
            _leftSidePanelTabs.RegisterValueChangedCallback(evt =>
                _viewModel.SetLeftSidePanelTabIndexCommand.Execute(evt.newValue)
            );

            // Initialize view from view model state.
            if (!_viewModel.IsLeftSidePanelOpen)
            {
                _leftSidePanel.AddToClassList("side-panel--close");
            }

            if (!_viewModel.IsRightSidePanelOpen)
            {
                _rightSidePanel.AddToClassList("side-panel--close");
            }
        }

        /// <summary>
        /// Handles property change notifications from the view model.
        /// Updates the UI based on which property changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The property changed event arguments.</param>
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.IsLeftSidePanelOpen):
                    _leftSidePanel.ToggleInClassList("side-panel--close");
                    break;
                case nameof(MainViewModel.IsRightSidePanelOpen):
                    _rightSidePanel.ToggleInClassList("side-panel--close");
                    break;
            }
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RegisterMainViewConverters()
        {
            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "BooleanToSidePanelToggleIcon",
                (ref bool isVisible) => isVisible ? "minus" : "plus"
            );

            DataTypeConverters.RegisterBidirectionalConverterGroup(
                "MainModeToInt",
                (ref MainMode mode) => (int)mode,
                (ref int modeIndex) => (MainMode)modeIndex
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                MainMode,
                StyleEnum<DisplayStyle>
            >(
                "MainModeToInspectorPanelDisplayStyle",
                (ref MainMode mode) =>
                    mode == MainMode.Planning ? DisplayStyle.Flex : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                MainMode,
                StyleEnum<DisplayStyle>
            >(
                "MainModeToAutomationPanelDisplayStyle",
                (ref MainMode mode) =>
                    mode == MainMode.Automation ? DisplayStyle.Flex : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                MainMode,
                StyleEnum<DisplayStyle>
            >(
                "MainModeToManualControlPanelDisplayStyle",
                (ref MainMode mode) =>
                    mode == MainMode.Automation ? DisplayStyle.Flex : DisplayStyle.None
            );
        }
    }
}
