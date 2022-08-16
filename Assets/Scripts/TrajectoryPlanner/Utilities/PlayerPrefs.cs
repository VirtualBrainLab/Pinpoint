using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Trajectory Planner PlayerPreferences saving/loading
/// 
/// To use this class:
/// 1. define a new float/bool/int/string in the settings
/// 2. link the UI element that this corresponds to
/// 3. add corresponding getter/setter functions
/// 4. in Awake() load the setting and set the ui element
/// 5. if needed, in tpmanager Start() or any other Start() function, call the getter on your setting and do something with that
/// 
/// Note that PlayerPrefs data is not available in Awake() calls in other components!!
/// </summary>
public class PlayerPrefs : MonoBehaviour
{
    // Settings
    private bool collisions;
    private bool recordingRegionOnly;
    private bool useAcronyms;
    private bool depthFromBrain;
    private bool convertAPML2probeAxis;
    private int slice3d;
    private bool inplane;
    private int invivoTransform;
    private bool useIBLAngles;
    private bool showSurfaceCoord;
    //private bool useIblBregma;
    private string _sensapexLinkServerIp;
    private int _sensapexLinkServerPort;
    private bool _axisControl;

    [SerializeField] Toggle collisionsToggle;
    [SerializeField] Toggle recordingRegionToggle;
    [SerializeField] Toggle acronymToggle;
    [SerializeField] Toggle depthToggle;
    [SerializeField] Toggle probeAxisToggle;
    [SerializeField] TMP_Dropdown slice3dDropdown;
    [SerializeField] Toggle inplaneToggle;
    [SerializeField] TMP_Dropdown invivoDropdown;
    [SerializeField] Toggle iblAngleToggle;
    [SerializeField] Toggle surfaceToggle;
    //[SerializeField] Toggle bregmaToggle;
    [SerializeField] TMP_InputField sensapexLinkServerIpInput;
    [SerializeField] TMP_InputField sensapexLinkServerPortInput;
    [SerializeField] Toggle axisControlToggle;


    /// <summary>
    /// On Awake() load the preferences and toggle the corresponding UI elements
    /// </summary>
    void Awake()
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
        inplaneToggle.isOn = inplane;

        invivoTransform = LoadIntPref("stereotaxic", 1);
        invivoDropdown.SetValueWithoutNotify(invivoTransform);

        useIBLAngles = LoadBoolPref("iblangle", true);
        iblAngleToggle.isOn = useIBLAngles;

        showSurfaceCoord = LoadBoolPref("surface", true);
        surfaceToggle.isOn = showSurfaceCoord;
        
        _sensapexLinkServerIp = LoadStringPref("sensapex_ip", "localhost");
        sensapexLinkServerIpInput.text = _sensapexLinkServerIp;
        
        _sensapexLinkServerPort = LoadIntPref("sensapex_port", 8080);
        sensapexLinkServerPortInput.text = _sensapexLinkServerPort.ToString();

        _axisControl = LoadBoolPref("axis_control", false);
        axisControlToggle.isOn = _axisControl;
    }

    /// <summary>
    /// Return an array with information about the positions (in CCF) of probes that were saved from the last session
    /// </summary>
    /// <returns></returns>
    public (Vector3 tipPos, float depth, Vector3 angles, int type)[] LoadSavedProbes()
    {
        int probeCount = UnityEngine.PlayerPrefs.GetInt("probecount", 0);

        var savedProbes = new (Vector3 tipPos, float depth, Vector3 angles, int type)[probeCount];

        for (int i = 0; i < probeCount; i++)
        {
            float ap = UnityEngine.PlayerPrefs.GetFloat("ap" + i);
            float ml = UnityEngine.PlayerPrefs.GetFloat("ml" + i);
            float dv = UnityEngine.PlayerPrefs.GetFloat("dv" + i);
            float depth = UnityEngine.PlayerPrefs.GetFloat("depth" + i);
            float phi = UnityEngine.PlayerPrefs.GetFloat("phi" + i);
            float theta = UnityEngine.PlayerPrefs.GetFloat("theta" + i);
            float spin = UnityEngine.PlayerPrefs.GetFloat("spin" + i);
            int type = UnityEngine.PlayerPrefs.GetInt("type" + i);

            savedProbes[i] = (new Vector3(ap, ml, dv), depth, new Vector3(phi, theta, spin), type);
        }

        return savedProbes;
    }

    #region Getters/Setters

    public void SetAxisControl(bool state)
    {
        _axisControl = state;
        UnityEngine.PlayerPrefs.SetInt("axis_control", _axisControl ? 1 : 0);
    }

    public bool GetAxisControl()
    {
        return _axisControl;
    }

    public void SetSurfaceCoord(bool state)
    {
        showSurfaceCoord = state;
        UnityEngine.PlayerPrefs.SetInt("surface", showSurfaceCoord ? 1 : 0);
    }

    public bool GetSurfaceCoord()
    {
        return showSurfaceCoord;
    }

    public void SetUseIBLAngles(bool state)
    {
        useIBLAngles = state;
        UnityEngine.PlayerPrefs.SetInt("iblangle", useIBLAngles ? 1 : 0);
    }

    public bool GetUseIBLAngles()
    {
        return useIBLAngles;
    }

    public void SetStereotaxic(int state)
    {
        invivoTransform = state;
        UnityEngine.PlayerPrefs.SetInt("stereotaxic", invivoTransform);
    }

    public int GetStereotaxic()
    {
        return invivoTransform;
    }

    public void SetInplane(bool state)
    {
        inplane = state;
        UnityEngine.PlayerPrefs.SetInt("inplane", inplane ? 1 : 0);
    }

    public bool GetInplane()
    {
        return inplane;
    }

    public void SetSlice3D(int state)
    {
        slice3d = state;
        UnityEngine.PlayerPrefs.SetInt("slice3d", slice3d);
    }

    public int GetSlice3D()
    {
        return slice3d;
    }

    public void SetAPML2ProbeAxis(bool state)
    {
        convertAPML2probeAxis = state;
        UnityEngine.PlayerPrefs.SetInt("probeaxis", convertAPML2probeAxis ? 1 : 0);
    }

    public bool GetAPML2ProbeAxis()
    {
        return convertAPML2probeAxis;
    }

    public void SetDepthFromBrain(bool state)
    {
        depthFromBrain = state;
        UnityEngine.PlayerPrefs.SetInt("depth", depthFromBrain ? 1 : 0);
    }

    public bool GetDepthFromBrain()
    {
        return depthFromBrain;
    }

    public void SetAcronyms(bool state)
    {
        useAcronyms = state;
        UnityEngine.PlayerPrefs.SetInt("acronyms", recordingRegionOnly ? 1 : 0);
    }

    public bool GetAcronyms()
    {
        return useAcronyms;
    }

    public void SetRecordingRegionOnly(bool state)
    {
        recordingRegionOnly = state;
        UnityEngine.PlayerPrefs.SetInt("recording", recordingRegionOnly ? 1 : 0);
    }

    public bool GetRecordingRegionOnly()
    {
        return recordingRegionOnly;
    }

    public void SetCollisions(bool toggleCollisions)
    {
        collisions = toggleCollisions;
        UnityEngine.PlayerPrefs.SetInt("collisions", collisions ? 1 : 0);
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
    
    public string GetServerIp()
    {
        return _sensapexLinkServerIp;
    }
    
    public int GetServerPort()
    {
        return _sensapexLinkServerPort;
    }

    #endregion

    #region Helper functions for booleans/integers/strings
    private bool LoadBoolPref(string prefStr, bool defaultValue)
    {
        return UnityEngine.PlayerPrefs.HasKey(prefStr) ? UnityEngine.PlayerPrefs.GetInt(prefStr) == 1 : defaultValue;
    }

    private int LoadIntPref(string prefStr, int defaultValue)
    {
        return UnityEngine.PlayerPrefs.HasKey(prefStr) ? UnityEngine.PlayerPrefs.GetInt(prefStr) : defaultValue;
    }

    private string LoadStringPref(string prefStr, string defaultValue)
    {
        return UnityEngine.PlayerPrefs.HasKey(prefStr) ? UnityEngine.PlayerPrefs.GetString(prefStr) : defaultValue;
    }

    #endregion

    /// <summary>
    /// Save the data about all of the probes passed in through allProbeData in CCF coordinates, note that depth is ignored 
    /// </summary>
    /// <param name="allProbeData">tip position, angles, and type for probes in CCF coordinates</param>
    public void SaveCurrentProbeData((float ap, float ml, float dv, float depth, float phi, float theta, float spin, int type)[] allProbeData)
    {
        for (int i = 0; i < allProbeData.Length; i++)
        {

            UnityEngine.PlayerPrefs.SetFloat("ap" + i, allProbeData[i].ap);
            UnityEngine.PlayerPrefs.SetFloat("ml" + i, allProbeData[i].ml);
            UnityEngine.PlayerPrefs.SetFloat("dv" + i, allProbeData[i].dv);
            UnityEngine.PlayerPrefs.SetFloat("depth" + i, allProbeData[i].depth);
            UnityEngine.PlayerPrefs.SetFloat("phi" + i, allProbeData[i].phi);
            UnityEngine.PlayerPrefs.SetFloat("theta" + i, allProbeData[i].theta);
            UnityEngine.PlayerPrefs.SetFloat("spin" + i, allProbeData[i].spin);
            UnityEngine.PlayerPrefs.SetInt("type" + i, allProbeData[i].type);
        }
        UnityEngine.PlayerPrefs.SetInt("probecount", allProbeData.Length);

        UnityEngine.PlayerPrefs.Save();
    }

    public void SaveSensapexLinkConnectionData(string serverIp, int serverPort)
    {
        UnityEngine.PlayerPrefs.SetString("sensapex_ip", serverIp);
        UnityEngine.PlayerPrefs.SetInt("sensapex_port", serverPort);
        UnityEngine.PlayerPrefs.Save();
    }
}
