using UnityEngine;

public abstract class AngleConvention
{

    public abstract string DisplayName { get; }

    public abstract Vector3 ToConvention(Vector3 pinpointAngles);

    public abstract Vector3 FromConvention(Vector3 conventionAngles);
}
