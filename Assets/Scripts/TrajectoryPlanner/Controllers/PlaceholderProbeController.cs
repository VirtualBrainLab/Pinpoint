using System;
using System.Collections;
using System.Collections.Generic;
using TrajectoryPlanner;
using UnityEngine;
using UnityEngine.EventSystems;
using CoordinateSpaces;

public class PlaceholderProbeController : ProbeController
{
    #region Defaults
    // in ap/ml/dv
    private Vector3 defaultStart = Vector3.zero; // new Vector3(5.4f, 5.7f, 0.332f);
    private Vector2 defaultAngles = new Vector2(-90f, 0f); // 0 phi is forward, default theta is 90 degrees down from horizontal, but internally this is a value of 0f
    #endregion

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    // References
    [SerializeField] private Transform _probeTipT;
    public override Transform ProbeTipT { get { return _probeTipT; } }

    private void Awake()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        Insertion = new ProbeInsertion(defaultStart, defaultAngles, new CCFSpace(), new CoordinateTransforms.CCFTransform());
    }

    /// <summary>
    /// Put this probe back at Bregma
    /// </summary>
    public override void ResetInsertion()
    {
        ResetPosition();
        ResetAngles();
        SetProbePosition();
    }

    public override void ResetPosition()
    {
        Insertion.apmldv = defaultStart;
    }

    public override void ResetAngles()
    {
        Insertion.angles = defaultAngles;
    }


    #region Set Probe pos/angles

    /// <summary>
    /// Set the probe position to the current apml/depth/phi/theta/spin values
    /// </summary>
    public override void SetProbePosition()
    {
        SetProbePosition(Insertion);
    }


    public override void SetProbePosition(Vector3 position)
    {
        // Not implemented
    }

    public override void SetProbePosition(Vector4 positionDepth)
    {
        // Not implemented
    }

    public override void SetProbeAngles(Vector3 angles)
    {
        // Not implemented
    }

    /// <summary>
    /// Set the position of the probe to match a ProbeInsertion object in CCF coordinates
    /// </summary>
    /// <param name="localInsertion">new insertion position</param>
    public override void SetProbePosition(ProbeInsertion localInsertion)
    {
        // Reset everything
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        // Manually adjust the coordinates and rotation
        transform.position += localInsertion.PositionWorld();
        transform.RotateAround(_probeTipT.position, transform.up, localInsertion.phi);
        transform.RotateAround(_probeTipT.position, transform.forward, localInsertion.theta);
        transform.RotateAround(_probeTipT.position, _probeTipT.up, localInsertion.spin);

        // save the data
        Insertion = localInsertion;
    }

    #endregion

    #region Getters

    /// <summary>
    /// Return the tip coordinates in **un-transformed** world coordinates
    /// </summary>
    /// <returns></returns>
    public override (Vector3 tipCoordWorld, Vector3 tipUpWorld) GetTipWorld()
    {
        // not implemented
        return (Vector3.zero, Vector3.zero);
    }

    public override (Vector3 startCoordWorld, Vector3 endCoordWorld) GetRecordingRegionWorld()
    {
        return (Vector3.zero, Vector3.zero);
    }

    public override (Vector3 startCoordWorld, Vector3 endCoordWorld) GetRecordingRegionWorld(Transform tipTransform)
    {
        return (Vector3.zero, Vector3.zero);
    }

    /// <summary>
    /// Return the height of the bottom in mm and the total height
    /// </summary>
    /// <returns>float array [0]=bottom, [1]=height</returns>
    public (float, float) GetRecordingRegionHeight()
    {
        return (0, 0);
    }

    #endregion

}
