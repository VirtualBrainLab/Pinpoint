using System.ComponentModel;
using UI.ViewModels;
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

        private readonly VisualElement _sidePanelLeft;
        private readonly VisualElement _sidePanelRight;

        private readonly Button _sidePanelLeftToggle;
        private readonly Button _sidePanelRightToggle;

        #endregion

        #region Architecture

        private readonly MainViewModel _viewModel;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MainView"/> class.
        /// Sets up UI document, component references, and event bindings.
        /// </summary>
        /// <param name="mainViewModel">The view model to bind to.</param>
        public MainView(MainViewModel mainViewModel)
        {
            // Instantiate the UI document.
            var document = PinpointAppBuilder.Instance.MainUIDocument;
            document.CloneTree(this);
            
            pickingMode = PickingMode.Ignore;

            // Get view model and register property changes and bindings.
            _viewModel = mainViewModel;
            _viewModel.PropertyChanged += OnPropertyChanged;
            dataSource = _viewModel;
            
            // Register component references.
            _sidePanelLeft = this.Q<VisualElement>("left-side-panel");
            _sidePanelRight = this.Q<VisualElement>("right-side-panel");
            _sidePanelLeftToggle = _sidePanelLeft.Q<Button>("left-side-panel__toggle");
            _sidePanelRightToggle = _sidePanelRight.Q<Button>("right-side-panel__toggle");
            
            // Initialize subviews.
            _ = new AutomationView(this.Q<VisualElement>("automation-view"));

            // Register callbacks.
            _sidePanelLeftToggle.clicked += () => _viewModel.ToggleSidePanelLeftCommand.Execute();
            _sidePanelRightToggle.clicked += () => _viewModel.ToggleSidePanelRightCommand.Execute();
            
            // Initialize view from view model state.
            if (!_viewModel.IsSidePanelLeftOpen)
            {
                _sidePanelLeft.AddToClassList("side-panel--close");
            }
            if (!_viewModel.IsSidePanelRightOpen)
            {
                _sidePanelRight.AddToClassList("side-panel--close");
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
                case nameof(MainViewModel.IsSidePanelLeftOpen):
                    _sidePanelLeft.ToggleInClassList("side-panel--close");
                    break;
                case nameof(MainViewModel.IsSidePanelRightOpen):
                    _sidePanelRight.ToggleInClassList("side-panel--close");
                    break;
            }
        }
    }
}
