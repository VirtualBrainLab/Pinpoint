using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TrajectoryPlanner;

public class TP_CraniotomySkull : MonoBehaviour
{
    [SerializeField] private TrajectoryPlannerManager tpmanager;
    [SerializeField] private GameObject skullMeshGO;
    [SerializeField] private GameObject craniotomyGO;
    private Renderer skullRenderer;
    private float craniotomySize = 1f;

    private void Awake()
    {
        skullRenderer = skullMeshGO.GetComponent<Renderer>();
        tpmanager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
    }

    public void UpdateVisibility()
    {
        craniotomyGO.transform.localScale = new Vector3(craniotomySize, 1f, craniotomySize);

        skullRenderer.material.SetVector("_CraniotomyPosition_0", craniotomyGO.transform.position);
        skullRenderer.material.SetVector("_CraniotomyUpAxis_0", craniotomyGO.transform.up);
        skullRenderer.material.SetFloat("_CraniotomySize_0", craniotomySize / 2);
    }

    public void SetCraniotomyPosition(Vector3 apmldv)
    {
        Vector3 world = Utils.apmldv2world(apmldv) - tpmanager.GetCenterOffset();

        craniotomyGO.transform.position = world;

        UpdateVisibility();
    }

    public void SetCraniotomySize(float sizeUM)
    {
        craniotomySize = sizeUM;
        UpdateVisibility();
    }
}
