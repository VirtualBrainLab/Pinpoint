using UnityEngine;

/// <summary>
/// Representation of a probe insertion in *CCF SPACE*.
/// Also allows you to convert insertions to string/JSON formats for export and reads string/JSON formats for ingestion.
/// </summary>
[System.Serializable]
public class ProbeInsertion
{
    public float ap;
    public float ml;
    public float dv;
    public float depth;
    public float phi;
    public float theta;
    public float spin;

    public Vector3 apmldv
    {
        get
        {
            return new Vector3(ap, ml, dv);
        }
    }

    public ProbeInsertion(float ap, float ml, float dv, float depth, float phi, float theta, float spin)
    {
        SetCoordinates(ap, ml, dv, depth, phi, theta, spin);
    }

    public ProbeInsertion(Vector3 coords, float depth, Vector3 angles, string transformString)
    {
        SetCoordinates(coords.x, coords.y, coords.z, depth, angles.x, angles.y, angles.z);
    }

    public ProbeInsertion(string stringRepresentation, bool JSON)
    {
        if (JSON)
            SetCoordinatesJSON(stringRepresentation);
        else
            SetCoordinatesString(stringRepresentation);
    }

    public void SetCoordinates(float ap, float ml, float dv, float depth, float phi, float theta, float spin)
    {
        this.ap = ap;
        this.ml = ml;
        this.dv = dv;
        this.depth = depth;
        this.phi = phi;
        this.theta = theta;
        this.spin = spin;
    }
    public void SetCoordinates(float ap, float ml, float dv, float depth, float phi, float theta, float spin, CoordinateTransform coordTransform)
    {
        Vector3 ccf_apmldv = coordTransform.ToCCF(new Vector3(ap, ml, dv));
        this.ap = ccf_apmldv.x;
        this.ml = ccf_apmldv.y;
        this.dv = ccf_apmldv.z;
        this.depth = depth;
        this.phi = phi;
        this.theta = theta;
        this.spin = spin;
    }

    public void SetCoordinates(ProbeInsertion otherInsertion)
    {
        ap = otherInsertion.ap;
        ml = otherInsertion.ml;
        dv = otherInsertion.dv;
        depth = otherInsertion.depth;
        phi = otherInsertion.phi;
        theta = otherInsertion.theta;
        spin = otherInsertion.spin;
    }
    public void SetCoordinates(ProbeInsertion otherInsertion, CoordinateTransform coordTransform)
    {
        Vector3 ccf_apmldv = coordTransform.ToCCF(otherInsertion.apmldv);
        this.ap = ccf_apmldv.x;
        this.ml = ccf_apmldv.y;
        this.dv = ccf_apmldv.z;
        depth = otherInsertion.depth;
        phi = otherInsertion.phi;
        theta = otherInsertion.theta;
        spin = otherInsertion.spin;
    }

    public void SetCoordinatesString(string input)
    {
        int metaIdx = input.IndexOf(';');
        int apIdx = input.IndexOf("ccfAP:");
        int mlIdx = input.IndexOf("ccfML:");
        int dvIdx = input.IndexOf("ccfDV:");
        int depthIdx = input.IndexOf("ccfDP:");
        int phiIdx = input.IndexOf("ccfPh:");
        int thetaIdx = input.IndexOf("ccfTh:");
        int spinIdx = input.IndexOf("ccfSp:");
        ap = float.Parse(input.Substring(apIdx + 7, mlIdx));
        ml = float.Parse(input.Substring(mlIdx + 7, dvIdx));
        depth = float.Parse(input.Substring(dvIdx + 7, phiIdx));
        phi = float.Parse(input.Substring(phiIdx + 7, thetaIdx));
        theta = float.Parse(input.Substring(thetaIdx + 7, spinIdx));
        spin = float.Parse(input.Substring(spinIdx + 7, input.Length));
    }

    public void SetCoordinatesJSON(string json)
    {
        ProbeInsertion temp = JsonUtility.FromJson<ProbeInsertion>(json);
        SetCoordinates(temp);
    }

    public string GetCoordinatesString()
    {
        return string.Format("ccfAP:{1} ccfML:{2} ccfDV:{3} ccfDP:{4} ccfPh:{5} ccfTh:{6} ccfSp:{7}", ap, ml, dv, depth, phi, theta, spin);
    }

    public string GetCoordinatesJSON()
    {
        return JsonUtility.ToJson(this);
    }

    public (float, float, float, float, float, float, float) GetCoordinatesFloat()
    {
        return (ap, ml, dv, depth, phi, theta, spin);
    }
    public (float, float, float, float, float, float, float) GetCoordinatesFloat(CoordinateTransform coordTransform)
    {
        Vector3 transCoord = coordTransform.FromCCF(apmldv);
        return (transCoord.x, transCoord.y, transCoord.z, depth, phi, theta, spin);
    }

    public (Vector3, float, Vector3) GetCoordinatesVector3()
    {
        return (apmldv, depth, new Vector3(phi, theta, spin));
    }
    public (Vector3, float, Vector3) GetCoordinatesVector3(CoordinateTransform coordTransform)
    {
        return (coordTransform.FromCCF(apmldv), depth, new Vector3(phi, theta, spin));
    }

    /// <summary>
    /// Set coordinates in IBL conventions, expects um units and IBL rotated angles
    /// </summary>
    public void SetCoordinates_IBL(float ap, float ml, float dv, float depth, float phi, float theta, float spin)
    {
        Vector2 worldPhiTheta = IBL2World(new Vector2(phi, theta));
        SetCoordinates(ap / 1000f, ml / 1000f, dv / 1000f, depth / 1000f, worldPhiTheta.x, worldPhiTheta.y, spin);
    }
    public void SetCoordinates_IBL(float ap, float ml, float dv, float depth, float phi, float theta, float spin, CoordinateTransform coordTransform)
    {
        Vector3 ccf_apmldv = coordTransform.ToCCF(new Vector3(ap / 1000f, ml / 1000f, dv / 1000f));
        Vector2 worldPhiTheta = IBL2World(new Vector2(phi, theta));
        SetCoordinates(ccf_apmldv.x, ccf_apmldv.y, ccf_apmldv.z, depth / 1000f, worldPhiTheta.x, worldPhiTheta.y, spin);
    }

    /// <summary>
    /// Return coordinates in IBL conventions, i.e. um units and rotated angles
    /// </summary>
    /// <returns></returns>
    public (float, float, float, float, float, float, float) GetCoordinatesFloat_IBL()
    {
        Vector2 iblPhiTheta = World2IBL(new Vector2(phi, theta));
        return (ap * 1000f, ml * 1000f, dv * 1000f, depth * 1000f, iblPhiTheta.x, iblPhiTheta.y, spin);
    }
    public (float, float, float, float, float, float, float) GetCoordinatesFloat_IBL(CoordinateTransform coordTransform)
    {
        Vector3 transCoord = coordTransform.FromCCF(apmldv);
        Vector2 iblPhiTheta = World2IBL(new Vector2(phi, theta));
        return (transCoord.x * 1000f, transCoord.y * 1000f, transCoord.z * 1000f, depth * 1000f, iblPhiTheta.x, iblPhiTheta.y, spin);
    }

    /// <summary>
    /// Rotate phi and theta to match IBL coordinates
    /// </summary>
    /// <param name="phiTheta"></param>
    /// <returns></returns>
    private Vector2 World2IBL(Vector2 phiTheta)
    {
        float iblPhi = -phiTheta.x - 90f;
        float iblTheta = -phiTheta.y;
        return new Vector2(iblPhi, iblTheta);
    }

    /// <summary>
    /// Rotate IBL coordinates to return to pinpoint space
    /// </summary>
    /// <param name="iblPhiTheta"></param>
    /// <returns></returns>
    private Vector2 IBL2World(Vector2 iblPhiTheta)
    {
        float worldPhi = -iblPhiTheta.x - 90f;
        float worldTheta = -iblPhiTheta.y;
        return new Vector2(worldPhi, worldTheta);
    }
}
