using UnityEngine;

public class PinpointUtils
{
    /// <summary>
    /// Rotate phi and theta to match IBL coordinates
    /// </summary>
    /// <param name="phiTheta"></param>
    /// <returns></returns>
    public static Vector3 World2IBL(Vector3 phiThetaSpin)
    {
        float iblPhi = -phiThetaSpin.x - 90f;
        float iblTheta = 90-phiThetaSpin.y;
        return new Vector3(iblPhi, iblTheta, phiThetaSpin.z);
    }

    /// <summary>
    /// Rotate IBL coordinates to return to pinpoint space
    /// </summary>
    /// <param name="iblPhiTheta"></param>
    /// <returns></returns>
    public static Vector3 IBL2World(Vector3 iblAngles)
    {
        float worldPhi = -iblAngles.x - 90f;
        float worldTheta = 90-iblAngles.y;
        return new Vector3(worldPhi, worldTheta, iblAngles.z);
    }
}
