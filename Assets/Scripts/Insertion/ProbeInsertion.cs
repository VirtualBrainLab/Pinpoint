using UnityEngine;
using CoordinateSpaces;
using CoordinateTransforms;

/// <summary>
/// Representation of a probe insertion in a native CoordinateSpace and CoordinateTransform
/// 
/// Note that ProbeInsertions don't internally represent rotations caused by a CoordinateTransform
/// to interpolate these properly you need to use e.g. the tip/top positions that are output by the
/// CoordinateTransform 
/// </summary>
public class ProbeInsertion
{
    #region Coordinate vars
    private CoordinateSpace _coordinateSpace;
    private CoordinateTransform _coordinateTransform;

    public CoordinateSpace CoordinateSpace { get { return _coordinateSpace; } }
    public CoordinateTransform CoordinateTransform { get { return _coordinateTransform; } }
    #endregion

    #region pos/angle vars

    public float ap;
    public float ml;
    public float dv;
    public float phi;
    public float theta;
    public float spin;

    /// <summary>
    /// The **transformed** coordinate in the active CoordinateSpace
    /// </summary>
    public Vector3 apmldv
    {
        get => new Vector3(ap, ml, dv);
        set
        {
            ap = value.x;
            ml = value.y;
            dv = value.z;
        }
    }

    public Vector3 angles
    {
        get => new Vector3(phi, theta, spin);
        set
        {
            phi = value.x;
            theta = value.y;
            spin = value.z;
        }
    }
    #endregion

    #region constructor

    public ProbeInsertion(float ap, float ml, float dv, float phi, float theta, float spin, 
        CoordinateSpace coordSpace, CoordinateTransform coordTransform)
    {
        this.ap = ap;
        this.ml = ml;
        this.dv = dv;
        this.phi = phi;
        this.theta = theta;
        this.spin = spin;
        _coordinateSpace = coordSpace;
        _coordinateTransform = coordTransform;
    }

    public ProbeInsertion(Vector3 tipPosition, Vector3 angles,
        CoordinateSpace coordSpace, CoordinateTransform coordTransform)
    {
        apmldv = tipPosition;
        this.angles = angles;
        _coordinateSpace = coordSpace;
        _coordinateTransform = coordTransform;
    }

    #endregion

    /// <summary>
    /// Get the corresponding **un-transformed** coordinate in the CoordinateSpace
    /// </summary>
    /// <returns></returns>
    public Vector3 PositionSpace()
    {
        return _coordinateTransform.Transform2Space(apmldv);
    }

    /// <summary>
    /// Get the corresponding **transformed** coordinate in World
    /// </summary>
    /// <returns></returns>
    public Vector3 PositionWorld()
    {
        return _coordinateSpace.Space2World(_coordinateTransform.Transform2SpaceAxisChange(apmldv));
    }

    /// <summary>
    /// Get the corresponding **un-transformed** coordinate in World
    /// </summary>
    /// <returns></returns>
    public Vector3 GetPositionWorldUnTransformed()
    {
        return _coordinateSpace.Space2World(PositionSpace());
    }

    /// <summary>
    /// Convert a world coordinate into the ProbeInsertion's transformed space
    /// </summary>
    /// <param name="coordWorld"></param>
    /// <returns></returns>
    public Vector3 World2Transformed(Vector3 coordWorld)
    {
        return _coordinateTransform.Space2Transform(_coordinateSpace.World2Space(coordWorld));
    }

    public Vector3 World2TransformedAxisChange(Vector3 coordWorld)
    {
        return _coordinateTransform.Space2TransformAxisChange(_coordinateSpace.World2SpaceAxisChange(coordWorld));
    }

    public Vector3 Transformed2World(Vector3 coordTransformed)
    {
        return _coordinateSpace.Space2World(_coordinateTransform.Transform2Space(coordTransformed));
    }
    public Vector3 Transformed2WorldAxisChange(Vector3 coordTransformed)
    {
        return _coordinateSpace.Space2WorldAxisChange(_coordinateTransform.Transform2SpaceAxisChange(coordTransformed));
    }

    public override string ToString()
    {
        return string.Format("position ({0},{1},{2}) angles ({3},{4},{5}) coordinate space {6} coordinate transform {7}", ap, ml, dv, phi, theta, spin, _coordinateSpace.ToString(), _coordinateTransform.ToString());
    }
}