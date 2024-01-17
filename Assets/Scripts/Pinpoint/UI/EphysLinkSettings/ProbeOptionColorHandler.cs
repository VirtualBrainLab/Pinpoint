using UnityEngine;
using UnityEngine.UI;

namespace Pinpoint.UI.EphysLinkSettings
{
    public class ProbeOptionColorHandler : MonoBehaviour
    {
        #region Components

        [SerializeField] private Toggle _toggle;
        [SerializeField] private Text _text;

        #endregion

        private void Start()
        {
            // Get the probe manager with this UUID (if it exists).
            var matchingManager = ProbeManager.Instances.Find(manager => manager.UUID == _text.text);
            if (!matchingManager) return;

            // Get a copy of the toggle's color block.
            var colorBlockCopy = _toggle.colors;
            colorBlockCopy.normalColor = matchingManager.Color;
            colorBlockCopy.selectedColor = new Color(colorBlockCopy.normalColor.r * 0.9f,
                colorBlockCopy.normalColor.g * 0.9f, colorBlockCopy.normalColor.b * 0.9f);
            _toggle.colors = colorBlockCopy;
        }
    }
}