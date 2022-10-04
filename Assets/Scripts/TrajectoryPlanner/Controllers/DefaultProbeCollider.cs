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

    private void OnDestroy()
    {
        //tpmanager.
    }

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
