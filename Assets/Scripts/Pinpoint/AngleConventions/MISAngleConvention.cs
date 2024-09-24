using UnityEngine;

public class MISAngleConvention : AngleConvention
{
    // The current implementation of this is going to make some assumptions about the orientations

    public override string DisplayName => "New Scale MIS";

    public override string XName => "Arc angle";

    public override string YName => "Arc tilt";

    public override string ZName => "Spin";

    public override bool AllowFrom => false;

    /// <summary>
    /// Convert MIS angles to IBL format
    /// </summary>
    /// <param name="pinpointAngles"></param>
    /// <returns></returns>
    public override Vector3 ToConvention(Vector3 pinpointAngles)
    {
        // todo
        Vector3 cartesianCoords = ToCartesian(pinpointAngles);
        //float arc_tilt = Mathf.Asin(pinpointAngles)
        //float arc_angle = 
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
