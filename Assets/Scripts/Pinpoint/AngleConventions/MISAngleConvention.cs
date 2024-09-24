using UnityEngine;

public class MISAngleConvention : AngleConvention
{
    // The current implementation of this is going to make some assumptions about the orientations

    public override string DisplayName => "New Scale MIS";

    /// <summary>
    /// Convert MIS angles to IBL format
    /// </summary>
    /// <param name="pinpointAngles"></param>
    /// <returns></returns>
    public override Vector3 ToConvention(Vector3 pinpointAngles)
    {
        // todo
        return new Vector3();
    }

    /// <summary>
    /// Convert from IBL format angles to Pinpoint
    /// </summary>
    /// <param name="conventionAngles"></param>
    /// <returns></returns>
    public override Vector3 FromConvention(Vector3 conventionAngles)
    {
        // todo
        return new Vector3();
    }
}
