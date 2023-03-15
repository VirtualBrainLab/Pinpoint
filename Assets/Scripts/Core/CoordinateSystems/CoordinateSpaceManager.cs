using UnityEngine;
using CoordinateSpaces;
using CoordinateTransforms;

public class CoordinateSpaceManager : MonoBehaviour
{
    public static CoordinateSpace ActiveCoordinateSpace;
    public static CoordinateTransform ActiveCoordinateTransform;

    /// <summary>
    /// Convert a world coordinate into the corresponding world coordinate after transformation
    /// </summary>
    /// <param name="coordWorld"></param>
    /// <returns></returns>
    public static Vector3 WorldU2WorldT(Vector3 coordWorld)
    {
        return ActiveCoordinateSpace.Space2World(ActiveCoordinateTransform.Transform2SpaceAxisChange(ActiveCoordinateTransform.Space2Transform(ActiveCoordinateSpace.World2Space(coordWorld))));
    }

    public static Vector3 WorldT2WorldU(Vector3 coordWorldT)
    {
        return ActiveCoordinateSpace.Space2World(ActiveCoordinateTransform.Transform2Space(ActiveCoordinateTransform.Space2TransformAxisChange(ActiveCoordinateSpace.World2Space(coordWorldT))));
    }

    /// <summary>
    /// Helper function
    /// Convert a world coordinate into a transformed coordinate using the reference coordinate and the axis change
    /// </summary>
    /// <param name="coordWorld"></param>
    /// <returns></returns>
    public static Vector3 World2TransformedAxisChange(Vector3 coordWorld)
    {
        return ActiveCoordinateTransform.Space2TransformAxisChange(ActiveCoordinateSpace.World2Space(coordWorld));
    }

    public static Vector3 Transformed2WorldAxisChange(Vector3 coordTransformed)
    {
        return ActiveCoordinateSpace.Space2World(ActiveCoordinateTransform.Transform2SpaceAxisChange(coordTransformed));
    }

    public static void SetRelativeCoordinate(Vector3 coord)
    {
        ActiveCoordinateSpace.RelativeOffset = coord;
    }
}