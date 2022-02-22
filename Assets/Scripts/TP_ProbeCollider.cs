using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
        // ignore mouse clicks if we're over a UI element
        if (EventSystem.current.IsPointerOverGameObject())
            return;
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
