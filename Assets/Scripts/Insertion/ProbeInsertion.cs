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

    #region Data
    private InsertionData _data;
    #endregion

    #region Coordinate Properties
    public string AtlasName
    {
        get => _data.AtlasName;
        set => _data.AtlasName = value;
    }
    public string TransformName
    {
        get => _data.TransformName;
        set => _data.TransformName = value;
    }
    #endregion

    #region Pos/Angle Properties

    public float AP
    {
        get => _data.APMLDV.x;
        set => _data.APMLDV.x = value;
    }
    public float ML
    {
        get => _data.APMLDV.y;
        set => _data.APMLDV.y = value;
    }
    public float DV
    {
        get => _data.APMLDV.z;
        set => _data.APMLDV.z = value;
    }
    public float Yaw
    {
        get => _data.Angles.x;
        set => _data.Angles.x = value;
    }
    public float Pitch
    {
        get => _data.Angles.y;
        set => _data.Angles.y = value;
    }
    public float Roll
    {
        get => _data.Angles.z;
        set => _data.Angles.z = value;
    }

    /// <summary>
    /// The **transformed** coordinate in the active CoordinateSpace (AP, ML, DV)
    /// </summary>
    public Vector3 APMLDV
    {
        get => _data.APMLDV;
        set
        {
            _data.APMLDV = value;
        }
    }

    /// <summary>
    /// (Yaw, Pitch, Spin)
    /// </summary>
    public Vector3 Angles
    {
        get => _data.Angles;
        set
        {
            _data.Angles = value;
        }
    }
    #endregion

    #region constructor

    public ProbeInsertion(float ap, float ml, float dv, float yaw, float pitch, float roll, 
        string atlasName, string transformName)
    {
        _data.APMLDV = new Vector3(ap, ml, dv);
        _data.Angles = new Vector3(yaw, pitch, roll);
        _data.AtlasName = atlasName;
        _data.TransformName = transformName;
        Instances.Add(this);
    }

    public ProbeInsertion(Vector3 tipPosition, Vector3 angles,
        string atlasName, string transformName)
    {
        _data.APMLDV = tipPosition;
        _data.Angles = angles;
        _data.AtlasName = atlasName;
        _data.TransformName = transformName;
        Instances.Add(this);
    }
     
    public ProbeInsertion(ProbeInsertion otherInsertion)
    {
        _data.APMLDV = otherInsertion.APMLDV;
        _data.Angles = otherInsertion.Angles;
        _data.AtlasName = otherInsertion.AtlasName;
        _data.TransformName = otherInsertion.TransformName;
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
        return BrainAtlasManager.ActiveAtlasTransform.T2U(_data.APMLDV);
    }

    /// <summary>
    /// Get the corresponding **transformed** coordinate in World
    /// </summary>
    /// <returns></returns>
    public Vector3 PositionWorldT()
    {
        return BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(BrainAtlasManager.ActiveAtlasTransform.T2U_Vector(_data.APMLDV));
    }

    public Vector3 PositionWorldU()
    {
        return BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(PositionSpaceU());
    }

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
        // Store the current reference coordinate
        _data.ReferenceCoord = BrainAtlasManager.ActiveReferenceAtlas.AtlasSpace.ReferenceCoord;
        return JsonUtility.ToJson(_data);
    }
}