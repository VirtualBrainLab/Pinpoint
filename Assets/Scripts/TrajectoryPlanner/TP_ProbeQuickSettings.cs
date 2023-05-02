using System.Linq;
using EphysLink;
using TMPro;
using TrajectoryPlanner.Probes;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrajectoryPlanner
{
    public class TP_ProbeQuickSettings : MonoBehaviour
    {
        #region Components

        [FormerlySerializedAs("probeIdText")] [SerializeField] private TMP_Text _probeIdText;
        [FormerlySerializedAs("coordinatePanel")] [SerializeField] private CoordinateEntryPanel _coordinatePanel;
        [FormerlySerializedAs("positionFields")] [SerializeField] private CanvasGroup _positionFields;
        [FormerlySerializedAs("angleFields")] [SerializeField] private CanvasGroup _angleFields;
        [SerializeField] private QuickSettingsLockBehavior _lockBehavior;
        
        private CommunicationManager _communicationManager;
        private TMP_InputField[] _inputFields;

        #endregion

        #region Unity

        /// <summary>
        ///     Initialize components
        /// </summary>
        private void Awake()
        {
            _communicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();
            _inputFields = gameObject.GetComponentsInChildren<TMP_InputField>(true);

            UpdateInteractable(true);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Set active probe (called by TrajectoryPlannerManager)
        /// </summary>
        /// <param name="probeManager">Probe Manager of active probe</param>
        public void SetActiveProbeManager()
        {
            if (ProbeManager.ActiveProbeManager == null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);

                ProbeManager.ActiveProbeManager.UIUpdateEvent.AddListener(UpdateProbeIdText);

                UpdateProbeIdText();

                _coordinatePanel.UpdateAxisLabels();

                // Handle picking up events
                ProbeManager.ActiveProbeManager.EphysLinkControlChangeEvent.AddListener(() =>
                {
                    UpdateInteractable();
                });

                _lockBehavior.SetLockState(ProbeManager.ActiveProbeManager.ProbeController.Locked);

                UpdateCoordinates();
                UpdateInteractable();
            }
        }

        public void UpdateInteractable(bool disableAll=false)
        {
            if (disableAll)
            {
                _positionFields.interactable = false;
                _angleFields.interactable = false;
            }
            else
            {
                _positionFields.interactable = ProbeManager.ActiveProbeManager != null ? !ProbeManager.ActiveProbeManager.IsEphysLinkControlled : true;
                _angleFields.interactable = ProbeManager.ActiveProbeManager == null || !ProbeManager.ActiveProbeManager.IsGhost;
            }
        }

        public void UpdateCoordinates()
        {
            _coordinatePanel.UpdateText();
        }

        public void UpdateProbeIdText()
        {
            _probeIdText.text = ProbeManager.ActiveProbeManager.name;
            _probeIdText.color = ProbeManager.ActiveProbeManager.Color;
        }

        /// <summary>
        ///     Move probe to brain surface and zero out depth
        /// </summary>
        public void ZeroDepth()
        {
            if (ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.enabled)
                ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.ComputeBrainSurfaceOffset();
            else
                ProbeManager.ActiveProbeManager.DropProbeToBrainSurface();

            UpdateCoordinates();
        }

        /// <summary>
        ///     Set current manipulator position to be Bregma and move probe to Bregma
        /// </summary>
        public void ResetZeroCoordinate()
        {
            if (ProbeManager.ActiveProbeManager.IsEphysLinkControlled)
            {
                _communicationManager.GetPos(ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.ManipulatorID,
                    zeroCoordinate => ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.ZeroCoordinateOffset = zeroCoordinate);
                ProbeManager.ActiveProbeManager.BrainSurfaceOffset = 0;
            }
            else
            {
                ProbeManager.ActiveProbeManager.ProbeController.ResetPosition();
                ProbeManager.ActiveProbeManager.ProbeController.SetProbePosition();
            }

            UpdateCoordinates();
        }

        public bool IsFocused()
        {
            return isActiveAndEnabled && _inputFields.Any(inputField => inputField != null && inputField.isFocused);
        }

        #endregion
    }
}