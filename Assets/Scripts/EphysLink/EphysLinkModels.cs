using UnityEngine;

public struct AngularResponse
{
    public Vector3 Angles;
    public string Error;
}

public struct BooleanStateResponse
{
    public bool State;
    public string Error;
}

public struct CanWriteRequest
{
    public string ManipulatorId;
    public bool CanWrite;
    public float Hours;
}

public struct DriveToDepthRequest
{
    public string ManipulatorId;
    public float Depth;
    public float Speed;
}

public struct DriveToDepthResponse
{
    public float Depth;
    public string Error;
}


public struct GetManipulatorsResponse
{
    public string[] Manipulators;
    public int NumAxes;
    public Vector4 Dimensions;
    public string Error;
}


public struct GotoPositionRequest
{
    public string ManipulatorId;
    public Vector4 Position;
    public float Speed;
}

public struct InsideBrainRequest
{
    public string ManipulatorId;
    public bool Inside;
}


public struct PositionalResponse
{
    public Vector4 Position;
    public string Error;
}

public struct ShankCountResponse
{
    public int ShankCount;
    public string Error;
}

