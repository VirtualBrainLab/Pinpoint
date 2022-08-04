using SensapexLink;
using TMPro;
using UnityEngine;

namespace TrajectoryPlanner
{
    public class TP_ProbeQuickSettings : MonoBehaviour
    {
        /// <summary>
        ///     Initialize components
        /// </summary>
        private void Awake()
        {
            _communicationManager = GameObject.Find("SensapexLink").GetComponent<CommunicationManager>();
        }

        #region Components

        [SerializeField] private TMP_Text panelTitle;
        private ProbeManager _probeManager;
        private CommunicationManager _communicationManager;

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

            panelTitle.text = "#" + probeManager.GetID();
            panelTitle.color = probeManager.GetColor();
        }

        /// <summary>
        ///     Move probe to brain surface and zero out depth
        /// </summary>
        public void ZeroDepth()
        {
            Debug.Log("Zeroing probe depth");
        }

        /// <summary>
        ///     Set current manipulator position to be Bregma and move probe to Bregma
        /// </summary>
        public void ResetBregma()
        {
            Debug.Log("Reset probe to Bregma");
        }

        #endregion
    }
}