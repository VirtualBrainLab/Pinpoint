using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TP_ToggleRigs : MonoBehaviour
{
    [SerializeField] TP_TrajectoryPlannerManager tpmanager;

    // Exposed the list of rigs
    [SerializeField] List<GameObject> rigGOs;

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject go in rigGOs)
            go.SetActive(false);
    }

    public void ToggleRigVisibility(int rigIdx)
    {
        rigGOs[rigIdx].SetActive(!rigGOs[rigIdx].activeSelf);
        Collider[] colliders = rigGOs[rigIdx].transform.GetComponentsInChildren<Collider>();
        tpmanager.UpdateRigColliders(colliders, rigGOs[rigIdx].activeSelf);
    }
}
