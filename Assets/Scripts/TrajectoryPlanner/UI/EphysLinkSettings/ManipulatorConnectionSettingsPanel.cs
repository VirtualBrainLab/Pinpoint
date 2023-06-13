using TMPro;
using TrajectoryPlanner.Probes;
using UnityEngine;

namespace TrajectoryPlanner.UI.EphysLinkSettings
{
    /// <summary>
    ///     Panel representing an available manipulator to connect to and its settings.
    /// </summary>
    public class ManipulatorConnectionSettingsPanel : MonoBehaviour
    {
        #region Properties

        private string _manipulatorId;
        public string ManipulatorId
        {
            set
            {
                _manipulatorId = value;
                _manipulatorIdText.text = value;
            }
        }

        #endregion
        #region Getters and Setters

        /// <summary>
        ///     Set the manipulator ID this panel is representing.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator this panel is representing</param>
        public void SetManipulatorId(string manipulatorId)
        {
            _manipulatorIdText.text = manipulatorId;
            // _manipulatorBehaviorController = ProbeManager.Instances
            //     .Find(manager => manager.ManipulatorBehaviorController.ManipulatorID == manipulatorId)
            //     .ManipulatorBehaviorController;
            // _handednessDropdown.value = _manipulatorBehaviorController.IsRightHanded ? 1 : 0;
        }

        #endregion

        #region UI Function

        /// <summary>
        ///     Handle changing manipulator's registered handedness on UI change.
        /// </summary>
        /// <param name="value">Selected index of the handedness options (0 = left handed, 1 = right handed)</param>
        public void OnManipulatorHandednessValueChanged(int value)
        {
            _manipulatorBehaviorController.IsRightHanded = value == 1;
        }

        #endregion

        #region Variables

        #region Components

        [SerializeField] private TMP_Text _manipulatorIdText;
        [SerializeField] private TMP_Dropdown _handednessDropdown;

        private ManipulatorBehaviorController _manipulatorBehaviorController;

        #endregion

        #endregion
    }
}