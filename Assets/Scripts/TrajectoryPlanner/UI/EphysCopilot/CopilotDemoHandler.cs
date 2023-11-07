using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TrajectoryPlanner.UI.EphysCopilot
{
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal struct DemoDataJson
    {
        public List<ManipulatorDataJson> data;
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal struct ManipulatorDataJson
    {
        public List<float> angle;
        public List<float> idle;
        public List<float> insertion;
    }

    /// <summary>
    /// Angle: Yaw, Pitch, Roll
    /// Idle: AP, ML, DV
    /// Insertion: AP, ML, DV, Depth
    /// </summary>
    internal struct ManipulatorData
    {
        public Vector3 Angle;
        public Vector3 IdlePos;
        public Vector4 InsertionPos;
    }

    public class CopilotDemoHandler : MonoBehaviour
    {
        #region Components

        [SerializeField] private GameObject _startButton;
        [SerializeField] private GameObject _stopButton;

        #endregion

        #region Properties
        
        private Dictionary<ProbeManager, ManipulatorData> _demoManipulatorToData = new();

        #endregion

        private void Start()
        {
            // Parse JSON
            var jsonString = File.ReadAllText(Application.streamingAssetsPath + "/copilot_demo.json");
            var data = JsonUtility.FromJson<DemoDataJson>(jsonString);

            // Convert to ManipulatorData and match with manipulator
            foreach (var manipulatorData in data.data)
            {
                // Convert data
                var convertedData = new ManipulatorData
                {
                    Angle = new Vector3(manipulatorData.angle[0], manipulatorData.angle[1], manipulatorData.angle[2]),
                    IdlePos = new Vector3(manipulatorData.idle[0], manipulatorData.idle[1], manipulatorData.idle[2]),
                    InsertionPos = new Vector4(manipulatorData.insertion[0], manipulatorData.insertion[1],
                        manipulatorData.insertion[2], manipulatorData.insertion[3])
                };
                
                // Match to manipulator
                var matchingManipulator = ProbeManager.Instances.FirstOrDefault(
                    manager => manager.IsEphysLinkControlled &&
                               IsCoterminal(manager.ProbeController.Insertion.angles, convertedData.Angle));

                // If there is a matching manipulator, keep track of it
                if (matchingManipulator!= null)
                {
                    _demoManipulatorToData.Add(matchingManipulator, convertedData);
                }
            }
            
            print("Matching manipulators: "+_demoManipulatorToData.Count);
        }

        #region Helper functions

        private static bool IsCoterminal(Vector3 first, Vector3 second)
        {
            return Mathf.Abs(first.x - second.x) % 360 < 0.01f && Mathf.Abs(first.y - second.y) % 360 < 0.01f &&
                   Mathf.Abs(first.z - second.z) % 360 < 0.01f;
        }

        #endregion
    }
}