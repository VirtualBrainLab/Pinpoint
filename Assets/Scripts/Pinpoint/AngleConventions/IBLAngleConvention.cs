using UnityEngine;

public class IBLAngleConvention : AngleConvention
{
    public override string DisplayName => "IBL";

    public override string XName => "Phi";

    public override string YName => "Theta";

    public override string ZName => "Roll";

    /// <summary>
    /// Convert Pinpoint angles to IBL format
    /// </summary>
    /// <param name="pinpointAngles"></param>
    /// <returns></returns>
    public override Vector3 ToConvention(Vector3 pinpointAngles)
    {
        float iblPhi = -pinpointAngles.x - 90f;
        float iblTheta = 90 - pinpointAngles.y;
        return new Vector3(iblPhi, iblTheta, pinpointAngles.z);
    }

    /// <summary>
    /// Convert from IBL format angles to Pinpoint
    /// </summary>
    /// <param name="conventionAngles"></param>
    /// <returns></returns>
    public override Vector3 FromConvention(Vector3 conventionAngles)
    {
        float worldPhi = -conventionAngles.x - 90f;
        float worldTheta = 90 - conventionAngles.y;
        return new Vector3(worldPhi, worldTheta, conventionAngles.z);
    }
}
