using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TrajectoryPlanner;

public class TP_CoordinateEntryPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField apField;
    [SerializeField] private TMP_InputField mlField;
    [SerializeField] private TMP_InputField dvField;
    [SerializeField] private TMP_InputField depthField;
    [SerializeField] private TMP_InputField phiField;
    [SerializeField] private TMP_InputField thetaField;
    [SerializeField] private TMP_InputField spinField;

    [SerializeField] private TrajectoryPlannerManager tpmanager;
    
    [SerializeField] private TP_ProbeQuickSettings probeQuickSettings;

    private ProbeManager linkedProbe;

    private void Start()
    {
        apField.onEndEdit.AddListener(delegate { ApplyPosition(); });
        mlField.onEndEdit.AddListener(delegate { ApplyPosition(); });
        dvField.onEndEdit.AddListener(delegate { ApplyPosition(); });
        depthField.onEndEdit.AddListener(delegate { ApplyPosition(); });

        phiField.onEndEdit.AddListener(delegate { ApplyAngles(); });
        thetaField.onEndEdit.AddListener(delegate { ApplyAngles(); });
        spinField.onEndEdit.AddListener(delegate { ApplyAngles(); });
    }

    private void Update()
    {
        if (tpmanager.MovedThisFrame() && !probeQuickSettings.IsFocused())
            SetTextValues();
    }

    public void LinkProbe(ProbeManager probeManager)
    {
        linkedProbe = probeManager;
    }

    public void SetTextValues()
    {
        if (linkedProbe == null) return;

        (float ap, float ml, float dv, float phi, float theta, float spin) = linkedProbe.GetProbeController().GetCoordinates();
        (float aps, float mls, float dvs, float depth, _, _, _) = linkedProbe.GetCoordinatesSurface();
        apField.text = Round2Str(aps);
        mlField.text = Round2Str(mls);
        dvField.text = Round2Str(dvs);
        depthField.text = Round2Str(depth);
        if (!tpmanager.GetActiveProbeController().IsConnectedToManipulator())
        {
            phiField.text = Round2Str(phi);
            thetaField.text = Round2Str(theta);
            spinField.text = Round2Str(spin);
        }
    }

    private string Round2Str(float value)
    {
        if (float.IsNaN(value))
            return "nan";
        return ((int)value).ToString();
    }

    private void ApplyPosition()
    {
        try
        {
            float ap = (apField.text.Length > 0) ? float.Parse(apField.text) : 0;
            float ml = (mlField.text.Length > 0) ? float.Parse(mlField.text) : 0;
            float dv = (dvField.text.Length > 0) ? float.Parse(dvField.text) : 0;
            float depth = (depthField.text.Length > 0 && depthField.text != "nan") ? 
                float.Parse(depthField.text) + 200f : 
                0;

            linkedProbe.GetProbeController().SetProbePositionTransformed(ap, ml, dv, depth/1000f);
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
            float ap = (apField.text.Length > 0) ? float.Parse(apField.text) : 0;
            float ml = (mlField.text.Length > 0) ? float.Parse(mlField.text) : 0;
            float dv = (dvField.text.Length > 0) ? float.Parse(dvField.text) : 0;
            float depth = (depthField.text.Length > 0 && depthField.text != "nan") ?
                float.Parse(depthField.text) + 200f :
                0;
            float phi = (phiField.text.Length > 0) ? float.Parse(phiField.text) : 0;
            float theta = (thetaField.text.Length > 0) ? float.Parse(thetaField.text) : 0;
            float spin = (spinField.text.Length > 0) ? float.Parse(spinField.text) : 0;

            Debug.Log((ap, ml, dv, depth, phi, theta, spin));

            linkedProbe.GetProbeController().SetProbeAnglesTransformed(phi, theta, spin);
        }
        catch
        {
            Debug.Log("Bad formatting?");
        }
    }
}
