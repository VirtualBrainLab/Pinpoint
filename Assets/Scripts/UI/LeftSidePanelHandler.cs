using UI.States;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class LeftSidePanelHandler : MonoBehaviour
    {
        #region Components

        // State
        [SerializeField]
        private LeftSidePanelState _state;

        // Document.
        [SerializeField]
        private UIDocument _uiDocument;
        private VisualElement _root => _uiDocument.rootVisualElement;

        // Panels.
        private VisualElement _leftSidePanel;

        // Menu bar.
        private Button _hideButton;

        #endregion

        #region Unity

        private void OnEnable()
        {
            // Get components.
            _leftSidePanel = _root.Q("LeftSidePanel");
            _hideButton = _leftSidePanel.Q<Button>("ToggleButton");

            // Register callbacks.
            _hideButton.clicked += ToggleVisibility;
        }

        private void OnDisable()
        {
            // Unregister callbacks.
            _hideButton.clicked -= ToggleVisibility;
        }

        #endregion

        #region UI Functions

        private void ToggleVisibility()
        {
            _state.IsVisible = !_state.IsVisible;
        }

        #endregion
    }
}
