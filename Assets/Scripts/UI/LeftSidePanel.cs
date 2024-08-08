using UI.States;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class LeftSidePanel : MonoBehaviour
    {
        #region Components

        // Document.
        [SerializeField]
        private UIDocument _uiDocument;
        private VisualElement _root => _uiDocument.rootVisualElement;

        // Main panels.
        private VisualElement _basePanel;
        private VisualElement _menuBar;
        private VisualElement _tabView;

        // Menu bar.
        private Button _toggleButton;

        #endregion

        #region State

        private readonly LeftSidePanelState _state = new();

        #endregion

        #region Unity

        private void OnEnable()
        {
            // Register components.
            _basePanel = _root.Q("LeftSidePanel");
            _menuBar = _basePanel.Q("MenuBar");
            _tabView = _basePanel.Q<TabView>();

            _toggleButton = _basePanel.Q<Button>("ToggleButton");

            // Bind state.
            _basePanel.dataSource = _state;
            _menuBar.SetBinding("style.display", PanelVisibilityBinding());
            _tabView.SetBinding("style.display", PanelVisibilityBinding());
            _toggleButton.SetBinding("text", PanelVisibilityBinding());

            // Register events.
            _toggleButton.clicked += TogglePanelVisibility;
        }

        #endregion

        #region UI Functions

        private void TogglePanelVisibility()
        {
            _state.IsPanelVisible = !_state.IsPanelVisible;
            _state.Publish();
            print(_menuBar.style.display);
        }

        #endregion

        #region Data binders

        private static DataBinding PanelVisibilityBinding()
        {
            var binding = new DataBinding
            {
                dataSourcePath = new PropertyPath(nameof(LeftSidePanelState.IsPanelVisible)),
                bindingMode = BindingMode.ToTarget
            };

            binding.sourceToUiConverters.AddConverter(
                (ref bool visible) => visible ? DisplayStyle.Flex : DisplayStyle.None
            );

            binding.sourceToUiConverters.AddConverter((ref bool visible) => visible ? "<<" : ">>");

            return binding;
        }

        #endregion
    }
}
