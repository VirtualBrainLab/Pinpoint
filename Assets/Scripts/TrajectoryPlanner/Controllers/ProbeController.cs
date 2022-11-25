using System.Collections;
using System.Collections.Generic;
using TrajectoryPlanner;
using UnityEngine;

public abstract class ProbeController : MonoBehaviour
{
    public TrajectoryPlannerManager TPManager { get; private set; }
    public ProbeManager ProbeManager { get; private set; }
    public ProbeInsertion Insertion { get; set; }

    public void Register(TrajectoryPlannerManager tpmanager, ProbeManager probeManager)
    {
        TPManager = tpmanager;
        ProbeManager = probeManager;
    }

    public abstract Transform ProbeTipT { get; }

    public abstract (Vector3 tipCoordWorld, Vector3 tipUpWorld, Vector3 tipForwardWorld) GetTipWorldU();

    public abstract (Vector3 startCoordWorld, Vector3 endCoordWorld) GetRecordingRegionWorld();
    public abstract (Vector3 startCoordWorld, Vector3 endCoordWorld) GetRecordingRegionWorld(Transform tipTransform);

    public abstract float GetProbeDepth();

    public abstract void ResetInsertion();

    public abstract void ResetPosition();

    public abstract void ResetAngles();

    public abstract void SetProbePosition();

    public abstract void SetProbePosition(Vector3 position);

    public abstract void SetProbePosition(Vector4 positionDepth);

    public abstract void SetProbePosition(ProbeInsertion localInsertion);

    public abstract void SetProbeAngles(Vector3 angles);
}
