using System;
using Pinpoint.UI.EphysLinkSettings;
using UI.States;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class DemoUIHandler : MonoBehaviour
    {
        #region Components

        [SerializeField]
        private DemoUIState _uiState;

        [SerializeField]
        private UIDocument _uiDocument;

        [SerializeField]
        private EphysLinkSettings _ephysLinkSettings;

        private VisualElement _root => _uiDocument.rootVisualElement;

        [SerializeField]
        private BrainCameraController _brainCameraController;

        private Button _exitButton;

        #endregion

        private void OnEnable()
        {
            // Set Camera.
            _brainCameraController.SetZoom(10);
            _brainCameraController.transform.rotation = Quaternion.Euler(180, -180, -180);
            
            // Set button.
            _exitButton = _root.Q<Button>("exit-button");
            _exitButton.clicked += OnExitDemoPressed;
        }

        private void Update()
        {
            _brainCameraController.transform.Rotate(0, 5 * Time.deltaTime, 0);
        }

        private void OnDisable()
        {
            _exitButton.clicked -= OnExitDemoPressed;
        }

        private void OnExitDemoPressed()
        {
            // Stop manipulators.

            // Reset camera position.

            // Reset UI state.
            _uiState.Stage = DemoStage.Home;

            // Close the UI.
            _ephysLinkSettings.StopAutomationDemo();
        }
    }
}
