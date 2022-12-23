using UnityEngine;
using TrajectoryPlanner;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.Events;

public class DefaultProbeCollider : MonoBehaviour
{
    [FormerlySerializedAs("probeManager")] [SerializeField] private ProbeManager _probeManager;

    public UnityEvent OnMouseDownEvent;
    public UnityEvent OnMouseDragEvent;
    public UnityEvent OnMouseUpEvent;

    private void OnMouseDown()
    {
        // If someone clicks on a probe, immediately make that the active probe and claim probe control
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        OnMouseDownEvent.Invoke();
        //tpmanager.SetActiveProbe(_probeManager);
        //((DefaultProbeController)_probeManager.GetProbeController()).DragMovementClick();
    }

    private void OnMouseDrag()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        OnMouseDragEvent.Invoke();
        //((DefaultProbeController)_probeManager.GetProbeController()).DragMovementDrag();
    }

    private void OnMouseUp()
    {
        OnMouseUpEvent.Invoke();
        //((DefaultProbeController)_probeManager.GetProbeController()).DragMovementRelease();
    }
}
