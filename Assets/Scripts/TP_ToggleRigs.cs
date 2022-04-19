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
    [SerializeField] TMP_Dropdown dropdown;

    // Widefield rig
    [SerializeField] List<GameObject> wf_gameObjects;
    [SerializeField] List<Collider> wf_colliders;

    // Skull rig
    private GameObject skullGO;

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
                RigVisibility_Skull(false);
                break;
            case 2:
                RigVisibility_WF(false);
                RigVisibility_Skull(true);
                break;
            default:
                RigVisibility_WF(false);
                RigVisibility_Skull(false);
                break;
        }
    }

    private void RigVisibility_Skull(bool state)
    {
        if (skullGO == null)
            StartCoroutine(LoadSkull());
        else
            skullGO.SetActive(state);
    }

    private IEnumerator LoadSkull()
    {
        Debug.Log("Loading skull");
        AsyncOperationHandle<GameObject> skullHandle = Addressables.LoadAssetAsync<GameObject>("3D/MouseSkullPrefab");

        skullHandle.Completed += SkullLoaded;

        yield return skullHandle;

        Debug.Log("also got here");
        skullGO = Instantiate(skullHandle.Result, transform);
        Debug.Log(skullGO);
    }

    private void SkullLoaded(AsyncOperationHandle<GameObject> skullHandle)
    {
        Debug.Log("Done");
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
