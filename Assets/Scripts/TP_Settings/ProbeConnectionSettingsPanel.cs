using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TP_Settings
{
    public class ProbeConnectionSettingsPanel : MonoBehaviour
    {
        #region Variables

        #region Properties

        private int _probeId;
        private int _manipulatorId;
        private Vector3 angles;
        private Vector4 _bregmaOffset;

        #endregion

        #region Components

        [SerializeField] private TMP_Text probeIdText;
        [SerializeField] private TMP_Dropdown manipulatorIdDropdown;
        [SerializeField] private TMP_Text connectButtonText;
        [SerializeField] private TMP_InputField phiInputField;
        [SerializeField] private TMP_InputField thetaInputField;
        [SerializeField] private TMP_InputField spinInputField;
        [SerializeField] private TMP_InputField xInputField;
        [SerializeField] private TMP_InputField yInputField;
        [SerializeField] private TMP_InputField zInputField;
        [SerializeField] private TMP_InputField dInputField;

        #endregion

        #endregion

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }

        #region Property Getters and Setters

        public int GetProbeId()
        {
            return _probeId;
        }

        public void SetProbeId(int id)
        {
            _probeId = id;
        }

        #endregion

        #region Component Accessors

        public void SetManipulatorIdDropdownOptions(List<string> idOptions)
        {
            manipulatorIdDropdown.ClearOptions();
            manipulatorIdDropdown.AddOptions(idOptions);

            // Select the option corresponding to the current manipulator id
            var indexOfId = _manipulatorId == 0 ? 0 : Math.Max(0, idOptions.IndexOf(_manipulatorId.ToString()));
            manipulatorIdDropdown.SetValueWithoutNotify(indexOfId);
        }

        #endregion
    }
}