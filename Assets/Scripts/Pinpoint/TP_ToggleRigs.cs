using System.Collections.Generic;
using UnityEngine;
using TrajectoryPlanner;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TP_ToggleRigs : MonoBehaviour
{
    [FormerlySerializedAs("tpmanager")] [SerializeField] private TrajectoryPlannerManager _tpmanager;

    // Exposed the list of rigs
    [FormerlySerializedAs("rigGOs")] [SerializeField] private List<GameObject> _rigGOs;

    // Exposed list of rig UI objects
    [SerializeField] private GameObject _rigUIParentGO;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < _rigGOs.Count; i++)
            if (PlayerPrefs.HasKey("rig" + i))
            {
                bool active = PlayerPrefs.GetInt("rig" + i) == 1;
                _rigGOs[i].SetActive(active);
                _rigUIParentGO.transform.GetChild(i).gameObject.GetComponent<Toggle>().SetIsOnWithoutNotify(active);
            }
            else
                _rigGOs[i].SetActive(false);
    }

    public void ToggleRigVisibility(int rigIdx)
    {
        _rigGOs[rigIdx].SetActive(!_rigGOs[rigIdx].activeSelf);
        Collider[] colliders = _rigGOs[rigIdx].transform.GetComponentsInChildren<Collider>();
        _tpmanager.UpdateRigColliders(colliders, _rigGOs[rigIdx].activeSelf);
        ColliderManager.CheckForCollisions();
    }

    private void OnApplicationQuit()
    {
        for (int i = 0; i < _rigGOs.Count; i++)
            PlayerPrefs.SetInt("rig" + i, _rigGOs[i].activeSelf ? 1 : 0);
    }
}
