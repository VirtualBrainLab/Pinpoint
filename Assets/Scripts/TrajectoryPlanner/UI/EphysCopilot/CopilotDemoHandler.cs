using System;
using System.IO;
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

        private void Start()
        {
            print(Application.streamingAssetsPath + "/copilot_demo.json");
            print(File.ReadAllText(Application.streamingAssetsPath + "/copilot_demo.json"));
            // var data = JsonUtility.FromJson<DemoData[]>(Resources
            //     .Load<TextAsset>(Application.streamingAssetsPath + "/copilot_demo.json").text);
            // print(data);
        }
    }
}
