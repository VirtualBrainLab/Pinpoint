using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Settings
{
    public class TP_SettingsMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _settingsMenuGO;

        public void ToggleSettingsMenu()
        {
            if (!_settingsMenuGO.activeSelf)
                _settingsMenuGO.SetActive(true);
            else
            {
                // if the settings menu is active, we want to make sure the user isn't typing before we close the menu
                foreach (TMP_InputField inputField in transform.GetComponentsInChildren<TMP_InputField>())
                {
                    if (inputField.isFocused)
                        return;
                }
                _settingsMenuGO.SetActive(false);
            }
        }
    }

}