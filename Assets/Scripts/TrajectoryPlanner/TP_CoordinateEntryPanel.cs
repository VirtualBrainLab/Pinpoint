using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TrajectoryPlanner;

public class TP_CoordinateEntryPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField apField_t;
    [SerializeField] private TMP_InputField mlField_t;
    [SerializeField] private TMP_InputField dvField_t;
    [SerializeField] private TMP_InputField phiField_t;
    [SerializeField] private TMP_InputField thetaField_t;
    [SerializeField] private TMP_InputField spinField_t;

    [SerializeField] private TMP_InputField apField_i;
    [SerializeField] private TMP_InputField mlField_i;
    [SerializeField] private TMP_InputField dvField_i;
    [SerializeField] private TMP_InputField depthField_i;
    [SerializeField] private TMP_InputField phiField_i;
    [SerializeField] private TMP_InputField thetaField_i;
    [SerializeField] private TMP_InputField spinField_i;

    [SerializeField] private GameObject tipPanel;
    [SerializeField] private GameObject insertionPanel;


    [SerializeField] private TrajectoryPlannerManager tpmanager;

    private int mode = 0; // when false, using insertion + depth mode

    private void Start()
    {
        foreach (TMP_InputField inputField in transform.GetComponentsInChildren<TMP_InputField>())
            inputField.onValueChanged.AddListener(delegate { Apply(); });
        // [TODO: mode should be saved in PlayerPrefs]
        SetMode(mode);
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

    public void SetTextValues(ProbeManager probeController)
    {
        if (mode == 0)
        {
            (float ap, float ml, float dv, float phi, float theta, float spin) = probeController.GetCoordinates();
            apField_t.text = ap.ToString();
            mlField_t.text = ml.ToString();
            dvField_t.text = dv.ToString();
            phiField_t.text = phi.ToString();
            thetaField_t.text = theta.ToString();
            spinField_t.text = spin.ToString();
        }
        else if (mode == 1)
        {
            (float ap, float ml, float dv, float depth, float phi, float theta, float spin) = probeController.GetCoordinatesSurface();
            apField_i.text = ap.ToString();
            mlField_i.text = ml.ToString();
            dvField_i.text = dv.ToString();
            depthField_i.text = depth.ToString();
            phiField_i.text = phi.ToString();
            thetaField_i.text = theta.ToString();
            spinField_i.text = spin.ToString();
        }
    }

    public void Apply()
    {
        try
        {
            TMP_InputField apField = mode == 0 ? apField_t : apField_i;
            TMP_InputField mlField = mode == 0 ? mlField_t : mlField_i;
            TMP_InputField dvField = mode == 0 ? dvField_t : dvField_i;
            TMP_InputField depthField = depthField_i;
            TMP_InputField phiField = mode == 0 ? phiField_t : phiField_i;
            TMP_InputField thetaField = mode == 0 ? thetaField_t : thetaField_i;
            TMP_InputField spinField = mode == 0 ? spinField_t : spinField_i;

            float ap = (apField.text.Length > 0) ? float.Parse(apField.text) : 0;
            float ml = (mlField.text.Length > 0) ? float.Parse(mlField.text) : 0;
            float dv = (dvField.text.Length > 0) ? float.Parse(dvField.text) : 0;
            float depth = (depthField.text.Length > 0) ? float.Parse(depthField.text) : 0;
            float phi = (phiField.text.Length > 0) ? float.Parse(phiField.text) : 0;
            float theta = (thetaField.text.Length > 0) ? float.Parse(thetaField.text) : 0;
            float spin = (spinField.text.Length > 0) ? float.Parse(spinField.text) : 0;

            if (mode==0)
                tpmanager.ManualCoordinateEntryTransformed(ap, ml, dv, phi, theta, spin);
            else
                tpmanager.ManualCoordinateEntryTransformed(ap, ml, dv, phi, theta, spin, depth);
        }
        catch
        {
            Debug.Log("Bad formatting?");
        }
    }

    public void SetMode(int mode)
    {
        Debug.Log(mode);
        this.mode = mode;
        if (mode==0)
        {
            tipPanel.SetActive(true);
            insertionPanel.SetActive(false);
        }
        else if (mode==1)
        {
            tipPanel.SetActive(false);
            insertionPanel.SetActive(true);
        }
        else
            Debug.LogError("Manual coordinate entry mode missing");
    }
}
