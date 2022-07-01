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
        if (craniotomyGO!=null)
        {
            craniotomyGO.transform.localScale = new Vector3(craniotomySize, 1f, craniotomySize);

            skullRenderer.material.SetVector("_CraniotomyPosition_0", craniotomyGO.transform.position);
            skullRenderer.material.SetVector("_CraniotomyUpAxis_0", craniotomyGO.transform.up);
            skullRenderer.material.SetFloat("_CraniotomySize_0", craniotomySize / 2);
        }
    }

    public void SetCraniotomyPosition(Vector3 bregmaRelative)
    {
        // We get the position in bregma-relative space in um, we need to convert this to world
        // note the +1.2 on the AP axis is because bregma is at 5.4, not halfway through the CCF volume at 6.6
        Vector3 world = new Vector3(-bregmaRelative.z/1000f, 3.18f, bregmaRelative.x / 1000f + 1.2f);

        if (craniotomyGO != null)
            craniotomyGO.transform.position = world;

        UpdateVisibility();
    }

    public void SetCraniotomySize(float sizeUM)
    {
        craniotomySize = sizeUM / 1000f;
        UpdateVisibility();
    }
}
