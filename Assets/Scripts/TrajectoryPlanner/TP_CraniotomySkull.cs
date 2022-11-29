using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TrajectoryPlanner;
using UnityEngine.Serialization;

public class TP_CraniotomySkull : MonoBehaviour
{
    [FormerlySerializedAs("tpmanager")] [SerializeField] private TrajectoryPlannerManager _tpmanager;
    [FormerlySerializedAs("skullMeshGO")] [SerializeField] private GameObject _skullMeshGo;
    [FormerlySerializedAs("craniotomyGO")] [SerializeField] private GameObject _craniotomyGo;
    private Renderer skullRenderer;
    private float craniotomySize = 1f;

    private void Awake()
    {
        skullRenderer = _skullMeshGo.GetComponent<Renderer>();
        _tpmanager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
    }

    public void UpdateVisibility()
    {
        _craniotomyGo.transform.localScale = new Vector3(craniotomySize, 1f, craniotomySize);

        if (skullRenderer != null)
        {
            skullRenderer.material.SetVector("_CraniotomyPosition_0", _craniotomyGo.transform.position);
            skullRenderer.material.SetVector("_CraniotomyUpAxis_0", _craniotomyGo.transform.up);
            skullRenderer.material.SetFloat("_CraniotomySize_0", craniotomySize / 2);
        }
    }

    public void SetCraniotomyPosition(Vector3 apmldv)
    {
        Vector3 world = _tpmanager.GetCoordinateSpace().Space2World(apmldv);
        //Vector3 world = Utils.apmldv2world(apmldv) - tpmanager.GetCenterOffset();

        _craniotomyGo.transform.position = world;

        UpdateVisibility();
    }

    public void SetCraniotomySize(float sizeUM)
    {
        craniotomySize = sizeUM;
        UpdateVisibility();
    }
}
