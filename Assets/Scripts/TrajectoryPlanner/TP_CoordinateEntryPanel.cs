using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TrajectoryPlanner;

public class TP_CoordinateEntryPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text apText;
    [SerializeField] private TMP_Text mlText;
    [SerializeField] private TMP_Text dvText;
    [SerializeField] private TMP_Text depthText;

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

    private void LateUpdate()
    {
        if (!tpmanager.MovedThisFrame() || probeQuickSettings.IsFocused()) return;
        UpdateText();
    }

    public void LinkProbe(ProbeManager probeManager)
    {
        linkedProbe = probeManager;
        // change the apmldv/depth text fields to match the prefix on this probe's insertion
        string prefix = linkedProbe.GetProbeController().Insertion.CoordinateTransform.Prefix;
        apText.text = prefix + "AP";
        mlText.text = prefix + "ML";
        dvText.text = prefix + "DV";
        depthText.text = prefix + "Depth";
    }

    public void UpdateText()
    {
        if (linkedProbe == null) return;

        //Vector3 apmldv = linkedProbe.GetProbeController().GetInsertion().apmldv;
        Vector3 angles = linkedProbe.GetProbeController().Insertion.angles;

        (_, Vector3 entryCoordT, float depthT) = linkedProbe.GetSurfaceCoordinateTransformed();

        float mult = tpmanager.GetSetting_DisplayUM() ? 1000f : 1f;

        apField.text = Round2Str(entryCoordT.x * mult);
        mlField.text = Round2Str(entryCoordT.y * mult);
        dvField.text = Round2Str(entryCoordT.z * mult);
        depthField.text = float.IsNaN(depthT) ? "nan" : Round2Str(depthT * mult);

        // if in IBL angles, rotate the angles appropriately
        if (tpmanager.GetSetting_UseIBLAngles())
            angles = Utils.World2IBL(angles);

        if (!probeQuickSettings.IsFocused())
        {
            phiField.text = Round2Str(angles.x);
            thetaField.text = Round2Str(angles.y);
            spinField.text = Round2Str(angles.z);
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

            Debug.LogError("TODO implement");
            //linkedProbe.GetProbeController().SetProbePositionTransformed(ap, ml, dv, depth/1000f);
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
            Vector3 angles = new Vector3((phiField.text.Length > 0) ? float.Parse(phiField.text) : 0,
                (thetaField.text.Length > 0) ? float.Parse(thetaField.text) : 0,
                (spinField.text.Length > 0) ? float.Parse(spinField.text) : 0);

            if (tpmanager.GetSetting_UseIBLAngles())
                angles = Utils.IBL2World(angles);

            linkedProbe.GetProbeController().SetProbeAngles(angles);
        }
        catch
        {
            Debug.Log("Bad formatting?");
        }
    }
}
