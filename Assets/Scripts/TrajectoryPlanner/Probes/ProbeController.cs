using UnityEngine;
using UnityEngine.Events;

public abstract class ProbeController : MonoBehaviour
{
    public ProbeManager ProbeManager { get; private set; }

    public ProbeInsertion Insertion { get; set; }

    public void Register(ProbeManager probeManager)
    {
        ProbeManager = probeManager;
    }

    public UnityEvent MovedThisFrameEvent;
    public UnityEvent FinishedMovingEvent;

    public bool Locked;

    public abstract Transform ProbeTipT { get; }

    public abstract (Vector3 tipCoordWorldU, Vector3 tipUpWorldU, Vector3 tipForwardWorldU) GetTipWorldU();

    public abstract void ResetInsertion();

    public abstract void ResetPosition();

    public abstract void ResetAngles();

    public abstract void SetProbePosition();

    public abstract void SetProbePosition(Vector3 position);

    public abstract void SetProbePosition(Vector4 positionDepth);

    public abstract void SetProbePosition(ProbeInsertion localInsertion);

    public abstract void SetProbeAngles(Vector3 angles);


}
