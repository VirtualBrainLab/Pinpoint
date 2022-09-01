using System;
using SensapexLink;
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
        private CanvasGroup _canvasGroup;

        [SerializeField] private TP_CoordinateEntryPanel coordinatePanel;

        #endregion

        #region Setup

        /// <summary>
        ///     Initialize components
        /// </summary>
        private void Awake()
        {
            _communicationManager = GameObject.Find("SensapexLink").GetComponent<CommunicationManager>();
            _questionDialogue = GameObject.Find("MainCanvas").transform.Find("QuestionDialoguePanel").gameObject
                .GetComponent<TP_QuestionDialogue>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void FixedUpdate()
        {
            _canvasGroup.interactable = _canvasGroup.interactable switch
            {
                false when _communicationManager.IsConnected() => true,
                true when !_communicationManager.IsConnected() => false,
                _ => _canvasGroup.interactable
            };
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
            coordinatePanel.LinkProbe(probeManager);

            panelTitle.text = probeManager.GetID().ToString();
            panelTitle.color = probeManager.GetColor();
        }

        /// <summary>
        ///     Move probe to brain surface and zero out depth
        /// </summary>
        public void ZeroDepth()
        {
            _questionDialogue.SetNoCallback(() => { });
            _questionDialogue.SetYesCallback(() =>
            {
                _probeManager.SetBrainSurfaceOffset();
            });
            _questionDialogue.NewQuestion("Zero out depth?");
        }

        /// <summary>
        ///     Set current manipulator position to be Bregma and move probe to Bregma
        /// </summary>
        public void ResetBregma()
        {
            _questionDialogue.SetNoCallback(() => { });
            _questionDialogue.SetYesCallback(() =>
            {
                if (_probeManager.IsConnectedToManipulator())
                    _communicationManager.GetPos(_probeManager.GetManipulatorId(), _probeManager.SetBregmaOffset);
                else
                    _probeManager.GetProbeController().ResetPosition();
            });
            _questionDialogue.NewQuestion("Reset Bregma?");
        }

        #endregion
    }
}