using System;
using BrainAtlas;
using CoordinateTransforms;
using UnityEngine;

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

    public override string XAxisStr { get { return "AP"; } }

    public override string YAxisStr { get { return "ML"; } }

    public override string ZAxisStr { get { return "DV"; } }

    public override Vector4 UnlockedDir { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public override Vector3 UnlockedRot { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override bool Locked { get { return false; } }

    private void Awake()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        Insertion = new ProbeInsertion(defaultStart, defaultAngles, BrainAtlasManager.ActiveReferenceAtlas.AtlasSpace.Name, BrainAtlasManager.ActiveAtlasTransform.Name);
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
        Insertion.APMLDV = defaultStart;
    }

    public override void ResetAngles()
    {
        Insertion.Angles = defaultAngles;
    }


    #region Set Probe pos/angles

    /// <summary>
    /// Set the probe position to the current apml/depth/phi/theta/spin values
    /// </summary>
    public override void SetProbePosition()
    {
        SetProbePositionHelper();
    }

    public override void SetProbePosition(Vector3 position)
    {
        Insertion.APMLDV = position;
        SetProbePosition();
    }

    public override void SetProbePosition(Vector4 positionDepth)
    {
        throw new NotImplementedException("No depth in placeholder probe");
    }

    public override void SetProbeAngles(Vector3 angles)
    {
        Insertion.Angles = angles;
        SetProbePosition();
    }

    /// <summary>
    /// Set the position of the probe to match a ProbeInsertion object in CCF coordinates
    /// </summary>
    /// <param name="localInsertion">new insertion position</param>
    private void SetProbePositionHelper()
    {
        // Reset everything
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        // Manually adjust the coordinates and rotation
        transform.position += Insertion.PositionWorldT();
        transform.RotateAround(_probeTipT.position, transform.up, Insertion.Yaw);
        transform.RotateAround(_probeTipT.position, transform.forward, Insertion.Pitch);
        transform.RotateAround(_probeTipT.position, _probeTipT.up, Insertion.Roll);
    }

    #endregion

    #region Getters

    /// <summary>
    /// Return the tip coordinates in **un-transformed** world coordinates
    /// </summary>
    /// <returns></returns>
    public override (Vector3 tipCoordWorldU, Vector3 tipRightWorldU, Vector3 tipUpWorldU, Vector3 tipForwardWorldU) GetTipWorldU()
    {
        // not implemented
        return (Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
    }

    /// <summary>
    /// Return the height of the bottom in mm and the total height
    /// </summary>
    /// <returns>float array [0]=bottom, [1]=height</returns>
    public (float, float) GetRecordingRegionHeight()
    {
        return (0, 0);
    }

    public override void ToggleControllerLock()
    {
        throw new NotImplementedException();
    }

    public override void SetControllerLock(bool locked)
    {
        throw new NotImplementedException();
    }

    #endregion

}
