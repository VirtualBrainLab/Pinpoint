using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_ProbeCollider : MonoBehaviour
{
    [SerializeField] ProbeController pcontroller;
    private TrajectoryPlannerManager tpmanager;
    private Renderer colliderRenderer;

    private void Start()
    {
        tpmanager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();

        colliderRenderer = GetComponent<Renderer>();
    }

    private void OnMouseDown()
    {
        // If someone clicks on a probe, immediately make that the active probe and claim probe control
        tpmanager.SetActiveProbe(pcontroller);
        pcontroller.DragMovementClick();
    }

    private void OnMouseDrag()
    {
        pcontroller.DragMovementDrag();
    }

    private void OnMouseUp()
    {
        pcontroller.DragMovementRelease();
    }

    public void SetVisibility(bool enabled)
    {
        colliderRenderer.enabled = enabled;
    }
}
