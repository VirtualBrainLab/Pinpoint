using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class MainUI : MonoBehaviour
    {
        #region Components

        [SerializeField]
        private UIDocument _uiDocument;

        private VisualElement _showLeftSidePanelPanel;
        private Button _showLeftSidePanelButton;
        private VisualElement _leftSidePanel;
        private VisualElement _showRightSidePanelPanel;
        private Button _showRightSidePanelButton;
        private VisualElement _rightSidePanel;

        #endregion

        #region Unity

        private void OnEnable()
        {
            // Register components.
            _showLeftSidePanelPanel = _uiDocument.rootVisualElement.Q("ShowLeftSidePanelPanel");
            _showLeftSidePanelButton = _uiDocument.rootVisualElement.Q<Button>(
                "ShowLeftSidePanelButton"
            );
            _leftSidePanel = _uiDocument.rootVisualElement.Q("LeftSidePanel");
            _showRightSidePanelPanel = _uiDocument.rootVisualElement.Q("ShowRightSidePanelPanel");
            _showRightSidePanelButton = _uiDocument.rootVisualElement.Q<Button>(
                "ShowRightSidePanelButton"
            );
            _rightSidePanel = _uiDocument.rootVisualElement.Q("RightSidePanel");

            // Register events.
            _showLeftSidePanelButton.RegisterCallback<ClickEvent>(ShowLeftSidePanel);
            _showRightSidePanelButton.RegisterCallback<ClickEvent>(ShowRightSidePanel);
        }

        #endregion

        #region UI Functions

        private void ShowLeftSidePanel(ClickEvent evt)
        {
            _leftSidePanel.style.display = DisplayStyle.Flex;
            _showLeftSidePanelPanel.style.display = DisplayStyle.None;
        }

        private void ShowRightSidePanel(ClickEvent evt)
        {
            _rightSidePanel.style.display = DisplayStyle.Flex;
            _showRightSidePanelPanel.style.display = DisplayStyle.None;
        }
        #endregion
    }
}
