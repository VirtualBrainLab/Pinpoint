using UnityEngine;

public class PinpointAngleConvention : AngleConvention
{
    public override string DisplayName => "Pinpoint";

    public override Vector3 FromConvention(Vector3 conventionAngles)
    {
        return conventionAngles;
    }

    public override Vector3 ToConvention(Vector3 pinpointAngles)
    {
        return pinpointAngles;
    }
}
