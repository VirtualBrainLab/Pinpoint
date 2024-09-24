using UnityEngine;
using System;

[Serializable]
public struct AffineTransformModel
{
    public string Name;
    public string Prefix;
    public Vector3 Scaling;
    public Vector3 Rotation;

    public AffineTransformModel(string name, string prefix, Vector3 scaling, Vector3 rotation)
    {
        Name = name;
        Prefix = prefix;
        Scaling = scaling;
        Rotation = rotation;
    }
}


[Serializable]
public struct CraniotomyModel
{
    public int Index;
    public Vector2 Size;
    public Vector3 Position;

    public CraniotomyModel(int index, Vector2 size, Vector3 position)
    {
        Index = index;
        Size = size;
        Position = position;
    }
}


[Serializable]
public struct InsertionModel
{
    public Vector3 Position;
    public Vector3 Angles;
    public string AtlasName;
    public string TransformName;
    public Vector3 ReferenceCoord;

    public InsertionModel(Vector3 position, Vector3 angles, string atlasName, string transformName, Vector3 referenceCoord)
    {
        Position = position;
        Angles = angles;
        AtlasName = atlasName;
        TransformName = transformName;
        ReferenceCoord = referenceCoord;
    }
}


[Serializable]
public struct ProbeModel
{
    public InsertionModel Insertion;
    public string Uuid;
    public string Name;
    public Color Color;

    public ProbeModel(InsertionModel insertion, string uuid, string name, Color color)
    {
        Insertion = insertion;
        Uuid = uuid;
        Name = name;
        Color = color;
    }
}


[Serializable]
public struct RigModel
{
    public string Name;
    public string Image;
    public Vector3 Position;
    public Vector3 Rotation;
    public bool Active;

    public RigModel(string name, string image, Vector3 position, Vector3 rotation, bool active)
    {
        Name = name;
        Image = image;
        Position = position;
        Rotation = rotation;
        Active = active;
    }
}

[Serializable]
public struct SceneModel
{
    public string AtlasName;
    public string TransformName;
    public ProbeModel[] Probes;
    public RigModel[] Rigs;
    public CraniotomyModel[] Craniotomies;
    public string[] SceneData;
    public string Settings;

    public SceneModel(string atlasName, string transformName, ProbeModel[] probes, RigModel[] rigs, CraniotomyModel[] craniotomies, string[] sceneData, string settings)
    {
        AtlasName = atlasName;
        TransformName = transformName;
        Probes = probes;
        Rigs = rigs;
        Craniotomies = craniotomies;
        SceneData = sceneData;
        Settings = settings;
    }
}

