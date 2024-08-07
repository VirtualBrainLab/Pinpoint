using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LeftSidePanelUI : MonoBehaviour
{
    #region Components

    [SerializeField]
    private UIDocument _uiDocument;

    private VisualElement _leftSidePanel;
    private Button _hideLeftSidePanelButton;
    private VisualElement _showLeftSidePanelPanel;
    private VisualElement _rightSidePanel;
    private Button _hideRightSidePanelButton;
    private VisualElement _showRightSidePanelPanel;

    #endregion

    #region Unity

    private void OnEnable()
    {
        // Register components.
        _leftSidePanel = _uiDocument.rootVisualElement.Q("LeftSidePanel");
        _hideLeftSidePanelButton = _uiDocument.rootVisualElement.Q<Button>(
            "LeftSidePanelHideButton"
        );
        _showLeftSidePanelPanel = _uiDocument.rootVisualElement.Q("ShowLeftSidePanelPanel");
        _rightSidePanel = _uiDocument.rootVisualElement.Q("RightSidePanel");
        _hideRightSidePanelButton = _uiDocument.rootVisualElement.Q<Button>(
            "RightSidePanelHideButton"
        );
        _showRightSidePanelPanel = _uiDocument.rootVisualElement.Q("ShowRightSidePanelPanel");

        // Register events.
        _hideLeftSidePanelButton.RegisterCallback<ClickEvent>(HideLeftSidePanel);
        _hideRightSidePanelButton.RegisterCallback<ClickEvent>(HideRightSidePanel);
    }

    #endregion

    #region UI Functions

    private void HideLeftSidePanel(ClickEvent evt)
    {
        _leftSidePanel.style.display = DisplayStyle.None;
        _showLeftSidePanelPanel.style.display = DisplayStyle.Flex;
    }

    private void HideRightSidePanel(ClickEvent evt)
    {
        _rightSidePanel.style.display = DisplayStyle.None;
        _showRightSidePanelPanel.style.display = DisplayStyle.Flex;
    }

    #endregion
}
