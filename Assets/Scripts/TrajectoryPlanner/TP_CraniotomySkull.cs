using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TrajectoryPlanner;

public class TP_CraniotomySkull : MonoBehaviour
{
    [SerializeField] private TrajectoryPlannerManager _tpmanager;
    [SerializeField] private GameObject _skullMeshGO;
    [SerializeField] private GameObject _craniotomyGO;
    private Renderer _skullRenderer;
    private float _craniotomySize = 1f;

    private void Awake()
    {
        _skullRenderer = _skullMeshGO.GetComponent<Renderer>();
        _tpmanager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
    }

    public void UpdateVisibility()
    {
        _craniotomyGO.transform.localScale = new Vector3(_craniotomySize, 1f, _craniotomySize);

        if (_skullRenderer != null)
        {
            _skullRenderer.material.SetVector("_CraniotomyPosition_0", _craniotomyGO.transform.position);
            _skullRenderer.material.SetVector("_CraniotomyUpAxis_0", _craniotomyGO.transform.up);
            _skullRenderer.material.SetFloat("_CraniotomySize_0", _craniotomySize / 2);
        }
    }

    public void SetCraniotomyPosition(Vector3 apmldv)
    {
        Vector3 world = _tpmanager.GetCoordinateSpace().Space2World(apmldv);
        //Vector3 world = Utils.apmldv2world(apmldv) - tpmanager.GetCenterOffset();

        _craniotomyGO.transform.position = world;

        UpdateVisibility();
    }

    public void SetCraniotomySize(float sizeUM)
    {
        _craniotomySize = sizeUM;
        UpdateVisibility();
    }
}
