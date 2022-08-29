using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TB_SettingsTabControl : MonoBehaviour
{
    [SerializeField] private List<GameObject> menuGOs;
    [SerializeField] private GameObject menuParent;

    public void UpdateMenuVisibility(int menuPosition)
    {
        for (int i = 0; i < menuParent.transform.childCount; i++)
        {
            Transform childT = menuParent.transform.GetChild(i);
            childT.gameObject.SetActive(i == menuPosition);
        }
    }
}
