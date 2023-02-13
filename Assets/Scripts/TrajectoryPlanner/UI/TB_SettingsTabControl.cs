using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UITabs
{
    public class TB_SettingsTabControl : MonoBehaviour
    {
        [FormerlySerializedAs("menuParent")] [SerializeField] private GameObject _menuParent;

        public void UpdateMenuVisibility(int menuPosition)
        {
            for (int i = 0; i < _menuParent.transform.childCount; i++)
            {
                Transform childT = _menuParent.transform.GetChild(i);
                childT.gameObject.SetActive(i == menuPosition);
            }
        }
    }

}