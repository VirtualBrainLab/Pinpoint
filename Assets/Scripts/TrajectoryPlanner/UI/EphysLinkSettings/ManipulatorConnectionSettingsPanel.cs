using TMPro;
using UnityEngine;

namespace TrajectoryPlanner.UI.EphysLinkSettings
{
    /// <summary>
    ///     Panel representing an available manipulator to connect to and its settings.
    /// </summary>
    public class ManipulatorConnectionSettingsPanel : MonoBehaviour
    {

        #region Getters and Setters

        /// <summary>
        ///     Set the manipulator ID this panel is representing.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator this panel is representing</param>
        public void SetManipulatorId(string manipulatorId)
        {
            _manipulatorId = manipulatorId;
            _manipulatorIdText.text = manipulatorId;
            _handednessDropdown.value = ProbeManager.RightHandedManipulatorIDs.Contains(manipulatorId) ? 1 : 0;
        }

        #endregion

        #region UI Function

        /// <summary>
        ///     Handle changing manipulator's registered handedness on UI change.
        /// </summary>
        /// <param name="value">Selected index of the handedness options (0 = left handed, 1 = right handed)</param>
        public void OnManipulatorHandednessValueChanged(int value)
        {
            if (value == 1)
                ProbeManager.RightHandedManipulatorIDs.Add(_manipulatorId);
            else
                ProbeManager.RightHandedManipulatorIDs.Remove(_manipulatorId);
            
            // Save changes
            Settings.SaveRightHandedManipulatorIds(ProbeManager.RightHandedManipulatorIDs);
        }

        #endregion

        #region Variables

        #region Components

        [SerializeField] private TMP_Text _manipulatorIdText;
        [SerializeField] private TMP_Dropdown _handednessDropdown;

        #endregion

        #region Properties

        private string _manipulatorId;

        #endregion

        #endregion
    }
}