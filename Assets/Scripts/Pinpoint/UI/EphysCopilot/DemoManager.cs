using UnityEngine;
using UnityEngine.Serialization;

namespace Pinpoint.UI.EphysCopilot
{
    public class DemoManager : MonoBehaviour
    {
        #region Components

        // Existing UI for toggling.
        [SerializeField] private GameObject _canvasGameObject;
        // Demo UI
        
        #endregion

        #region UI Functions

        public void StartDemo()
        {
            // Hide existing UI.
            for (var i = 0; i < _canvasGameObject.transform.childCount; i++)
            {
                var child = _canvasGameObject.transform.GetChild(i).gameObject;
                if (child.name == "CopilotDemo") continue;
                child.SetActive(false);
            }
        }

        public void StopDemo()
        {
            // Show existing UI.
            for (var i = 0; i < _canvasGameObject.transform.childCount; i++)
            {
                var child = _canvasGameObject.transform.GetChild(i).gameObject;
                if (child.name == "CopilotDemo") continue;
                child.SetActive(true);
            }
        }

        #endregion
    }
}
