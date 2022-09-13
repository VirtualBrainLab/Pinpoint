using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Settings
{
    public class TB_SettingsTabControl : MonoBehaviour
    {
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

}