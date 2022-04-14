using UnityEngine;

public class CCFCoordinateSystem : CoordinateSystem
{
    public float ap;
    public float ml;
    public float dv;
    public float phi;
    public float theta;
    public float spin;

    public CCFCoordinateSystem()
    {
        ap = 0; ml = 0; dv = 0; phi = 0; theta = 0; spin = 0;
    }

    public string Serialize()
    {
        return JsonUtility.ToJson(this);
    }

    public static CCFCoordinateSystem DeSerialize(string json)
    {
        return JsonUtility.FromJson<CCFCoordinateSystem>(json);
    }
}

public class CoordinateSystem
{

}