using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TP_CoordinateEntryPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField apField;
    [SerializeField] private TMP_InputField mlField;
    [SerializeField] private TMP_InputField dvField;
    [SerializeField] private TMP_InputField depthField;
    [SerializeField] private TMP_InputField phiField;
    [SerializeField] private TMP_InputField thetaField;
    [SerializeField] private TMP_InputField spinField;

    [SerializeField] private TP_TrajectoryPlannerManager tpmanager;

    private void Start()
    {
        apField.onValueChanged.AddListener(delegate { Apply(); });
        mlField.onValueChanged.AddListener(delegate { Apply(); });
        dvField.onValueChanged.AddListener(delegate { Apply(); });
        depthField.onValueChanged.AddListener(delegate { Apply(); });
        phiField.onValueChanged.AddListener(delegate { Apply(); });
        thetaField.onValueChanged.AddListener(delegate { Apply(); });
        spinField.onValueChanged.AddListener(delegate { Apply(); });
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            // If the user presses Return, apply values and close the window
            Apply();
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        SetTextValues(tpmanager.GetActiveProbeController());
    }

    public void SetTextValues(TP_ProbeController probeController)
    {
        (float ap, float ml, float dv, float depth, float phi, float theta, float spin) = probeController.GetCoordinates();
        apField.text = ap.ToString();
        mlField.text = ml.ToString();
        dvField.text = dv.ToString();
        depthField.text = depth.ToString();

        // convert phi/theta to 
        phiField.text = phi.ToString();
        thetaField.text = theta.ToString();
        spinField.text = spin.ToString();
    }

    public void Apply()
    {
        try
        {
            float ap = (apField.text.Length > 0) ? float.Parse(apField.text) : 0;
            float ml = (mlField.text.Length > 0) ? float.Parse(mlField.text) : 0;
            float dv = (dvField.text.Length > 0) ? float.Parse(dvField.text) : 0;
            float depth = (depthField.text.Length > 0) ? float.Parse(depthField.text) : 0;
            float phi = (phiField.text.Length > 0) ? float.Parse(phiField.text) : 0;
            float theta = (thetaField.text.Length > 0) ? float.Parse(thetaField.text) : 0;
            float spin = (spinField.text.Length > 0) ? float.Parse(spinField.text) : 0;

            tpmanager.ManualCoordinateEntry(ap, ml, dv, depth, phi, theta, spin);
        }
        catch
        {
            Debug.Log("Bad formatting?");
        }
    }
}
