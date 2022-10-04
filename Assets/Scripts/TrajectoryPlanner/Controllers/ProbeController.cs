using System.Collections;
using System.Collections.Generic;
using TrajectoryPlanner;
using UnityEngine;

public abstract class ProbeController : MonoBehaviour
{
    private TrajectoryPlannerManager _tpmanager;
    public TrajectoryPlannerManager TPManager { get { return _tpmanager; } }

    private ProbeManager _probeManager;
    public ProbeManager ProbeManager { get { return _probeManager; } }

    private ProbeInsertion _insertion;
    public ProbeInsertion Insertion { get { return _insertion; } set { _insertion = value; } }

    public void Register(TrajectoryPlannerManager tpmanager, ProbeManager probeManager)
    {
        this._tpmanager = tpmanager;
        this._probeManager = probeManager;
    }

    public abstract Transform ProbeTipT { get; }

    public abstract (Vector3 tipCoordWorld, Vector3 tipUpWorld) GetTipWorld();

    public abstract (Vector3 startCoordWorld, Vector3 endCoordWorld) GetRecordingRegionWorld();
    public abstract (Vector3 startCoordWorld, Vector3 endCoordWorld) GetRecordingRegionWorld(Transform tipTransform);

    public abstract void ResetInsertion();

    public abstract void ResetPosition();

    public abstract void ResetAngles();

    public abstract void SetProbePosition();

    public abstract void SetProbePosition(Vector3 position);

    public abstract void SetProbePosition(Vector4 positionDepth);

    public abstract void SetProbePosition(ProbeInsertion localInsertion);

    public abstract void SetProbeAngles(Vector3 angles);
}
