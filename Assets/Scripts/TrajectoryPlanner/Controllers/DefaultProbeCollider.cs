using UnityEngine;
using TrajectoryPlanner;
using UnityEngine.EventSystems;

public class DefaultProbeCollider : MonoBehaviour
{
    [SerializeField] ProbeManager probeManager;
    private TrajectoryPlannerManager tpmanager;

    private void Start()
    {
        tpmanager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
    }

    // [TODO] Refactor these into UnityEvents so that we don't need to have access to tpmanager!!

    private void OnMouseDown()
    {
        // If someone clicks on a probe, immediately make that the active probe and claim probe control
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        tpmanager.SetActiveProbe(probeManager);
        ((DefaultProbeController)probeManager.GetProbeController()).DragMovementClick();
    }

    private void OnMouseDrag()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        ((DefaultProbeController)probeManager.GetProbeController()).DragMovementDrag();
    }

    private void OnMouseUp()
    {
        ((DefaultProbeController)probeManager.GetProbeController()).DragMovementRelease();
    }
}
