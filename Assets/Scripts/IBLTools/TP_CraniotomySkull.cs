using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_CraniotomySkull : MonoBehaviour
{
    [SerializeField] private TP_TrajectoryPlannerManager tpmanager;
    [SerializeField] private GameObject skullMeshGO;
    [SerializeField] private GameObject craniotomyGO;
    private Renderer skullRenderer;
    private float craniotomySize = 1f;

    private void Awake()
    {
        skullRenderer = skullMeshGO.GetComponent<Renderer>();
        tpmanager = GameObject.Find("main").GetComponent<TP_TrajectoryPlannerManager>();
    }

    public void UpdateVisibility()
    {
        craniotomyGO.transform.localScale = new Vector3(craniotomySize, 1f, craniotomySize);

        skullRenderer.material.SetVector("_CraniotomyPosition_0", craniotomyGO.transform.position);
        skullRenderer.material.SetVector("_CraniotomyUpAxis_0", craniotomyGO.transform.up);
        skullRenderer.material.SetFloat("_CraniotomySize_0", craniotomySize/2);
    }

    public void SetCraniotomyPosition(Vector3 apdvlr)
    {
        Vector3 newPos = Utils.apdvlr2World(apdvlr) - tpmanager.GetCenterOffset();
        newPos.y = 3.18f;
        craniotomyGO.transform.position = newPos;
        UpdateVisibility();
    }

    public void SetCraniotomySize(float size)
    {
        craniotomySize = size;
        UpdateVisibility();
    }
}
