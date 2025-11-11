using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TP_ToggleRigs : MonoBehaviour
{
    // Exposed the list of rigs
    [SerializeField] private List<string> _rigNames;
    [SerializeField] private List<GameObject> _rigGOs;

    public void ToggleRigVisibility(string rigName, bool active)
    {
        var rigIdx = _rigNames.IndexOf(rigName);

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
