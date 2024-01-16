using System;
using UnityEngine;
using System.Collections.Generic;
using BrainAtlas;

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
    public string AtlasName { get; set; }
    public string TransformName { get; set; }
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
        string atlasName, string transformName)
    {
        this.AP = ap;
        this.ML = ml;
        this.DV = dv;
        this.Yaw = yaw;
        this.Pitch = pitch;
        this.Roll = roll;
        AtlasName = atlasName;
        TransformName = transformName;
        Instances.Add(this);
    }

    public ProbeInsertion(Vector3 tipPosition, Vector3 angles,
        string atlasName, string transformName)
    {
        apmldv = tipPosition;
        this.angles = angles;
        AtlasName = atlasName;
        TransformName = transformName;
        Instances.Add(this);
    }
     
    public ProbeInsertion(ProbeInsertion otherInsertion)
    {
        apmldv = otherInsertion.apmldv;
        angles = otherInsertion.angles;
        AtlasName = otherInsertion.AtlasName;
        TransformName = otherInsertion.TransformName;
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
        return BrainAtlasManager.ActiveAtlasTransform.T2U(apmldv);
    }

    /// <summary>
    /// Get the corresponding **transformed** coordinate in World
    /// </summary>
    /// <returns></returns>
    public Vector3 PositionWorldT()
    {
        return BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(BrainAtlasManager.ActiveAtlasTransform.T2U_Vector(apmldv));
    }

    /// <summary>
    /// Get the corresponding **un-transformed** coordinate in World
    /// </summary>
    /// <returns></returns>
    public Vector3 PositionWorldU()
    {
        return BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(PositionSpaceU());
    }

    /// <summary>
    /// Convert a world coordinate into the ProbeInsertion's transformed space
    /// </summary>
    /// <param name="coordWorld"></param>
    /// <returns></returns>
    [Obsolete("Method is now BrainAtlasManager, please replace")]
    public Vector3 World2T(Vector3 coordWorld)
    {
        return BrainAtlasManager.ActiveAtlasTransform.U2T(BrainAtlasManager.ActiveReferenceAtlas.World2Atlas(coordWorld));
    }

    [Obsolete("Method is now BrainAtlasManager, please replace")]
    public Vector3 World2T_Vector(Vector3 vectorWorld)
    {
        return BrainAtlasManager.ActiveAtlasTransform.U2T_Vector(BrainAtlasManager.ActiveReferenceAtlas.World2Atlas_Vector(vectorWorld));
    }

    [Obsolete("Method is now BrainAtlasManager, please replace")]
    public Vector3 T2World(Vector3 coordT)
    {
        return BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(BrainAtlasManager.ActiveAtlasTransform.T2U(coordT));
    }
    [Obsolete("Method is now BrainAtlasManager, please replace")]
    public Vector3 T2World_Vector(Vector3 vectorT)
    {
        return BrainAtlasManager.ActiveReferenceAtlas.Atlas2World_Vector(BrainAtlasManager.ActiveAtlasTransform.T2U_Vector(vectorT));
    }

    public override string ToString()
    {
        return JsonUtility.ToJson(this);
    }
}