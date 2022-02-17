using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TP_PlayerPrefs : MonoBehaviour
{
    // Settings
    private bool collisions;
    private bool useIblBregma;
    private bool recordingRegionOnly;
    private bool useAcronyms;
    private bool depthFromBrain;
    private bool convertAPML2probeAxis;
    private bool slice3d;
    private bool inplane;

    [SerializeField] TrajectoryPlannerManager tpmanager;

    [SerializeField] Toggle collisionsToggle;
    [SerializeField] Toggle bregmaToggle;
    [SerializeField] Toggle recordingRegionToggle;
    [SerializeField] Toggle acronymToggle;
    [SerializeField] Toggle depthToggle;
    [SerializeField] Toggle probeAxisToggle;
    [SerializeField] Toggle slice3dToggle;
    [SerializeField] Toggle inplaneToggle;

    // Saving probes
    // simplest solution: on exit, stringify the probes, and then recover them from the string

    // Start is called before the first frame update
    void Start()
    {

        collisions = LoadBoolPref("collisions", false);
        collisionsToggle.isOn = collisions;

        useIblBregma = LoadBoolPref("bregma", true);
        bregmaToggle.isOn = useIblBregma;

        recordingRegionOnly = LoadBoolPref("recording", true);
        recordingRegionToggle.isOn = recordingRegionOnly;

        useAcronyms = LoadBoolPref("acronyms", false);
        acronymToggle.isOn = useAcronyms;

        depthFromBrain = LoadBoolPref("depth", true);
        depthToggle.isOn = depthFromBrain;

        convertAPML2probeAxis = LoadBoolPref("probeaxis", false);
        probeAxisToggle.isOn = convertAPML2probeAxis;

        slice3d = LoadBoolPref("slice3d", false);
        slice3dToggle.isOn = slice3d;

        inplane = LoadBoolPref("inplane", true);
        tpmanager.SetInPlane(inplane);
        inplaneToggle.isOn = inplane;
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

    public void SetSlice3D(bool state)
    {
        slice3d = state;
        PlayerPrefs.SetInt("slice3d", slice3d ? 1 : 0);
    }

    public bool GetSlice3D()
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
        recordingRegionOnly = state;
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

    public void SetBregma(bool useBregma)
    {
        useIblBregma = useBregma;
        PlayerPrefs.SetInt("bregma", useIblBregma ? 1 : 0);
    }

    public bool GetBregma()
    {
        return useIblBregma;
    }
    private bool LoadBoolPref(string prefStr, bool defaultValue)
    {
        return PlayerPrefs.HasKey(prefStr) ? PlayerPrefs.GetInt(prefStr) == 1 : defaultValue;
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}
