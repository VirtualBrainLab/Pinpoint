using System;
using UnityEngine;

namespace TrajectoryPlanner.UI.EphysCopilot
{
    [Serializable]
    internal struct DemoData
    {
        public Vector3 angle;
        public Vector3 idle;
        public Vector4 insertion;
    }
    public class CopilotDemoHandler : MonoBehaviour
    {
        #region Components

        [SerializeField] private GameObject _startButton;
        [SerializeField] private GameObject _stopButton;

        #endregion
    }
}
