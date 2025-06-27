using System.ComponentModel;
using UI.ViewModels;
using UnityEngine.UIElements;

namespace UI.Views
{
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

        public MainView(MainViewModel mainViewModel)
        {
            // Instantiate the UI document.
            var document = PinpointAppBuilder.Instance.MainUIDocument;
            document.CloneTree(this);

            // Get view model and register property changes and bindings.
            _viewModel = mainViewModel;
            _viewModel.PropertyChanged += OnPropertyChanged;
            dataSource = _viewModel;

            // Register component references.
            _sidePanelLeft = this.Q<VisualElement>("side-panel__left");
            _sidePanelRight = this.Q<VisualElement>("side-panel__right");
            _sidePanelLeftToggle = _sidePanelLeft.Q<Button>("side-panel__toggle");
            _sidePanelRightToggle = _sidePanelRight.Q<Button>("side-panel__toggle");

            // Register callbacks.
            _sidePanelLeftToggle.clicked += () => _viewModel.ToggleSidePanelLeftCommand.Execute();
            _sidePanelRightToggle.clicked += () => _viewModel.ToggleSidePanelRightCommand.Execute();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.IsSidePanelLeftOpen):
                    ToggleSidePanelLeft();
                    break;
                case nameof(MainViewModel.IsSidePanelRightOpen):
                    ToggleSidePanelRight();
                    break;
            }
        }

        #region Functions

        private void ToggleSidePanelLeft()
        {
            _sidePanelLeft.ToggleInClassList("side-panel--close");
        }

        private void ToggleSidePanelRight()
        {
            _sidePanelRight.ToggleInClassList("side-panel--close");
        }

        #endregion
    }
}
