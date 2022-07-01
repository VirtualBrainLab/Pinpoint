using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TP_ProbeCollider : MonoBehaviour
{
    [SerializeField] TP_ProbeController pcontroller;
    private TP_TrajectoryPlannerManager tpmanager;

    private void Start()
    {
        tpmanager = GameObject.Find("main").GetComponent<TP_TrajectoryPlannerManager>();
    }

    private void OnDestroy()
    {
        //tpmanager.
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
}
