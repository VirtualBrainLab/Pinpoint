using System;
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
    public CoordinateSpace CoordinateSpace { get; private set; }
    public CoordinateTransform CoordinateTransform { get; private set; }
    #endregion

    #region pos/angle vars

    public float AP;
    public float ML;
    public float DV;
    public float Phi;
    public float Theta;
    public float Spin;

    /// <summary>
    /// The **transformed** coordinate in the active CoordinateSpace
    /// </summary>
    public Vector3 APMLDV
    {
        get => new Vector3(AP, ML, DV);
        set
        {
            AP = value.x;
            ML = value.y;
            DV = value.z;
        }
    }

    public Vector3 Angles
    {
        get => new Vector3(Phi, Theta, Spin);
        set
        {
            Phi = value.x;
            Theta = value.y;
            Spin = value.z;
        }
    }
    #endregion

    #region constructor

    public ProbeInsertion(float ap, float ml, float dv, float phi, float theta, float spin, 
        CoordinateSpace coordSpace, CoordinateTransform coordTransform)
    {
        AP = ap;
        ML = ml;
        DV = dv;
        Phi = phi;
        Theta = theta;
        Spin = spin;
        CoordinateSpace = coordSpace;
        CoordinateTransform = coordTransform;
    }

    public ProbeInsertion(Vector3 tipPosition, Vector3 angles,
        CoordinateSpace coordSpace, CoordinateTransform coordTransform)
    {
        APMLDV = tipPosition;
        Angles = angles;
        CoordinateSpace = coordSpace;
        CoordinateTransform = coordTransform;
    }

    #endregion

    /// <summary>
    /// Get the corresponding **un-transformed** coordinate in the CoordinateSpace
    /// </summary>
    /// <returns></returns>
    public Vector3 PositionSpace()
    {
        return CoordinateTransform.Transform2Space(APMLDV);
    }

    /// <summary>
    /// Get the corresponding **transformed** coordinate in World
    /// </summary>
    /// <returns></returns>
    public Vector3 PositionWorld()
    {
        return CoordinateSpace.Space2World(CoordinateTransform.Transform2SpaceAxisChange(APMLDV));
    }

    /// <summary>
    /// Get the corresponding **un-transformed** coordinate in World
    /// </summary>
    /// <returns></returns>
    public Vector3 GetPositionWorldUnTransformed()
    {
        return CoordinateSpace.Space2World(PositionSpace());
    }

    /// <summary>
    /// Convert a world coordinate into the ProbeInsertion's transformed space
    /// </summary>
    /// <param name="coordWorld"></param>
    /// <returns></returns>
    public Vector3 World2Transformed(Vector3 coordWorld)
    {
        return CoordinateTransform.Space2Transform(CoordinateSpace.World2Space(coordWorld));
    }

    public Vector3 World2TransformedAxisChange(Vector3 coordWorld)
    {
        return CoordinateTransform.Space2TransformAxisChange(CoordinateSpace.World2SpaceAxisChange(coordWorld));
    }

    public Vector3 Transformed2World(Vector3 coordTransformed)
    {
        return CoordinateSpace.Space2World(CoordinateTransform.Transform2Space(coordTransformed));
    }
    public Vector3 Transformed2WorldAxisChange(Vector3 coordTransformed)
    {
        return CoordinateSpace.Space2WorldAxisChange(CoordinateTransform.Transform2SpaceAxisChange(coordTransformed));
    }

    public override string ToString()
    {
        return string.Format("position ({0},{1},{2}) angles ({3},{4},{5}) coordinate space {6} coordinate transform {7}", AP, ML, DV, Phi, Theta, Spin, CoordinateSpace.ToString(), CoordinateTransform.ToString());
    }
}