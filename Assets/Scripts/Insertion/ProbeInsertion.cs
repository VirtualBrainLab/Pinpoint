using System;
using UnityEngine;
using CoordinateSpaces;
using CoordinateTransforms;
using System.Collections.Generic;
using BrainAtlas;
using BrainAtlas.CoordinateSystems;

/// <summary>
/// Representation of a probe insertion in a native CoordinateSpace and CoordinateTransform
/// 
/// Note that ProbeInsertions don't internally represent rotations caused by a CoordinateTransform
/// to interpolate these properly you need to use e.g. the tip/top positions that are output by the
/// CoordinateTransform 
/// </summary>
[Serializable]
public class ProbeInsertion
{
    #region Static instances
    [NonSerialized]
    public static HashSet<ProbeInsertion> Instances = new HashSet<ProbeInsertion>();
    #endregion

    #region Coordinate vars
    public ReferenceAtlas ReferenceAtlas { get; set; }
    public AtlasTransform AtlasTransform { get; set; }
    #endregion

    #region Name data
    public string AtlasName { get { return ReferenceAtlas.Name; } }
    public string TransformName { get { return AtlasTransform.Name; } }
    #endregion

    #region pos/angle vars

    public float AP;
    public float ML;
    public float DV;
    public float Yaw;
    public float Pitch;
    public float Roll;

    /// <summary>
    /// The **transformed** coordinate in the active CoordinateSpace (AP, ML, DV)
    /// </summary>
    public Vector3 apmldv
    {
        get => new Vector3(AP, ML, DV);
        set
        {
            AP = value.x;
            ML = value.y;
            DV = value.z;
        }
    }

    /// <summary>
    /// (Yaw, Pitch, Spin)
    /// </summary>
    public Vector3 angles
    {
        get => new(Yaw, Pitch, Roll);
        set
        {
            Yaw = value.x;
            Pitch = value.y;
            Roll = value.z;
        }
    }
    #endregion

    #region constructor

    public ProbeInsertion(float ap, float ml, float dv, float yaw, float pitch, float roll, 
        ReferenceAtlas coordSpace, AtlasTransform coordTransform, bool targetable = true)
    {
        this.AP = ap;
        this.ML = ml;
        this.DV = dv;
        this.Yaw = yaw;
        this.Pitch = pitch;
        this.Roll = roll;
        ReferenceAtlas = coordSpace;
        AtlasTransform = coordTransform;
        Instances.Add(this);
    }

    public ProbeInsertion(Vector3 tipPosition, Vector3 angles,
        ReferenceAtlas coordSpace, AtlasTransform coordTransform, bool targetable = true)
    {
        apmldv = tipPosition;
        this.angles = angles;
        ReferenceAtlas = coordSpace;
        AtlasTransform = coordTransform;
        Instances.Add(this);
    }
     
    public ProbeInsertion(ProbeInsertion otherInsertion, bool targetable = true)
    {
        apmldv = otherInsertion.apmldv;
        angles = otherInsertion.angles;
        ReferenceAtlas = otherInsertion.ReferenceAtlas;
        AtlasTransform = otherInsertion.AtlasTransform;
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
        return AtlasTransform.T2U(apmldv);
    }

    /// <summary>
    /// Get the corresponding **transformed** coordinate in World
    /// </summary>
    /// <returns></returns>
    public Vector3 PositionWorldT()
    {
        return ReferenceAtlas.Atlas2World(AtlasTransform.T2U_Vector(apmldv));
    }

    /// <summary>
    /// Get the corresponding **un-transformed** coordinate in World
    /// </summary>
    /// <returns></returns>
    public Vector3 PositionWorldU()
    {
        return ReferenceAtlas.Atlas2World(PositionSpaceU());
    }

    /// <summary>
    /// Convert a world coordinate into the ProbeInsertion's transformed space
    /// </summary>
    /// <param name="coordWorld"></param>
    /// <returns></returns>
    public Vector3 World2T(Vector3 coordWorld)
    {
        return AtlasTransform.U2T(ReferenceAtlas.World2Atlas(coordWorld));
    }

    public Vector3 World2T_Vector(Vector3 vectorWorld)
    {
        return AtlasTransform.U2T_Vector(ReferenceAtlas.World2Atlas_Vector(vectorWorld));
    }

    public Vector3 T2World(Vector3 coordT)
    {
        return ReferenceAtlas.Atlas2World(AtlasTransform.T2U(coordT));
    }
    public Vector3 T2World_Vector(Vector3 vectorT)
    {
        return ReferenceAtlas.Atlas2World_Vector(AtlasTransform.T2U_Vector(vectorT));
    }

    public override string ToString()
    {
        return JsonUtility.ToJson(this);
    }
}