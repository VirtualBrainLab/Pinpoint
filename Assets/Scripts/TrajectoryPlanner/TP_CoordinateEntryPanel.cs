using TMPro;
using TrajectoryPlanner;
using UnityEngine;

public class TP_CoordinateEntryPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _apText;
    [SerializeField] private TMP_Text _mlText;
    [SerializeField] private TMP_Text _dvText;
    [SerializeField] private TMP_Text _depthText;

    [SerializeField] private TMP_InputField _apField;
    [SerializeField] private TMP_InputField _mlField;
    [SerializeField] private TMP_InputField _dvField;
    [SerializeField] private TMP_InputField _depthField;
    [SerializeField] private TMP_InputField _phiField;
    [SerializeField] private TMP_InputField _thetaField;
    [SerializeField] private TMP_InputField _spinField;

    [SerializeField] private TrajectoryPlannerManager _tpmanager;
    
    [SerializeField] private TP_ProbeQuickSettings _probeQuickSettings;

    private ProbeManager _linkedProbe;

    private void Start()
    {
        _apField.onEndEdit.AddListener(delegate { ApplyPosition(); });
        _mlField.onEndEdit.AddListener(delegate { ApplyPosition(); });
        _dvField.onEndEdit.AddListener(delegate { ApplyPosition(); });
        _depthField.onEndEdit.AddListener(delegate { ApplyPosition(); });

        _phiField.onEndEdit.AddListener(delegate { ApplyAngles(); });
        _thetaField.onEndEdit.AddListener(delegate { ApplyAngles(); });
        _spinField.onEndEdit.AddListener(delegate { ApplyAngles(); });
    }

    public void LinkProbe(ProbeManager probeManager)
    {
        _linkedProbe = probeManager;
        // change the apmldv/depth text fields to match the prefix on this probe's insertion
        string prefix = _linkedProbe.GetProbeController().Insertion.CoordinateTransform.Prefix;
        _apText.text = prefix + "AP";
        _mlText.text = prefix + "ML";
        _dvText.text = prefix + "DV";
        _depthText.text = prefix + "Depth";
    }

    public void UnlinkProbe()
    {
        _linkedProbe = null;
    }

    public void UpdateText()
    {
        if (_linkedProbe == null)
        {
            _apField.text = "";
            _mlField.text = "";
            _dvField.text = "";
            _depthField.text = "";
            _phiField.text = "";
            _thetaField.text = "";
            _spinField.text = "";
            return;
        }

        Vector3 apmldv;
        Vector3 angles = _linkedProbe.GetProbeController().Insertion.angles;
        float depth = float.NaN;

        if (_linkedProbe.IsProbeInBrain())
        {
            (apmldv, depth) = _linkedProbe.GetSurfaceCoordinateT();
        }
        else
        {
            apmldv = _linkedProbe.GetProbeController().Insertion.apmldv;
        }

        float mult = _tpmanager.GetSetting_DisplayUM() ? 1000f : 1f;

        _apField.text = Round2Str(apmldv.x * mult);
        _mlField.text = Round2Str(apmldv.y * mult);
        _dvField.text = Round2Str(apmldv.z * mult);
        _depthField.text = float.IsNaN(depth) ? "nan" : Round2Str(depth * mult);

        // if in IBL angles, rotate the angles appropriately
        if (_tpmanager.GetSetting_UseIBLAngles())
            angles = Utils.World2IBL(angles);

        if (!_probeQuickSettings.IsFocused())
        {
            _phiField.text = Round2Str(angles.x);
            _thetaField.text = Round2Str(angles.y);
            _spinField.text = Round2Str(angles.z);
        }
    }

    private string Round2Str(float value)
    {
        if (float.IsNaN(value))
            return "nan";

        return _tpmanager.GetSetting_DisplayUM() ? ((int)value).ToString() : value.ToString("F3");
    }

    private void ApplyPosition()
    {
        //try
        //{
        //    float ap = (apField.text.Length > 0) ? float.Parse(apField.text) : 0;
        //    float ml = (mlField.text.Length > 0) ? float.Parse(mlField.text) : 0;
        //    float dv = (dvField.text.Length > 0) ? float.Parse(dvField.text) : 0;
        //    float depth = (depthField.text.Length > 0 && depthField.text != "nan") ? 
        //        float.Parse(depthField.text) + 200f : 
        //        0;

        //    Debug.LogError("TODO implement");
        //    //linkedProbe.GetProbeController().SetProbePositionTransformed(ap, ml, dv, depth/1000f);
        //}
        //catch
        //{
        //    Debug.Log("Bad formatting?");
        //}
    }

    private void ApplyAngles()
    {
        try
        {
            Vector3 angles = new Vector3((_phiField.text.Length > 0) ? float.Parse(_phiField.text) : 0,
                (_thetaField.text.Length > 0) ? float.Parse(_thetaField.text) : 0,
                (_spinField.text.Length > 0) ? float.Parse(_spinField.text) : 0);

            if (_tpmanager.GetSetting_UseIBLAngles())
                angles = Utils.IBL2World(angles);

            _linkedProbe.GetProbeController().SetProbeAngles(angles);
            if (_linkedProbe.HasGhost)
                _linkedProbe.GhostProbeManager.GetProbeController().SetProbeAngles(angles);
        }
        catch
        {
            Debug.Log("Bad formatting?");
        }
    }
}
