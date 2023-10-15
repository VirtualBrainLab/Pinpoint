using UnityEngine;
using UnityEngine.Events;
using CoordinateSpaces;
using CoordinateTransforms;
using BrainAtlas;
using BrainAtlas.CoordinateSystems;

public abstract class ProbeController : MonoBehaviour
{
    public ProbeManager ProbeManager { get; private set; }

    public ProbeInsertion Insertion { get; set; }

    public abstract string XAxisStr { get; }
    public abstract string YAxisStr { get; }
    public abstract string ZAxisStr { get; }

    public void Register(ProbeManager probeManager)
    {
        ProbeManager = probeManager;
    }

    public UnityEvent MovedThisFrameEvent;
    public UnityEvent FinishedMovingEvent;

    public abstract bool Locked { get; }
    public abstract Vector4 UnlockedDir { get; set; }
    public abstract Vector3 UnlockedRot { get; set; }
    public bool ManipulatorManualControl;
    public bool ManipulatorKeyboardMoveInProgress;

    public abstract Transform ProbeTipT { get; }

    public abstract (Vector3 tipCoordWorldU, Vector3 tipRightWorldU, Vector3 tipUpWorldU, Vector3 tipForwardWorldU) GetTipWorldU();

    public abstract void ToggleControllerLock();

    public abstract void SetControllerLock(bool locked);
    public abstract void ResetInsertion();

    public abstract void ResetPosition();

    public abstract void ResetAngles();

    public abstract void SetProbePosition();

    public abstract void SetProbePosition(Vector3 position);

    public abstract void SetProbePosition(Vector4 positionDepth);

    public abstract void SetProbeAngles(Vector3 angles);

    /// <summary>
    /// Override the current ReferenceAtlas and AtlasTransform with new ones.
    /// Make sure to translate the probe position into the new space appropriately.
    /// </summary>
    /// <param name="atlas"></param>
    /// <param name="transform"></param>
    public void SetSpaceTransform(CoordinateSpace atlas, CoordinateTransform transform)
    {
        // Covnert the tip coordinate into the new space
        var tipData = GetTipWorldU();
        Vector3 tipCoordNewSpace = transform.U2T(atlas.World2Space(tipData.tipCoordWorldU));
        Insertion.apmldv = tipCoordNewSpace;
        // Set the transforms
        Insertion.CoordinateSpace = atlas;
        Insertion.CoordinateTransform = transform;
        // Set the probe position
        SetProbePosition();
    }


}
