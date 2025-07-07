using System.ComponentModel;
using Models;
using UI.Utils;
using UI.ViewModels;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Views
{
    /// <summary>
    /// Represents the main view of the Pinpoint application UI.
    /// Binds to the <see cref="MainViewModel"/> for property changes.
    /// </summary>
    public class MainView : VisualElement
    {
        #region Component References

        private readonly VisualElement _leftSidePanel;
        private readonly VisualElement _rightSidePanel;

        private readonly Button _sidePanelLeftToggle;
        private readonly Button _sidePanelRightToggle;

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
            // Instantiate the UI document.
            var document = PinpointAppBuilder.Instance.MainUIDocument;
            document.CloneTree(this);

            // Let user input to pass through to the 3D scene.
            pickingMode = PickingMode.Ignore;

            // Get view model and register property changes and bindings.
            _viewModel = mainViewModel;
            _viewModel.PropertyChanged += OnPropertyChanged;
            dataSource = _viewModel;

            // Register component references.
            _leftSidePanel = this.Q<VisualElement>("left-side-panel");
            _rightSidePanel = this.Q<VisualElement>("right-side-panel");
            _sidePanelLeftToggle = _leftSidePanel.Q<Button>("left-side-panel__toggle");
            _sidePanelRightToggle = _rightSidePanel.Q<Button>("right-side-panel__toggle");

            // Initialize subviews.
            _ = new AutomationView(this.Q<VisualElement>("automation-view"), automationViewModel);
            _ = new AtlasView(this.Q<VisualElement>("atlas-view"), atlasViewModel);

            // Register callbacks.
            _sidePanelLeftToggle.clicked += _viewModel.ToggleLeftSidePanelCommand.Execute;
            _sidePanelRightToggle.clicked += _viewModel.ToggleRightSidePanelCommand.Execute;

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
                "BooleanToLeftSidePanelToggleText",
                (ref bool isVisible) => isVisible ? "\u25C0" : "\u25B6"
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "BooleanToRightSidePanelToggleText",
                (ref bool isVisible) => isVisible ? "\u25B6" : "\u25C0"
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
