using UnityEngine;

public struct AngularResponse
{
    public Vector3 Angles;
    public string Error;

    public AngularResponse(Vector3 angles, string error)
    {
        Angles = angles;
        Error = error;
    }
}

public struct BooleanStateResponse
{
    public bool State;
    public string Error;

    public BooleanStateResponse(bool state, string error)
    {
        State = state;
        Error = error;
    }
}

public struct CanWriteRequest
{
    public string ManipulatorId;
    public bool CanWrite;
    public float Hours;

    public CanWriteRequest(string manipulatorId, bool canWrite, float hours)
    {
        ManipulatorId = manipulatorId;
        CanWrite = canWrite;
        Hours = hours;
    }
}

public struct DriveToDepthRequest
{
    public string ManipulatorId;
    public float Depth;
    public float Speed;

    public DriveToDepthRequest(string manipulatorId, float depth, float speed)
    {
        ManipulatorId = manipulatorId;
        Depth = depth;
        Speed = speed;
    }
}

public struct DriveToDepthResponse
{
    public float Depth;
    public string Error;

    public DriveToDepthResponse(float depth, string error)
    {
        Depth = depth;
        Error = error;
    }
}


public struct GetManipulatorsResponse
{
    public string[] Manipulators;
    public int NumAxes;
    public Vector4 Dimensions;
    public string Error;

    public GetManipulatorsResponse(string[] manipulators, int numAxes, Vector4 dimensions, string error)
    {
        Manipulators = manipulators;
        NumAxes = numAxes;
        Dimensions = dimensions;
        Error = error;
    }
}


public struct GotoPositionRequest
{
    public string ManipulatorId;
    public Vector4 Position;
    public float Speed;

    public GotoPositionRequest(string manipulatorId, Vector4 position, float speed)
    {
        ManipulatorId = manipulatorId;
        Position = position;
        Speed = speed;
    }
}

public struct InsideBrainRequest
{
    public string ManipulatorId;
    public bool Inside;

    public InsideBrainRequest(string manipulatorId, bool inside)
    {
        ManipulatorId = manipulatorId;
        Inside = inside;
    }
}


public struct PositionalResponse
{
    public Vector4 Position;
    public string Error;

    public PositionalResponse(Vector4 position, string error)
    {
        Position = position;
        Error = error;
    }
}

public struct ShankCountResponse
{
    public int ShankCount;
    public string Error;

    public ShankCountResponse(int shankCount, string error)
    {
        ShankCount = shankCount;
        Error = error;
    }
}

