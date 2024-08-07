using System;
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
        private VisualElement _contentPanel;
        private VisualElement _showPanel;

        // Show and hide buttons.
        private Button _showButton;
        private Button _hideButton;

        #endregion

        #region Unity

        private void OnEnable()
        {
            // Register components.
            _basePanel = _root.Q("LeftSidePanel");
            _contentPanel = _basePanel.Q("ContentPanel");
            _showPanel = _basePanel.Q("ShowPanel");

            _showButton = _showPanel.Q<Button>();
            _hideButton = _contentPanel.Q<Button>("HideButton");

            // Register events.
            _showButton.clicked += ShowPanel;
            _hideButton.clicked += HidePanel;
        }

        private void OnDisable()
        {
            // Unregister events.
            _showButton.clicked -= ShowPanel;
            _hideButton.clicked -= HidePanel;
        }

        #endregion

        #region UI Functions

        private void HidePanel()
        {
            _contentPanel.style.display = DisplayStyle.None;
            _showPanel.style.display = DisplayStyle.Flex;
        }

        private void ShowPanel()
        {
            _contentPanel.style.display = DisplayStyle.Flex;
            _showPanel.style.display = DisplayStyle.None;
        }

        #endregion
    }
}
