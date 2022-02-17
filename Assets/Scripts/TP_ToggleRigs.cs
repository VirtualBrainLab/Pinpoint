using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TP_ToggleRigs : MonoBehaviour
{
    [SerializeField] TrajectoryPlannerManager tpmanager;
    [SerializeField] TMP_Dropdown dropdown;

    // Widefield rig
    [SerializeField] List<GameObject> wf_gameObjects;
    [SerializeField] List<Collider> wf_colliders;

    // Skull rig
    [SerializeField] GameObject skullGO;

    private bool wfVisible;

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject go in wf_gameObjects)
            go.SetActive(false);
    }

    public void ToggleVisibility()
    {
        switch (dropdown.value)
        {
            case 1:
                RigVisibility_WF(true);
                break;
            case 2:
                RigVisibility_Skull(true);
                break;
            default:
                RigVisibility_WF(false);
                RigVisibility_Skull(false);
                break;
        }
    }

    private void RigVisibility_Skull(bool v)
    {
        skullGO.SetActive(v);
    }

    private void RigVisibility_WF(bool visibility) {
        wfVisible = visibility;
        foreach (GameObject go in wf_gameObjects)
            go.SetActive(wfVisible);
        if (wfVisible)
            tpmanager.UpdateRigColliders(wf_colliders, true);
        else
            tpmanager.UpdateRigColliders(wf_colliders, false);
    }
}
