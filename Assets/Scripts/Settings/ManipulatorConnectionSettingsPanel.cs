using TMPro;
using TrajectoryPlanner;
using UnityEngine;

namespace Settings
{
    /// <summary>
    ///     Panel representing an available manipulator to connect to and its settings.
    /// </summary>
    public class ManipulatorConnectionSettingsPanel : MonoBehaviour
    {
        #region Unity

        private void Awake()
        {
            _trajectoryPlannerManager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
        }

        #endregion

        #region Getters and Setters

        /// <summary>
        ///     Set the manipulator ID this panel is representing.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator this panel is representing</param>
        public void SetManipulatorId(string manipulatorId)
        {
            _manipulatorId = manipulatorId;
            manipulatorIdText.text = manipulatorId.ToString();
            handednessDropdown.value = _trajectoryPlannerManager.IsManipulatorRightHanded(manipulatorId) ? 1 : 0;
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
                _trajectoryPlannerManager.AddRightHandedManipulator(_manipulatorId);
            else
                _trajectoryPlannerManager.RemoveRightHandedManipulator(_manipulatorId);
        }

        #endregion

        #region Variables

        #region Components

        [SerializeField] private TMP_Text manipulatorIdText;
        [SerializeField] private TMP_Dropdown handednessDropdown;

        private TrajectoryPlannerManager _trajectoryPlannerManager;

        #endregion

        #region Properties

        private string _manipulatorId;

        #endregion

        #endregion
    }
}