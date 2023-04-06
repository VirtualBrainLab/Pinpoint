using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace UITabs
{
    public class TP_SettingsMenu : MonoBehaviour
    {
        [FormerlySerializedAs("settingsMenuGO")] [SerializeField] private GameObject _settingsMenuGo;
        [SerializeField] private GameObject _helpText;

        private const string HELP_KEY = "hide-help-at-startup";

        private void Awake()
        {
            if (PlayerPrefs.HasKey(HELP_KEY) && PlayerPrefs.GetInt(HELP_KEY) == 1)
                _helpText.SetActive(false);
        }

        public void ToggleSettingsMenu()
        {
            if (!_settingsMenuGo.activeSelf)
            {
                // Hide the help text
                _settingsMenuGo.SetActive(true);

                if (_helpText.activeSelf)
                {
                    _helpText.SetActive(false);
                    PlayerPrefs.SetInt(HELP_KEY, 1);
                }
            }
            else
            {
                // if the settings menu is active, we want to make sure the user isn't typing before we close the menu
                foreach (TMP_InputField inputField in transform.GetComponentsInChildren<TMP_InputField>())
                {
                    if (inputField.isFocused)
                        return;
                }
                _settingsMenuGo.SetActive(false);
            }
        }
    }

}