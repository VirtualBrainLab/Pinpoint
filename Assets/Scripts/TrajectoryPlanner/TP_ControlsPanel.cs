using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_ControlsPanel : MonoBehaviour
{
    [SerializeField] private GameObject childGO;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            childGO.SetActive(!childGO.activeSelf);
        }
    }
}
