using UnityEngine;
using UnityEngine.Events;
using CoordinateSpaces;
using CoordinateTransforms;

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

    /// <summary>
    /// Override the current CoordinateSpace and CoordinateTransform with new ones.
    /// Make sure to translate the probe position into the new space appropriately.
    /// </summary>
    /// <param name="space"></param>
    /// <param name="transform"></param>
    public void SetSpaceTransform(CoordinateSpace space, CoordinateTransform transform)
    {
        // Covnert the tip coordinate into the new space
        var tipData = GetTipWorldU();
        Vector3 tipCoordNewSpace = transform.Space2Transform(space.World2Space(tipData.tipCoordWorldU));
        Insertion.apmldv = tipCoordNewSpace;
        // Set the transforms
        Insertion.CoordinateSpace = space;
        Insertion.CoordinateTransform = transform;
        // Set the probe position
        SetProbePosition();
    }

    public abstract void SetProbeAngles(Vector3 angles);


}
