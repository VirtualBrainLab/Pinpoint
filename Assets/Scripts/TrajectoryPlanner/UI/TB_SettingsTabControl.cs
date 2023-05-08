using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UITabs
{
    public class TB_SettingsTabControl : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _webglDisabledFeaturesGOs;

        [FormerlySerializedAs("menuParent")] [SerializeField] private GameObject _menuParent;

#if UNITY_WEBGL && !UNITY_EDITOR
        private void OnEnable()
        {
            foreach (GameObject go in _webglDisabledFeaturesGOs)
                go.SetActive(false);
        }
#endif

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