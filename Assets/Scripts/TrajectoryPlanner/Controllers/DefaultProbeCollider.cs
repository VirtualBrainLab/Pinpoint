using UnityEngine;
using TrajectoryPlanner;
using UnityEngine.EventSystems;

public class DefaultProbeCollider : MonoBehaviour
{
    [SerializeField] private ProbeManager _probeManager;
    private TrajectoryPlannerManager _tpmanager;

    private void Start()
    {
        _tpmanager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
    }

    // [TODO] Refactor these into UnityEvents so that we don't need to have access to tpmanager!!

    private void OnMouseDown()
    {
        // If someone clicks on a probe, immediately make that the active probe and claim probe control
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        _tpmanager.SetActiveProbe(_probeManager);
        ((DefaultProbeController)_probeManager.GetProbeController()).DragMovementClick();
    }

    private void OnMouseDrag()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        ((DefaultProbeController)_probeManager.GetProbeController()).DragMovementDrag();
    }

    private void OnMouseUp()
    {
        ((DefaultProbeController)_probeManager.GetProbeController()).DragMovementRelease();
    }
}
