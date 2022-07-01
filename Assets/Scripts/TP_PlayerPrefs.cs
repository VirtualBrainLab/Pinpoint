using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TP_PlayerPrefs : MonoBehaviour
{
    // Settings
    private bool collisions;
    //private bool useIblBregma;
    private bool recordingRegionOnly;
    private bool useAcronyms;
    private bool depthFromBrain;
    private bool convertAPML2probeAxis;
    private int slice3d;
    private bool inplane;
    private int invivoTransform;
    private bool useIBLAngles;

    [SerializeField] TP_TrajectoryPlannerManager tpmanager;

    [SerializeField] Toggle collisionsToggle;
    //[SerializeField] Toggle bregmaToggle;
    [SerializeField] Toggle recordingRegionToggle;
    [SerializeField] Toggle acronymToggle;
    [SerializeField] Toggle depthToggle;
    [SerializeField] Toggle probeAxisToggle;
    [SerializeField] TMP_Dropdown slice3dDropdown;
    [SerializeField] Toggle inplaneToggle;
    [SerializeField] TMP_Dropdown invivoDropdown;
    [SerializeField] Toggle iblAngleToggle;

    [SerializeField] TP_QuestionDialogue qDialogue;

    // Saving probes
    // simplest solution: on exit, stringify the probes, and then recover them from the string

    // Start is called before the first frame update
    void Start()
    {
        collisions = LoadBoolPref("collisions", true);
        collisionsToggle.isOn = collisions;

        //useIblBregma = LoadBoolPref("bregma", true);
        ////bregmaToggle.isOn = useIblBregma;

        recordingRegionOnly = LoadBoolPref("recording", true);
        recordingRegionToggle.isOn = recordingRegionOnly;

        useAcronyms = LoadBoolPref("acronyms", true);
        acronymToggle.isOn = useAcronyms;

        depthFromBrain = LoadBoolPref("depth", true);
        depthToggle.isOn = depthFromBrain;

        convertAPML2probeAxis = LoadBoolPref("probeaxis", false);
        probeAxisToggle.isOn = convertAPML2probeAxis;

        slice3d = LoadIntPref("slice3d", 0);
        slice3dDropdown.SetValueWithoutNotify(slice3d);

        inplane = LoadBoolPref("inplane", true);
        tpmanager.SetInPlane(inplane);
        inplaneToggle.isOn = inplane;

        invivoTransform = LoadIntPref("stereotaxic", 1);
        tpmanager.SetInVivoTransformState(invivoTransform);
        invivoDropdown.SetValueWithoutNotify(invivoTransform);

        useIBLAngles = LoadBoolPref("iblangle", true);
        tpmanager.SetUseIBLAngles(useIBLAngles);
        iblAngleToggle.isOn = useIBLAngles;
    }
    public void AsyncStart()
    {
        if (qDialogue)
        {
            if (PlayerPrefs.GetInt("probecount", 0) > 0)
            {
                qDialogue.NewQuestion("Load previously saved probes?");
                qDialogue.SetYesCallback(LoadSavedProbes);
            }
        }
    }

    public void LoadSavedProbes()
    {
        int probeCount = PlayerPrefs.GetInt("probecount", 0);

        for (int i = 0; i < probeCount; i++)
        {
            float ap = PlayerPrefs.GetFloat("ap" + i);
            float ml = PlayerPrefs.GetFloat("ml" + i);
            float depth = PlayerPrefs.GetFloat("depth" + i);
            float phi = PlayerPrefs.GetFloat("phi" + i);
            float theta = PlayerPrefs.GetFloat("theta" + i);
            float spin = PlayerPrefs.GetFloat("spin" + i);
            int type = PlayerPrefs.GetInt("type" + i);

            Debug.Log(ap);
            Debug.Log(ml);
            Debug.Log(depth);
            Debug.Log(phi);
            Debug.Log(theta);
            Debug.Log(spin);

            tpmanager.AddNewProbe(type, ap, ml, depth, phi, theta, spin);
        }
    }

    public void SetUseIBLAngles(bool state)
    {
        useIBLAngles = state;
        PlayerPrefs.SetInt("iblangle", useIBLAngles ? 1 : 0);
    }

    public bool GetUseIBLAngles()
    {
        return useIBLAngles;
    }

    public void SetStereotaxic(int state)
    {
        invivoTransform = state;
        PlayerPrefs.SetInt("stereotaxic", invivoTransform);
    }

    public int GetStereotaxic()
    {
        return invivoTransform;
    }

    public void SetInplane(bool state)
    {
        inplane = state;
        PlayerPrefs.SetInt("inplane", inplane ? 1 : 0);
    }

    public bool GetInplane()
    {
        return inplane;
    }

    public void SetSlice3D(int state)
    {
        slice3d = state;
        PlayerPrefs.SetInt("slice3d", slice3d);
    }

    public int GetSlice3D()
    {
        return slice3d;
    }

    public void SetAPML2ProbeAxis(bool state)
    {
        convertAPML2probeAxis = state;
        PlayerPrefs.SetInt("probeaxis", convertAPML2probeAxis ? 1 : 0);
    }

    public bool GetAPML2ProbeAxis()
    {
        return convertAPML2probeAxis;
    }

    public void SetDepthFromBrain(bool state)
    {
        depthFromBrain = state;
        PlayerPrefs.SetInt("depth", depthFromBrain ? 1 : 0);
    }

    public bool GetDepthFromBrain()
    {
        return depthFromBrain;
    }

    public void SetAcronyms(bool state)
    {
        useAcronyms = state;
        PlayerPrefs.SetInt("acronyms", recordingRegionOnly ? 1 : 0);
    }

    public bool GetAcronyms()
    {
        return useAcronyms;
    }

    public void SetRecordingRegionOnly(bool state)
    {
        recordingRegionOnly = state;
        PlayerPrefs.SetInt("recording", recordingRegionOnly ? 1 : 0);
    }

    public bool GetRecordingRegionOnly()
    {
        return recordingRegionOnly;
    }

    public void SetCollisions(bool toggleCollisions)
    {
        collisions = toggleCollisions;
        PlayerPrefs.SetInt("collisions", collisions ? 1 : 0);
    }

    public bool GetCollisions()
    {
        return collisions;
    }

    //public void SetBregma(bool useBregma)
    //{
    //    useIblBregma = useBregma;
    //    PlayerPrefs.SetInt("bregma", useIblBregma ? 1 : 0);
    //}

    public bool GetBregma()
    {
        return true;
        //return useIblBregma;
    }
    private bool LoadBoolPref(string prefStr, bool defaultValue)
    {
        return PlayerPrefs.HasKey(prefStr) ? PlayerPrefs.GetInt(prefStr) == 1 : defaultValue;
    }

    private int LoadIntPref(string prefStr, int defaultValue)
    {
        return PlayerPrefs.HasKey(prefStr) ? PlayerPrefs.GetInt(prefStr) : defaultValue;
    }

    private void OnApplicationQuit()
    {
        List<TP_ProbeController> allProbes = tpmanager.GetAllProbes();
        for (int i = 0; i < allProbes.Count; i++)
        {
            TP_ProbeController probe = allProbes[i];
            (float ap, float ml, float depth, float phi, float theta, float spin) = probe.GetCoordinates();
            PlayerPrefs.SetFloat("ap" + i, ap);
            PlayerPrefs.SetFloat("ml" + i, ml);
            PlayerPrefs.SetFloat("depth" + i, depth);
            PlayerPrefs.SetFloat("phi" + i, phi);
            PlayerPrefs.SetFloat("theta" + i, theta);
            PlayerPrefs.SetFloat("spin" + i, spin);
            PlayerPrefs.SetInt("type" + i, probe.GetProbeType());
        }
        PlayerPrefs.SetInt("probecount", allProbes.Count);

        PlayerPrefs.Save();
    }
}
