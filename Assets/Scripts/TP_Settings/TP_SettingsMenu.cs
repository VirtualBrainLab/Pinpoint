using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_SettingsMenu : MonoBehaviour
{
    [SerializeField] private GameObject settingsMenuGO;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
            settingsMenuGO.SetActive(!settingsMenuGO.activeSelf);
    }
}
