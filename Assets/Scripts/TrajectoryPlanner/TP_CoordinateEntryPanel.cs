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

    private ProbeManager linkedProbe;

    private void Start()
    {
        foreach (TMP_InputField inputField in transform.GetComponentsInChildren<TMP_InputField>())
            inputField.onEndEdit.AddListener(delegate { Apply(); });
    }

    private void Update()
    {
        if (tpmanager.MovedThisFrame())
            SetTextValues();
    }

    public void LinkProbe(ProbeManager probeManager)
    {
        linkedProbe = probeManager;
    }

    public void SetTextValues()
    {
        if (linkedProbe == null) return;

        (float ap, float ml, float dv, float depth, float phi, float theta, float spin) = linkedProbe.GetCoordinatesSurface();
        apField.text = ap.ToString();
        mlField.text = ml.ToString();
        dvField.text = dv.ToString();
        depthField.text = depth.ToString();
        phiField.text = phi.ToString();
        thetaField.text = theta.ToString();
        spinField.text = spin.ToString();
    }

    public void Apply()
    {
        Debug.Log("Value changed");
        try
        {
            float ap = (apField.text.Length > 0) ? float.Parse(apField.text) : 0;
            float ml = (mlField.text.Length > 0) ? float.Parse(mlField.text) : 0;
            float dv = (dvField.text.Length > 0) ? float.Parse(dvField.text) : 0;
            float depth = (depthField.text.Length > 0) ? float.Parse(depthField.text) : 0;
            float phi = (phiField.text.Length > 0) ? float.Parse(phiField.text) : 0;
            float theta = (thetaField.text.Length > 0) ? float.Parse(thetaField.text) : 0;
            float spin = (spinField.text.Length > 0) ? float.Parse(spinField.text) : 0;

            linkedProbe.GetProbeController().ManualCoordinateEntryTransformed(ap, ml, dv, phi, theta, spin, depth);
        }
        catch
        {
            Debug.Log("Bad formatting?");
        }
    }
}
