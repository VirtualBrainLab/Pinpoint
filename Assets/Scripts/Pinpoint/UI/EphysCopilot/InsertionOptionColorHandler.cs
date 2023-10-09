using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrajectoryPlanner.UI.EphysCopilot
{
    public class InsertionOptionColorHandler : MonoBehaviour
    {
        #region Components

        [SerializeField] private Toggle _toggle;
        [SerializeField] private TMP_Text _text;

        #endregion

        // Start is called before the first frame update
        private void Start()
        {
            // Compute UUID extent index
            var textEndIndex = _text.text.LastIndexOf(": A", StringComparison.Ordinal);
            if (textEndIndex == -1) return;

            // Get the probe manager with this UUID (if it exists)
            var probeNameString = _text.text[..textEndIndex];
            throw new NotImplementedException();
            //var matchingManager = InsertionSelectionPanelHandler.TargetableProbeManagers.First(manager =>
            //    manager.name.Equals(probeNameString) || (manager.OverrideName?.Equals(probeNameString) ?? false));
            //if (!matchingManager) return;

            //// Set the toggle color to match the probe color
            //var colorBlockCopy = _toggle.colors;
            //colorBlockCopy.normalColor = matchingManager.Color;
            //colorBlockCopy.selectedColor = new Color(colorBlockCopy.normalColor.r * 0.9f,
            //    colorBlockCopy.normalColor.g * 0.9f, colorBlockCopy.normalColor.b * 0.9f);
            //_toggle.colors = colorBlockCopy;
        }
    }
}