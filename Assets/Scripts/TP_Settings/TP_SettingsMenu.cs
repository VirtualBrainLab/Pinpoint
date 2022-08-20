using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TP_SettingsMenu : MonoBehaviour
{
    [SerializeField] private GameObject settingsMenuGO;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (!settingsMenuGO.activeSelf)
                settingsMenuGO.SetActive(true);
            else
            {
                // if the settings menu is active, we want to make sure the user isn't typing before we close the menu
                foreach (TMP_InputField inputField in transform.GetComponentsInChildren<TMP_InputField>())
                {
                    if (inputField.isFocused)
                        return;
                }
                settingsMenuGO.SetActive(false);
            }
        }
    }
}
