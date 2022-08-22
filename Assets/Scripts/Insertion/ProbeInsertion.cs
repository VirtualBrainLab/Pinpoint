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

    public Vector3 angles
    {
        get
        {
            return new Vector3(phi, theta, spin);
        }
    }

    public ProbeInsertion(float ap, float ml, float dv, float phi, float theta, float spin)
    {
        SetCoordinates(ap, ml, dv, phi, theta, spin);
    }

    public ProbeInsertion(Vector3 coords, Vector3 angles)
    {
        SetCoordinates(coords.x, coords.y, coords.z, angles.x, angles.y, angles.z);
    }

    public ProbeInsertion(string stringRepresentation, bool JSON)
    {
        if (JSON)
            SetCoordinatesJSON(stringRepresentation);
        else
            SetCoordinatesString(stringRepresentation);
    }

    public void SetCoordinates(float ap, float ml, float dv, float phi, float theta, float spin)
    {
        this.ap = ap;
        this.ml = ml;
        this.dv = dv;
        this.phi = phi;
        this.theta = theta;
        this.spin = spin;
    }
    public void SetCoordinates(float ap, float ml, float dv, float phi, float theta, float spin, CoordinateTransform coordTransform)
    {
        Vector3 ccf_apmldv = coordTransform.ToCCF(new Vector3(ap, ml, dv));
        this.ap = ccf_apmldv.x;
        this.ml = ccf_apmldv.y;
        this.dv = ccf_apmldv.z;
        this.phi = phi;
        this.theta = theta;
        this.spin = spin;
    }

    public void SetCoordinates(ProbeInsertion otherInsertion)
    {
        ap = otherInsertion.ap;
        ml = otherInsertion.ml;
        dv = otherInsertion.dv;
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
        int phiIdx = input.IndexOf("ccfPh:");
        int thetaIdx = input.IndexOf("ccfTh:");
        int spinIdx = input.IndexOf("ccfSp:");
        ap = float.Parse(input.Substring(apIdx + 7, mlIdx));
        ml = float.Parse(input.Substring(mlIdx + 7, dvIdx));
        dv = float.Parse(input.Substring(dvIdx + 7, phiIdx));
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
        return string.Format("ccfAP:{1} ccfML:{2} ccfDV:{3} ccfPh:{4} ccfTh:{5} ccfSp:{6}", ap, ml, dv, phi, theta, spin);
    }

    public string GetCoordinatesJSON()
    {
        return JsonUtility.ToJson(this);
    }

    public (float, float, float, float, float, float) GetCoordinatesFloat()
    {
        return (ap, ml, dv, phi, theta, spin);
    }
    public (float, float, float, float, float, float) GetCoordinatesFloat(CoordinateTransform coordTransform)
    {
        Vector3 transCoord = coordTransform.FromCCF(apmldv);
        return (transCoord.x, transCoord.y, transCoord.z, phi, theta, spin);
    }

    public (Vector3, Vector3) GetCoordinatesVector3()
    {
        return (apmldv, new Vector3(phi, theta, spin));
    }
    public (Vector3, Vector3) GetCoordinatesVector3(CoordinateTransform coordTransform)
    {
        return (coordTransform.FromCCF(apmldv), new Vector3(phi, theta, spin));
    }

    /// <summary>
    /// Set coordinates in IBL conventions, expects um units and IBL rotated angles
    /// </summary>
    public void SetCoordinates_IBL(float ap, float ml, float dv, float phi, float theta, float spin, CoordinateTransform coordTransform = null)
    {
        Vector2 worldPhiTheta = Utils.IBL2World(new Vector2(phi, theta));

        if (coordTransform != null)
        {
            Vector3 ccf_apmldv = coordTransform.ToCCF(new Vector3(ap / 1000f, ml / 1000f, dv / 1000f));
            SetCoordinates(ccf_apmldv.x, ccf_apmldv.y, ccf_apmldv.z, worldPhiTheta.x, worldPhiTheta.y, spin);
        }
        else
            SetCoordinates(ap / 1000f, ml / 1000f, dv / 1000f, worldPhiTheta.x, worldPhiTheta.y, spin);

    }

    /// <summary>
    /// Return coordinates in IBL conventions, i.e. um units and rotated angles
    /// </summary>
    /// <returns></returns>
    public (float, float, float, float, float, float) GetCoordinatesFloat_IBL(CoordinateTransform coordTransform = null)
    {
        Vector2 iblPhiTheta = Utils.World2IBL(new Vector2(phi, theta));
        if (coordTransform != null)
        {
            Vector3 transCoord = coordTransform.FromCCF(apmldv);
            return (transCoord.x * 1000f, transCoord.y * 1000f, transCoord.z * 1000f, iblPhiTheta.x, iblPhiTheta.y, spin);
        }
        else
            return (ap * 1000f, ml * 1000f, dv * 1000f, iblPhiTheta.x, iblPhiTheta.y, spin);
    }
    
}
