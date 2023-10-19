using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TP_ToggleRigs : MonoBehaviour
{
    // Exposed the list of rigs
    [SerializeField] private List<GameObject> _rigGOs;

    // Exposed list of rig UI objects
    [SerializeField] private GameObject _rigUIParentGO;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < _rigGOs.Count; i++)
        {
            string rigKey = $"rig{i}";
            bool active = PlayerPrefs.HasKey(rigKey) && (PlayerPrefs.GetInt(rigKey, 0) == 1);

            _rigGOs[i].SetActive(active);
            _rigUIParentGO.transform.GetChild(i).gameObject.GetComponent<Toggle>().SetIsOnWithoutNotify(active);
        }
    }

    public void ToggleRigVisibility(int rigIdx)
    {
        bool active = _rigUIParentGO.transform.GetChild(rigIdx).GetComponent<Toggle>().isOn;

        _rigGOs[rigIdx].SetActive(active);

        Collider[] colliders = _rigGOs[rigIdx].transform.GetComponentsInChildren<Collider>();
        if (active)
            ColliderManager.AddRigColliderInstances(colliders);
        else
            ColliderManager.RemoveRigColliderInstances(colliders);
        ColliderManager.CheckForCollisions();

        PlayerPrefs.SetInt($"rig{rigIdx}", active ? 1 : 0);
    }
}
