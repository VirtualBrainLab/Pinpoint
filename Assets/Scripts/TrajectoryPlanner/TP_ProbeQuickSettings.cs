using System;
using System.Linq;
using EphysLink;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrajectoryPlanner
{
    public class TP_ProbeQuickSettings : MonoBehaviour
    {
        #region Components

        [SerializeField] private TMP_Text panelTitle;
        [SerializeField] private TP_CoordinateEntryPanel coordinatePanel;
        [SerializeField] private CanvasGroup positionFields;
        [SerializeField] private CanvasGroup angleFields;
        [SerializeField] private CanvasGroup buttons;
        [SerializeField] private TMP_InputField automaticMovementSpeedInputField;
        [SerializeField] private Button automaticMovementGoButton;
        
        private ProbeManager _probeManager;
        private CommunicationManager _communicationManager;
        private TP_QuestionDialogue _questionDialogue;
        private TMP_InputField[] _inputFields;

        #endregion

        #region Setup

        /// <summary>
        ///     Initialize components
        /// </summary>
        private void Awake()
        {
            _communicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();
            _questionDialogue = GameObject.Find("MainCanvas").transform.Find("QuestionDialoguePanel").gameObject
                .GetComponent<TP_QuestionDialogue>();

            _inputFields = gameObject.GetComponentsInChildren<TMP_InputField>(true);

            UpdateInteractable(true);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Set active probe (called by TrajectoryPlannerManager)
        /// </summary>
        /// <param name="probeManager">Probe Manager of active probe</param>
        public void SetProbeManager(ProbeManager probeManager)
        {
            if (probeManager == null)
                gameObject.SetActive(false);
            else
            {
                gameObject.SetActive(true);
                _probeManager = probeManager;

                panelTitle.text = probeManager.GetID().ToString();
                panelTitle.color = probeManager.GetColor();

                coordinatePanel.LinkProbe(probeManager);

                UpdateInteractable();
                UpdateCoordinates();
            }
        }

        public void UpdateInteractable(bool disableAll=false)
        {
            if (disableAll)
            {
                positionFields.interactable = false;
                angleFields.interactable = false;
                buttons.interactable = false;
            }
            else
            {
                positionFields.interactable = false; // !_probeManager.GetEphysLinkMovement();
                angleFields.interactable = true;
                buttons.interactable = true;
            }
        }

        public void UpdateCoordinates()
        {
            coordinatePanel.UpdateText();
        }

        /// <summary>
        ///     Move probe to brain surface and zero out depth
        /// </summary>
        public void ZeroDepth()
        {
            _probeManager.SetBrainSurfaceOffset();

            UpdateCoordinates();
        }

        /// <summary>
        ///     Set current manipulator position to be Bregma and move probe to Bregma
        /// </summary>
        public void ResetZeroCoordinate()
        {
            if (_probeManager.IsConnectedToManipulator())
            {
                _communicationManager.GetPos(_probeManager.GetManipulatorId(), _probeManager.SetZeroCoordinateOffset);
                _probeManager.SetBrainSurfaceOffset(0);
            }
            else
            {
                _probeManager.GetProbeController().ResetPosition();
                _probeManager.GetProbeController().SetProbePosition();
            }

            UpdateCoordinates();
        }

        public bool IsFocused()
        {
            return isActiveAndEnabled && _inputFields.Any(inputField => inputField != null && inputField.isFocused);
        }

        /// <summary>
        /// Toggle on or off automatic manipulator control
        /// </summary>
        /// <param name="isOn">Toggle state</param>
        public void ToggleAutomaticControl(bool isOn)
        {
            automaticMovementSpeedInputField.interactable = isOn;
            automaticMovementGoButton.interactable = isOn;
            
            // Spawn ghost
        }

        /// <summary>
        /// Automatically limit speed input from text input
        /// </summary>
        /// <param name="input">Value from text input</param>
        public void LimitSpeedInput(string input)
        {
            if (input.Length == 0)
            {
                return;
            }

            switch (int.Parse(input))
            {
                case < 0:
                    automaticMovementSpeedInputField.SetTextWithoutNotify("0");
                    break;
                case > 8000:
                    automaticMovementSpeedInputField.SetTextWithoutNotify("8000");
                    break;
            }
        }

        /// <summary>
        /// Send position to start automatic driving
        /// </summary>
        public void AutomaticallyDriveManipulator()
        {
            if (automaticMovementSpeedInputField.text.Length == 0)
            {
                automaticMovementSpeedInputField.SetTextWithoutNotify("200");
            }

            var speed = int.Parse(automaticMovementSpeedInputField.text);
            
            print(speed);
        }

        #endregion
    }
}