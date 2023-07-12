using System;
using UnityEngine;
using CoordinateSpaces;
using CoordinateTransforms;
using System.Collections.Generic;

/// <summary>
/// Representation of a probe insertion in a native CoordinateSpace and CoordinateTransform
/// 
/// Note that ProbeInsertions don't internally represent rotations caused by a CoordinateTransform
/// to interpolate these properly you need to use e.g. the tip/top positions that are output by the
/// CoordinateTransform 
/// </summary>
public class ProbeInsertion
{
    #region Static instances
    public static HashSet<ProbeInsertion> Instances = new HashSet<ProbeInsertion>();
    #endregion

    #region Coordinate vars
    public CoordinateSpace CoordinateSpace { get; set; }
    public CoordinateTransform CoordinateTransform { get; set; }
    #endregion

    #region pos/angle vars

    public float ap;
    public float ml;
    public float dv;
    public float yaw;
    public float pitch;
    public float roll;

    /// <summary>
    /// The **transformed** coordinate in the active CoordinateSpace (AP, ML, DV)
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

    /// <summary>
    /// (Yaw, Pitch, Spin)
    /// </summary>
    public Vector3 angles
    {
        get => new Vector3(yaw, pitch, roll);
        set
        {
            yaw = value.x;
            pitch = value.y;
            roll = value.z;
        }
    }
    #endregion

    #region constructor

    public ProbeInsertion(float ap, float ml, float dv, float yaw, float pitch, float roll, 
        CoordinateSpace coordSpace, CoordinateTransform coordTransform, bool targetable = true)
    {
        this.ap = ap;
        this.ml = ml;
        this.dv = dv;
        this.yaw = yaw;
        this.pitch = pitch;
        this.roll = roll;
        CoordinateSpace = coordSpace;
        CoordinateTransform = coordTransform;
        Instances.Add(this);
    }

    public ProbeInsertion(Vector3 tipPosition, Vector3 angles,
        CoordinateSpace coordSpace, CoordinateTransform coordTransform, bool targetable = true)
    {
        apmldv = tipPosition;
        this.angles = angles;
        CoordinateSpace = coordSpace;
        CoordinateTransform = coordTransform;
        Instances.Add(this);
    }
     
    public ProbeInsertion(ProbeInsertion otherInsertion, bool targetable = true)
    {
        apmldv = otherInsertion.apmldv;
        angles = otherInsertion.angles;
        CoordinateSpace = otherInsertion.CoordinateSpace;
        CoordinateTransform = otherInsertion.CoordinateTransform;
        Instances.Add(this);
    }

    ~ProbeInsertion()
    {
        if (Instances.Contains(this))
            Instances.Remove(this);
    }

    #endregion

    /// <summary>
    /// Get the corresponding **un-transformed** coordinate in the CoordinateSpace
    /// </summary>
    /// <returns></returns>
    public Vector3 PositionSpaceU()
    {
        return CoordinateTransform.Transform2Space(apmldv);
    }

    /// <summary>
    /// Get the corresponding **transformed** coordinate in World
    /// </summary>
    /// <returns></returns>
    public Vector3 PositionWorldT()
    {
        return CoordinateSpace.Space2World(CoordinateTransform.Transform2SpaceAxisChange(apmldv));
    }

    /// <summary>
    /// Get the corresponding **un-transformed** coordinate in World
    /// </summary>
    /// <returns></returns>
    public Vector3 PositionWorldU()
    {
        return CoordinateSpace.Space2World(PositionSpaceU());
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
        return string.Format("position ({0},{1},{2}) angles ({3},{4},{5}) coordinate space {6} coordinate transform {7}", ap, ml, dv, yaw, pitch, roll, CoordinateSpace.ToString(), CoordinateTransform.ToString());
    }

    public string PositionToString()
    {
        return $"AP: {Math.Round(ap*1000)} ML: {Math.Round(ml*1000)} DV: {Math.Round(dv*1000)}";
    }
}