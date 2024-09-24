using BrainAtlas;
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
    [SerializeField] private TMP_InputField _yawField;
    [SerializeField] private TMP_InputField _pitchField;
    [SerializeField] private TMP_InputField _rollField;
    
    [SerializeField] private TP_ProbeQuickSettings _probeQuickSettings;

    private AngleConvention _angleConvention;

    private void Awake()
    {
        _apField.onSubmit.AddListener(delegate { ApplyPosition(); });
        _mlField.onSubmit.AddListener(delegate { ApplyPosition(); });
        _dvField.onSubmit.AddListener(delegate { ApplyPosition(); });
        _depthField.onSubmit.AddListener(delegate { ApplyPosition(); });

        _yawField.onEndEdit.AddListener(delegate { ApplyAngles(); });
        _pitchField.onEndEdit.AddListener(delegate { ApplyAngles(); });
        _rollField.onEndEdit.AddListener(delegate { ApplyAngles(); });
    }

    public void UpdateAxisLabels()
    {
        // change the apmldv/depth text fields to match the prefix on this probe's insertion
        if (ProbeManager.ActiveProbeManager != null)
        {
            string prefix = BrainAtlasManager.ActiveAtlasTransform.Prefix;

            if (Settings.ConvertAPML2Probe)
            {
                _apText.text = prefix + "Forward";
                _mlText.text = prefix + "Right";
            }
            else
            {
                _apText.text = prefix + "AP";
                _mlText.text = prefix + "ML";
            }

            _dvText.text = prefix + "DV";
            _depthText.text = prefix + "Depth";
        }
    }

    public void UpdateInteractable(Vector4 pos, Vector3 ang)
    {
        _apField.interactable = pos.x != 0f;
        _mlField.interactable = pos.y != 0f;
        _dvField.interactable = pos.z != 0f;
        _depthField.interactable = pos.w != 0f;

        _yawField.interactable = ang.x != 0f;
        _pitchField.interactable = ang.y != 0f;
        _rollField.interactable = ang.z != 0f;
    }

    public void UpdateText()
    {
        if (ProbeManager.ActiveProbeManager == null)
        {
            _apField.text = "";
            _mlField.text = "";
            _dvField.text = "";
            _depthField.text = "";
            _yawField.text = "";
            _pitchField.text = "";
            _rollField.text = "";
            return;
        }

        Vector3 apmldv;
        Vector3 angles = ProbeManager.ActiveProbeManager.ProbeController.Insertion.Angles;
        float depth = float.NaN;

        if (ProbeManager.ActiveProbeManager.IsProbeInBrain())
        {
            (apmldv, depth) = ProbeManager.ActiveProbeManager.GetSurfaceCoordinateT();
        }
        else
        {
            apmldv = ProbeManager.ActiveProbeManager.ProbeController.Insertion.APMLDV;
        }

        if (Settings.ConvertAPML2Probe)
        {
            float cos = Mathf.Cos(-angles.x * Mathf.Deg2Rad);
            float sin = Mathf.Sin(-angles.x * Mathf.Deg2Rad);

            float xRot = apmldv.x * cos - apmldv.y * sin;
            float yRot = apmldv.x * sin + apmldv.y * cos;

            apmldv.x = xRot;
            apmldv.y = yRot;
        }

        float mult = Settings.DisplayUM ? 1000f : 1f;

        _apField.text = Round2Str(apmldv.x * mult);
        _mlField.text = Round2Str(apmldv.y * mult);
        _dvField.text = Round2Str(apmldv.z * mult);
        _depthField.text = float.IsNaN(depth) ? "nan" : Round2Str(depth * mult);

        // if in IBL angles, rotate the angles appropriately
        angles = _angleConvention.ToConvention(angles);

        if (!_probeQuickSettings.IsFocused())
        {
            _yawField.text = Round2Str(angles.x);
            _pitchField.text = Round2Str(angles.y);
            _rollField.text = Round2Str(angles.z);
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
            Vector3 angles = new Vector3((_yawField.text.Length > 0) ? float.Parse(_yawField.text) : 0,
                (_pitchField.text.Length > 0) ? float.Parse(_pitchField.text) : 0,
                (_rollField.text.Length > 0) ? float.Parse(_rollField.text) : 0);

            angles = _angleConvention.FromConvention(angles);

            if (Settings.ConvertAPML2Probe)
                Debug.LogWarning("Converting back from probe angles is not yet implemented");

            ProbeManager.ActiveProbeManager.ProbeController.SetProbeAngles(angles);
        }
        catch
        {
            Debug.Log("Bad formatting?");
        }
    }

    public void SetActiveAngleConvention(AngleConvention newAngleConvention)
    {
        _angleConvention = newAngleConvention;
    }
}
