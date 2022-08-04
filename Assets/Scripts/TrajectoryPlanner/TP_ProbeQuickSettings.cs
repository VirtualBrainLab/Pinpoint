using System;
using TMPro;
using UnityEngine;

namespace TrajectoryPlanner
{
    public class TP_ProbeQuickSettings : MonoBehaviour
    {
        #region Components

        [SerializeField] private TMP_Text panelTitle;
        private ProbeManager _probeManager;

        #endregion


        #region Public Methods

        public void SetProbeManager(ProbeManager probeManager)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            _probeManager = probeManager;

            panelTitle.text = "#" + probeManager.GetID();
            panelTitle.color = probeManager.GetColor();
        }

        public void ZeroDepth()
        {
            Debug.Log("Zeroing probe depth");
        }

        #endregion
    }
}