using System;
using UnityEngine;

/// <summary>
/// Craniotomy Position is the center point, extent is -Width/2 to +Width/2, etc if Rectangle or Width = Height = Diameter
/// </summary>
/// 

[Serializable]
public struct CraniotomyData
{
    public Vector3 Position;
    public float Width;
    public float Height;
    public bool Rectangle;
}
