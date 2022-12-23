using System.Collections.Generic;
using UnityEngine;
using TrajectoryPlanner;
using UnityEngine.Serialization;

public class TP_ToggleRigs : MonoBehaviour
{
    [FormerlySerializedAs("tpmanager")] [SerializeField] private TrajectoryPlannerManager _tpmanager;

    // Exposed the list of rigs
    [FormerlySerializedAs("rigGOs")] [SerializeField] private List<GameObject> _rigGOs;

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject go in _rigGOs)
            go.SetActive(false);
    }

    public void ToggleRigVisibility(int rigIdx)
    {
        _rigGOs[rigIdx].SetActive(!_rigGOs[rigIdx].activeSelf);
        Collider[] colliders = _rigGOs[rigIdx].transform.GetComponentsInChildren<Collider>();
        _tpmanager.UpdateRigColliders(colliders, _rigGOs[rigIdx].activeSelf);
        _tpmanager.GetActiveProbeManager().CheckCollisions(ColliderManager.InactiveColliderInstances);
    }
}
