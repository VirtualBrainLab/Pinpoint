using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pinpoint.UI.EphysCopilot
{
    public class DemoManager : MonoBehaviour
    {
        #region Components

        // Existing UI for toggling.
        [SerializeField]
        private GameObject _canvasGameObject;

        // Demo UI.

        // Camera.
        [SerializeField]
        private BrainCameraController _mainCamera;

        #endregion

        #region Properties

        private readonly HashSet<GameObject> _existingUIGameObjects = new();

        private struct DemoData
        {
            public string Manipulator1Id;
            public Positions Manipulator1Positions;

            public string Manipulator2Id;
            public Positions Manipulator2Positions;

            public string Manipulator3Id;
            public Positions Manipulator3Positions;

            [Serializable]
            public struct Positions
            {
                public Vector4 Home;
                public Vector4 Bregma;
                public Vector4 Target;
            }
        }

        private DemoData _demoData;

        #endregion


        #region Unity

        private void Update()
        {
            _mainCamera.transform.Rotate(0, 5 * Time.deltaTime, 0);
        }

        #endregion

        #region UI Functions

        public void StartDemo()
        {
            // Hide existing UI and show demo UI.
            for (var i = 0; i < _canvasGameObject.transform.childCount; i++)
            {
                var child = _canvasGameObject.transform.GetChild(i).gameObject;

                // Ignore the demo UI.
                if (child.name == "CopilotDemo")
                    continue;
                // Ignore inactive UI.
                if (!child.activeSelf)
                {
                    continue;
                }

                // Hide the UI.
                child.SetActive(false);

                // Add to the list of existing UI.
                _existingUIGameObjects.Add(child);
            }
            gameObject.SetActive(true);

            // Setup camera.
            _mainCamera.SetZoom(10);
            _mainCamera.transform.rotation = Quaternion.Euler(180, -180, -180);

            // Read demo data.
            var demoDataPath = Path.Combine(Application.streamingAssetsPath, "DemoData.json");
            var demoDataJson = File.ReadAllText(demoDataPath);
            _demoData = JsonUtility.FromJson<DemoData>(demoDataJson);
        }

        public void StopDemo()
        {
            // Show existing UI and hide demo UI.
            foreach (var existingUIGameObject in _existingUIGameObjects)
            {
                existingUIGameObject.SetActive(true);
            }
            gameObject.SetActive(false);
            _existingUIGameObjects.Clear();

            // Reset camera.
            _mainCamera.SetZoom(5);
        }

        #endregion
    }
}
