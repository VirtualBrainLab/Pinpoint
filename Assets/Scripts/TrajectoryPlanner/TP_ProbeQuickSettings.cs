using System;
using System.Linq;
using EphysLink;
using TMPro;
using UnityEngine;

namespace TrajectoryPlanner
{
    public class TP_ProbeQuickSettings : MonoBehaviour
    {
        #region Components

        [SerializeField] private TMP_Text panelTitle;
        private ProbeManager _probeManager;
        private CommunicationManager _communicationManager;
        private TP_QuestionDialogue _questionDialogue;

        [SerializeField] private TP_CoordinateEntryPanel coordinatePanel;
        [SerializeField] private CanvasGroup positionFields;
        [SerializeField] private CanvasGroup angleFields;
        [SerializeField] private CanvasGroup buttons;

        private TMP_InputField[] inputFields;

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

            inputFields = gameObject.GetComponentsInChildren<TMP_InputField>(true);

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
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            _probeManager = probeManager;

            panelTitle.text = probeManager.GetID().ToString();
            panelTitle.color = probeManager.GetColor();

            coordinatePanel.LinkProbe(probeManager);
            UpdateInteractable();
            UpdateCoordinates();
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
        }

        public bool IsFocused()
        {
            return inputFields.Any(inputField => inputField.isFocused);
        }

        #endregion
    }
}