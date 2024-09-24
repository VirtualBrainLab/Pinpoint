using UnityEngine;

public abstract class AngleConvention
{

    public abstract string DisplayName { get; }
    public abstract string XName { get; }
    public abstract string YName { get; }
    public abstract string ZName { get; }

    public abstract bool AllowFrom { get; }

    public abstract Vector3 ToConvention(Vector3 pinpointAngles);

    public abstract Vector3 FromConvention(Vector3 conventionAngles);

    /// <summary>
    /// Convert Pinpoint angles to Cartesian coordinates in a unit sphere
    /// </summary>
    /// <param name="pinpointAngles"></param>
    /// <returns></returns>
    public static Vector3 ToCartesian(Vector3 pinpointAngles)
    {
        pinpointAngles.y = 90f - pinpointAngles.y; // invert the pitch
        Vector3 radAngles = pinpointAngles * Mathf.Deg2Rad;
        float x = Mathf.Sin(radAngles.y) * Mathf.Cos(radAngles.x);
        float y = Mathf.Sin(radAngles.y) * Mathf.Sin(radAngles.x);
        float z = Mathf.Cos(radAngles.y);

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Convert Cartesian coordiantes back to Pinpoint angles
    /// </summary>
    /// <param name="cartesianCoords">normalized vector of cartesian coordinates</param>
    /// <returns></returns>
    public static Vector3 FromCartesian(Vector3 cartesianCoords)
    {
        float yaw;

        // Handle near-vertical vectors by checking if x and y are small
        if (Mathf.Abs(cartesianCoords.x) < 1e-6f && Mathf.Abs(cartesianCoords.y) < 1e-6f)
        {
            return new Vector3(0f, 90f, 0f);
        }

        yaw = Mathf.Atan2(cartesianCoords.y, cartesianCoords.x) * Mathf.Rad2Deg;

        // Calculate the pitch (angle from the z-axis)
        float pitch = Mathf.Acos(cartesianCoords.z) * Mathf.Rad2Deg;

        // Return the pinpoint angles
        return new Vector3(yaw, 90f - pitch, 0f);
    }
}
