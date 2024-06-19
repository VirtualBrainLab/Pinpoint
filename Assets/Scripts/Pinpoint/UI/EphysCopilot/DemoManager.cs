using System.Collections.Generic;
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
        // Demo UI

        #endregion

        #region Properties

        private HashSet<GameObject> _existingUIGameObjects = new();

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
        }

        #endregion
    }
}
