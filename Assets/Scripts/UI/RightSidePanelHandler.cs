using UI.States;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class RightSidePanelHandler : MonoBehaviour
    {
        #region Components

        // State
        [SerializeField]
        private RightSidePanelState _state;

        // Document.
        [SerializeField]
        private UIDocument _uiDocument;
        private VisualElement _root => _uiDocument.rootVisualElement;

        // Panels.
        private VisualElement _rightSidePanel;

        // Menu bar.
        private Button _hideButton;

        #endregion

        #region Unity

        private void OnEnable()
        {
            // Get components.
            _rightSidePanel = _root.Q("RightSidePanel");
            _hideButton = _rightSidePanel.Q<Button>("ToggleButton");

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
