using UnityEngine;

public struct ManipulatorControllerData
{
    public string ManipulatorID;
    public int NumAxes;
    public Vector3 Dimensions;

    public Vector4 ZeroCoordinateOffset;
    public float BrainSurfaceOffset;
    public bool IsSetToDropToSurfaceWithDepth;

    public string CoordinateSpaceName;
    public string CoordinateTransformName;

    public bool IsRightHanded;
}
