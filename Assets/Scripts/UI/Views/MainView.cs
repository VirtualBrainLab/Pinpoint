using System;
using System.Linq;
using UI.ViewModels;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class MainView: VisualElement
    {
        #region Component References
        
        private readonly VisualElement _sidePanelLeft;
        private readonly VisualElement _sidePanelRight;

        private readonly Button _sidePanelLeftToggle;
        private readonly Button _sidePanelRightToggle;

        #endregion

        #region Architecture

        private readonly MainViewBindings _viewBindings;
        private readonly MainViewModel _viewModel;

        #endregion

        public MainView(MainViewModel mainViewModel)
        {
            // Instantiate the UI document.
            var document = PinpointAppBuilder.Instance.MainUIDocument;
            document.CloneTree(this);
            
            // Set the flex grow to fill the parent container.
            style.flexGrow = 1;
            
            // Get view bindings.
            _viewBindings = Children().First().dataSource as MainViewBindings;
            if (_viewBindings == null)
            {
                throw new MissingFieldException("MainViewBindings was not found.");
            }
            
            // Register component references.
            _sidePanelLeft = this.Q<VisualElement>("side-panel__left");
            _sidePanelRight = this.Q<VisualElement>("side-panel__right");
            _sidePanelLeftToggle = _sidePanelLeft.Q<Button>("side-panel__toggle");
            _sidePanelRightToggle = _sidePanelRight.Q<Button>("side-panel__toggle");
            
            // Register callbacks.
            // TODO
        }

    }
}