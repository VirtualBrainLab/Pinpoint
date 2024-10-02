using UnityEngine;

public class SagittalCoronalAngleConvention : AngleConvention
{
    public override string DisplayName => "Coronal/Sagittal";

    public override string XName => "Coronal";

    public override string YName => "Sagittal";

    public override string ZName => "Roll";

    public override bool AllowFrom => false;

    public override Vector3 FromConvention(Vector3 conventionAngles)
    {

        //conventionAngles = conventionAngles.normalized;

        //// Extract the sagittal (XZ) and coronal (YZ) angles from the convention angles
        //float XZAngle = conventionAngles.x;
        //float YZAngle = -conventionAngles.y; // Undo the sign change from ToConvention
        //float combinedRoll = conventionAngles.z;

        //Debug.Log(conventionAngles);

        ////Convert the angles back to Cartesian coordinates
        //float z = 1f; // We assume unit length for simplicity (same as in ToConvention)
        //float x = Mathf.Tan((90f - XZAngle) * Mathf.Deg2Rad) * z;
        //float y = Mathf.Tan((90f - YZAngle) * Mathf.Deg2Rad) * z;

        //Debug.Log((x, y, z));

        //Vector3 pinpointAngles = FromCartesian(new Vector3(x, y, z));

        //Debug.Log(pinpointAngles);

        //pinpointAngles.z = combinedRoll - pinpointAngles.x;


        //Debug.Log(pinpointAngles);

        return conventionAngles;
    }

    public override Vector3 ToConvention(Vector3 pinpointAngles)
    {
        Vector3 cartesianCoords = ToCartesian(pinpointAngles);

        Debug.Log(cartesianCoords);

        // Get the XZ angle, which is the sagittal angle
        float XZAngle = 90f - Mathf.Atan2(cartesianCoords.z, cartesianCoords.x) * Mathf.Rad2Deg;
        // Get the YZ angle, which is the coronal plane
        float YZAngle = 90f - Mathf.Atan2(cartesianCoords.z, cartesianCoords.y) * Mathf.Rad2Deg;

        Vector3 angles = new Vector3(XZAngle, -YZAngle, pinpointAngles.x + pinpointAngles.z);
        Vector3 back = FromConvention(angles);

        Debug.Log((pinpointAngles, back));
        return new Vector3(XZAngle, -YZAngle, pinpointAngles.x + pinpointAngles.z);
    }
}
