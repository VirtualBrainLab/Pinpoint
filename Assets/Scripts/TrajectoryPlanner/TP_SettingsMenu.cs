using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Settings
{
    public class TP_SettingsMenu : MonoBehaviour
    {
        [FormerlySerializedAs("settingsMenuGO")] [SerializeField] private GameObject _settingsMenuGo;

        public void ToggleSettingsMenu()
        {
            if (!_settingsMenuGo.activeSelf)
                _settingsMenuGo.SetActive(true);
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