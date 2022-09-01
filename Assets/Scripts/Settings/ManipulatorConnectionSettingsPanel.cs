using System;
using TMPro;
using TrajectoryPlanner;
using UnityEngine;

namespace Settings
{
    public class ManipulatorConnectionSettingsPanel : MonoBehaviour
    {
        #region Variables

        #region Components

        [SerializeField] private TMP_Text manipulatorIdText;
        [SerializeField] private TMP_Dropdown handednessDropdown;
        
        private TrajectoryPlannerManager _trajectoryPlannerManager;

        #endregion

        #region Properties

        private int _manipulatorId;

        #endregion

        #endregion

        #region Setup

        private void Awake()
        {
            _trajectoryPlannerManager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
        }

        #endregion

        #region Getters and Setters

        public void SetManipulatorId(int manipulatorId)
        {
            _manipulatorId = manipulatorId;
            manipulatorIdText.text = manipulatorId.ToString();
        }

        #endregion

        #region UI Function

        public void OnSetManipulatorHandedness()
        {
            if (handednessDropdown.value == 1)
            {
                _trajectoryPlannerManager.AddRightHandedManipulator(_manipulatorId);
            }
        }

        #endregion
    }
}