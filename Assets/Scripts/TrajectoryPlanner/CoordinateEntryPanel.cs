using TMPro;
using TrajectoryPlanner;
using UnityEngine;

public class CoordinateEntryPanel : MonoBehaviour
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
    
    [SerializeField] private TP_ProbeQuickSettings _probeQuickSettings;

    private void Awake()
    {
        _apField.onSubmit.AddListener(delegate { ApplyPosition(); });
        _mlField.onSubmit.AddListener(delegate { ApplyPosition(); });
        _dvField.onSubmit.AddListener(delegate { ApplyPosition(); });
        _depthField.onSubmit.AddListener(delegate { ApplyPosition(); });

        _phiField.onEndEdit.AddListener(delegate { ApplyAngles(); });
        _thetaField.onEndEdit.AddListener(delegate { ApplyAngles(); });
        _spinField.onEndEdit.AddListener(delegate { ApplyAngles(); });
    }

    public void NewProbe()
    {
        // change the apmldv/depth text fields to match the prefix on this probe's insertion
        string prefix = ProbeManager.ActiveProbeManager.ProbeController.Insertion.CoordinateTransform.Prefix;
        _apText.text = prefix + "AP";
        _mlText.text = prefix + "ML";
        _dvText.text = prefix + "DV";
        _depthText.text = prefix + "Depth";
    }

    public void UpdateText()
    {
        if (ProbeManager.ActiveProbeManager == null)
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
        Vector3 angles = ProbeManager.ActiveProbeManager.ProbeController.Insertion.angles;
        float depth = float.NaN;

        if (ProbeManager.ActiveProbeManager.IsProbeInBrain())
        {
            (apmldv, depth) = ProbeManager.ActiveProbeManager.GetSurfaceCoordinateT();
        }
        else
        {
            apmldv = ProbeManager.ActiveProbeManager.ProbeController.Insertion.apmldv;
        }

        float mult = Settings.DisplayUM ? 1000f : 1f;

        _apField.text = Round2Str(apmldv.x * mult);
        _mlField.text = Round2Str(apmldv.y * mult);
        _dvField.text = Round2Str(apmldv.z * mult);
        _depthField.text = float.IsNaN(depth) ? "nan" : Round2Str(depth * mult);

        // if in IBL angles, rotate the angles appropriately
        if (Settings.UseIBLAngles)
            angles = TP_Utils.World2IBL(angles);

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

        return Settings.DisplayUM ? ((int)value).ToString() : value.ToString("F3");
    }

    private void ApplyPosition()
    {
        try
        {
            float ap = (_apField.text.Length > 0) ? float.Parse(_apField.text) : 0;
            float ml = (_mlField.text.Length > 0) ? float.Parse(_mlField.text) : 0;
            float dv = (_dvField.text.Length > 0) ? float.Parse(_dvField.text) : 0;
            float depth = (_depthField.text.Length > 0 && _depthField.text != "nan") ?
                float.Parse(_depthField.text) :
                0;

            Vector4 position = new Vector4(ap, ml, dv, depth) / 1000f;

            ProbeManager.ActiveProbeManager.ProbeController.SetProbePosition(position);
        }
        catch
        {
            Debug.Log("Bad formatting?");
        }
    }

    private void ApplyAngles()
    {
        try
        {
            Vector3 angles = new Vector3((_phiField.text.Length > 0) ? float.Parse(_phiField.text) : 0,
                (_thetaField.text.Length > 0) ? float.Parse(_thetaField.text) : 0,
                (_spinField.text.Length > 0) ? float.Parse(_spinField.text) : 0);

            if (Settings.UseIBLAngles)
                angles = TP_Utils.IBL2World(angles);

            ProbeManager.ActiveProbeManager.ProbeController.SetProbeAngles(angles);
            if (ProbeManager.ActiveProbeManager.HasGhost)
                ProbeManager.ActiveProbeManager.GhostProbeManager.ProbeController.SetProbeAngles(angles);
        }
        catch
        {
            Debug.Log("Bad formatting?");
        }
    }
}
