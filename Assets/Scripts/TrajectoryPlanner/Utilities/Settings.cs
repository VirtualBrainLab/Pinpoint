using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
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
public class Settings : MonoBehaviour
{
    public static Settings Instance;

    public static InternalData data;

    #region Probe settings
    // Collision detection
    private const string COLLISIONS_STR = "collisions";
    private const bool COLLISIONS_DEFAULT = true;
    [FormerlySerializedAs("collisionsToggle")][SerializeField] private Toggle _collisionsToggle;
    public UnityEvent DetectCollisionsChangedEvent;

    public static bool DetectCollisions
    {
        get { return data.DetectCollisions; }
        set
        {
            data.DetectCollisions = value;
            PlayerPrefs.SetInt(COLLISIONS_STR, data.DetectCollisions ? 1 : 0);
            Instance.DetectCollisionsChangedEvent.Invoke();
        }
    }

    // Convert APML rotation to the probe's axis rotation
    private const string APML2PROBE_STR = "apml2probe";
    private const bool APML2PROBE_DEFAULT = false;
    [FormerlySerializedAs("probeAxisToggle")][SerializeField] private Toggle _probeAxisToggle;
    public UnityEvent ConvertAPML2ProbeChangedEvent;

    public static bool ConvertAPML2Probe
    {
        get { return data.RotateAPML2ProbeAxis; }
        set
        {
            data.RotateAPML2ProbeAxis = value;
            PlayerPrefs.SetInt(APML2PROBE_STR, data.RotateAPML2ProbeAxis ? 1 : 0);
            Instance.ConvertAPML2ProbeChangedEvent.Invoke();
        }
    }

    private const string USEIBLANGLES_STR = "iblangles";
    private const bool USEIBLANGLES_DEFAULT = true;
    [FormerlySerializedAs("iblAngleToggle")][SerializeField] private Toggle _iblAngleToggle;
    public UnityEvent UseIBLAnglesChangedEvent;

    public static bool UseIBLAngles
    {
        get { return data.s_useIBLAngles; }
        set
        {
            data.s_useIBLAngles = value;
            PlayerPrefs.SetInt(USEIBLANGLES_STR, data.s_useIBLAngles ? 1 : 0);
            Instance.UseIBLAnglesChangedEvent.Invoke();
        }
    }

    private const string AXISCONTROL_STR = "axiscontrol";
    private const bool AXISCONTROL_DEFAULT = true;
    [FormerlySerializedAs("axisControlToggle")][SerializeField] private Toggle _axisControlToggle;
    public UnityEvent AxisControlChangedEvent;

    public static bool AxisControl
    {
        get { return data.s_axisControl; }
        set
        {
            data.s_axisControl = value;
            PlayerPrefs.SetInt(AXISCONTROL_STR, data.s_axisControl ? 1 : 0);
            Instance.AxisControlChangedEvent.Invoke();
        }
    }

    #endregion

    #region Area settings
    // Use acronyms or full areas
    private const string USEACRONYMS_STR = "acronyms";
    private const bool USEACRONYMS_DEFAULT = true;
    [FormerlySerializedAs("acronymToggle")][SerializeField] private Toggle _acronymToggle;
    public UnityEvent<bool> UseAcronymsChangedEvent;

    public static bool UseAcronyms
    {
        get { return data.s_useAcronyms; }
        set
        {
            data.s_useAcronyms = value;
            PlayerPrefs.SetInt(USEACRONYMS_STR, data.s_useAcronyms ? 1 : 0);
            Instance.UseAcronymsChangedEvent.Invoke(data.s_useAcronyms);
        }
    }

    // Use Beryl regions instead of ALL CCF regions
    private const string USEBERYL_STR = "beryl";
    private const bool USEBERYL_DEFAULT = true;
    [FormerlySerializedAs("useBerylToggle")][SerializeField] private Toggle _useBerylToggle;
    public UnityEvent<bool> UseBerylChangedEvent;

    public static bool UseBeryl
    {
        get { return data.s_useBeryl; }
        set
        {
            data.s_useBeryl = value;
            PlayerPrefs.SetInt(USEBERYL_STR, data.s_useBeryl ? 1 : 0);
            Instance.UseBerylChangedEvent.Invoke(data.s_useBeryl);
        }
    }

    #endregion

    #region Graphics settings

    // Show the surface coordinate sphere
    private const string SHOWSURFACECOORD_STR = "surfacecoord";
    private const bool SHOWSURFACECOORD_DEFAULT = true;
    [FormerlySerializedAs("surfaceToggle")][SerializeField] private Toggle _surfaceToggle;
    public UnityEvent SurfaceCoordChangedEvent;

    public static bool ShowSurfaceCoordinate
    {
        get { return data.ShowSurfaceCoord; }
        set
        {
            data.ShowSurfaceCoord = value;
            PlayerPrefs.SetInt(SHOWSURFACECOORD_STR, data.ShowSurfaceCoord ? 1 : 0);
            Instance.SurfaceCoordChangedEvent.Invoke();
        }
    }


    // Display the in-plane slice
    private const string SHOWINPLANE_STR = "inplane";
    private const bool SHOWINPLANE_DEFAULT = true;
    [FormerlySerializedAs("inplaneToggle")][SerializeField] private Toggle _inplaneToggle;
    public UnityEvent ShowInPlaneChangedEvent;

    public static bool ShowInPlaneSlice
    {
        get { return data.ShowInPlaneSlice; }
        set
        {
            data.ShowInPlaneSlice = value;
            PlayerPrefs.SetInt(SHOWINPLANE_STR, data.ShowInPlaneSlice ? 1 : 0);
            Instance.ShowInPlaneChangedEvent.Invoke();
        }
    }

    private const string GHOSTINACTIVEPROBES_STR = "ghostinactive";
    private const bool GHOSTINACTIVEPROBES_DEFAULT = true;
    [FormerlySerializedAs("ghostInactiveProbesToggle")][SerializeField] private Toggle _ghostInactiveProbesToggle;
    public UnityEvent GhostInactiveProbesChangedEvent;

    public static bool GhostInactiveProbes
    {
        get { return data.GhostInactiveProbes; }
        set
        {
            data.GhostInactiveProbes = value;
            PlayerPrefs.SetInt(GHOSTINACTIVEPROBES_STR, data.GhostInactiveProbes ? 1 : 0);
            Instance.GhostInactiveProbesChangedEvent.Invoke();
        }
    }


    private const string GHOSTINACTIVEAREAS_STR = "ghostinactive_areas";
    private const bool GHOSTINACTIVEAREAS_DEFAULT = false;
    [FormerlySerializedAs("ghostInactiveAreasToggle")][SerializeField] private Toggle _ghostInactiveAreasToggle;
    public UnityEvent GhostInactiveAreasChangedEvent;

    public static bool GhostInactiveAreas
    {
        get { return data.GhostInactiveAreas; }
        set
        {
            data.GhostInactiveAreas = value;
            PlayerPrefs.SetInt(GHOSTINACTIVEAREAS_STR, data.GhostInactiveAreas ? 1 : 0);
            Instance.GhostInactiveAreasChangedEvent.Invoke();
        }
    }

    private const string DISPLAYUM_STR = "displayum";
    private const bool DISPLAYUM_DEFAULT = true;
    [FormerlySerializedAs("displayUmToggle")][SerializeField] private Toggle _displayUmToggle;
    public UnityEvent DisplayUMChangedEvent;

    public static bool DisplayUM
    {
        get { return data.UnitsInUM; }
        set
        {
            data.UnitsInUM = value;
            PlayerPrefs.SetInt(DISPLAYUM_STR, data.UnitsInUM ? 1 : 0);
            Instance.DisplayUMChangedEvent.Invoke();
        }
    }

    private const string SHOWALLPROBEPANELS_STR = "showallpanels";
    private const bool SHOWALLPROBEPANELS_DEFAULT = true;
    [FormerlySerializedAs("showAllProbePanelsToggle")][SerializeField] private Toggle _showAllProbePanelsToggle;
    public UnityEvent ShowAllProbePanelsChangedEvent;

    public static bool ShowAllProbePanels
    {
        get { return data.ShowAllProbePanels; }
        set
        {
            data.ShowAllProbePanels = value;
            PlayerPrefs.SetInt(SHOWALLPROBEPANELS_STR, data.ShowAllProbePanels ? 1 : 0);
            Instance.ShowAllProbePanelsChangedEvent.Invoke();
        }
    }


    private const string PROBE_PANEL_HEIGHT_STR = "probepanelheight";
    private const float PROBE_PANEL_HEIGHT_DEFAULT = 1440f;
    [SerializeField] private Slider _probePanelHeightSlider;
    public UnityEvent<float> ProbePanelHeightChangedEvent;

    public static float ProbePanelHeight
    {
        get { return data.ProbePanelHeight; }
        set
        {
            data.ProbePanelHeight = value;
            PlayerPrefs.SetFloat(PROBE_PANEL_HEIGHT_STR, data.ProbePanelHeight);
            Instance.ProbePanelHeightChangedEvent.Invoke(data.ProbePanelHeight);
        }
    }

    #endregion

    #region Atlas

    // Display the 3D area slice
    private const string SHOW3DSLICE_STR = "slice3d";
    private const int SHOW3DSLICE_DEFAULT = 0;
    [FormerlySerializedAs("slice3dDropdown")][SerializeField] private TMP_Dropdown _slice3dDropdown;
    public UnityEvent<int> Slice3DChangedEvent;

    public static int Slice3DDropdownOption
    {
        get { return data.ShowAtlas3DSlices; }
        set
        {
            data.ShowAtlas3DSlices = value;
            PlayerPrefs.SetInt(SHOW3DSLICE_STR, data.ShowAtlas3DSlices);
            Instance.Slice3DChangedEvent.Invoke(data.ShowAtlas3DSlices);
        }
    }

    private const string RELATIVECOORD_STR = "relcoord";
    private readonly Vector3 RELCOORD_DEFAULT = new Vector3(5.4f, 5.7f, 0.332f);
    public UnityEvent<Vector3> RelativeCoordinateChangedEvent;

    public static Vector3 RelativeCoordinate
    {
        get { return data.s_relCoord; }
        set
        {
            data.s_relCoord = value;
            SaveVector3Pref(RELATIVECOORD_STR, data.s_relCoord);
            Instance.RelativeCoordinateChangedEvent.Invoke(data.s_relCoord);
        }
    }


    private const string INVIVO_STR = "invivo";
    private const int INVIVO_DEFAULT = 1;
    [FormerlySerializedAs("invivoDropdown")][SerializeField] private TMP_Dropdown _invivoDropdown;
    public UnityEvent<int> InvivoTransformChangedEvent;

    public static int InvivoTransform
    {
        get
        {
            return data.ActiveCoordinateTransformIndex;
        }
        set
        {
            data.ActiveCoordinateTransformIndex = value;
            PlayerPrefs.SetInt(INVIVO_STR, data.ActiveCoordinateTransformIndex);
            Instance.InvivoTransformChangedEvent.Invoke(data.ActiveCoordinateTransformIndex);
        }
    }

    private const string BREGMALAMBDA_STR = "bldist";
    private const float BREGMALAMBDA_DEFAULT = 4.15f;
    [SerializeField] private Slider _blSlider;
    public UnityEvent<float> BregmaLambdaChangedEvent;

    public static float BregmaLambdaDistance
    {
        get
        {
            return data.BregmaLambdaDistance;
        }
        set
        {
            data.BregmaLambdaDistance = value;
            PlayerPrefs.SetFloat(BREGMALAMBDA_STR, data.BregmaLambdaDistance);
            Instance.BregmaLambdaChangedEvent.Invoke(data.BregmaLambdaDistance);
        }
    }

    #endregion

    #region Ephys Link

    public UnityEvent<string> EphysLinkServerIpChangedEvent;
    public static string EphysLinkServerIp
    {
        get => data.EphysLinkServerIP;
        set
        {
            data.EphysLinkServerIP = value;
            PlayerPrefs.SetString("ephys_link_ip", value);
            PlayerPrefs.Save();
            Instance.EphysLinkServerIpChangedEvent.Invoke(value);
        }
    }
    
    public UnityEvent<int> EphysLinkServerPortChangedEvent;

    public static int EphysLinkServerPort
    {
        get => data.EphysLinkServerPort;
        set
        {
            data.EphysLinkServerPort = value;
            PlayerPrefs.SetInt("ephys_link_port", value);
            PlayerPrefs.Save();
            Instance.EphysLinkServerPortChangedEvent.Invoke(value);
        }
    }
    
    [SerializeField] private InputField _ephysLinkServerIpInput;
    [SerializeField] private InputField _ephysLinkServerPortInput;

    /// <summary>
    ///     Return if it has been more than 24 hours since the last launch.
    /// </summary>
    /// <returns>If it has been more than 24 hours since the last launch</returns>
    public static bool IsEphysLinkDataExpired()
    {
        var timestampString = PlayerPrefs.GetString("timestamp");
        if (timestampString == "") return false;

        return new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds() - long.Parse(timestampString) >= 86400;
    }


    public static string EphysLinkRightHandedManipulators
    {
        get => data.EphysLinkRightHandedManipulators;
        set
        {
            data.EphysLinkRightHandedManipulators = value;
            PlayerPrefs.SetString("ephys_link_right_handed_manipulators", value);
            PlayerPrefs.Save();
        }
    }

    #endregion

    #region Accounts
    private const string LOGGEDIN_STR = "stayloggedin";
    private const bool LOGGEDIN_DEFAULT = true;
    [FormerlySerializedAs("collisionsToggle")][SerializeField] private Toggle _stayLoggedInToggle;
    //public UnityEvent DetectCollisionsChangedEvent;

    public static bool StayLoggedIn
    {
        get { return data.AccountsLoginToggle; }
        set
        {
            data.AccountsLoginToggle = value;
            PlayerPrefs.SetInt(LOGGEDIN_STR, data.AccountsLoginToggle ? 1 : 0);
            //Instance.DetectCollisionsChangedEvent.Invoke();
        }
    }
    #endregion

    #region API
    private const string OPENEPHYS_DATA_STR = "openephys";
    private const bool OPENEPHYS_DATA_DEFAULT = false;
    [FormerlySerializedAs("collisionsToggle")][SerializeField] private Toggle _openEphysDataToggle;
    public UnityEvent<bool> OpenEphysDataToggleEvent;

    public static bool OpenEphysToggle
    {
        get { return data.OpenEphysAPIToggle; }
        set
        {
            data.OpenEphysAPIToggle = value;
            PlayerPrefs.SetInt(OPENEPHYS_DATA_STR, OPENEPHYS_DATA_DEFAULT ? 1 : 0);
            Instance.OpenEphysDataToggleEvent.Invoke(data.OpenEphysAPIToggle);
        }
    }

    private const string SGLX_DATA_STR = "spikeglx";
    private const bool SGLX_DATA_DEFAULT = false;
    [FormerlySerializedAs("collisionsToggle")][SerializeField] private Toggle _spikeGLXDataToggle;
    public UnityEvent<bool> SpikeGLXDataToggleEvent;

    public static bool SpikeGLXToggle
    {
        get { return data.SpikeGLXAPIToggle; }
        set
        {
            data.SpikeGLXAPIToggle = value;
            PlayerPrefs.SetInt(SGLX_DATA_STR, SGLX_DATA_DEFAULT ? 1 : 0);
            Instance.SpikeGLXDataToggleEvent.Invoke(data.SpikeGLXAPIToggle);
        }
    }
    #endregion

    #region Unity

    private void Awake()
    {
        // Set Singleton
        if (Instance != null)
            Debug.LogError("Make sure there is only one Settings object in the scene!");
        Instance = this;
    }
    
    private void Start()
    {
        LoadSettings();
        ApplySettings();
    }

    /// <summary>
    /// Load and apply settings from PlayerPrefs
    /// </summary>
    private void LoadSettings()
    {
        DetectCollisions = LoadBoolPref(COLLISIONS_STR, COLLISIONS_DEFAULT);
        ConvertAPML2Probe = LoadBoolPref(APML2PROBE_STR, APML2PROBE_DEFAULT);
        UseIBLAngles = LoadBoolPref(USEIBLANGLES_STR, USEIBLANGLES_DEFAULT);
        UseAcronyms = LoadBoolPref(USEACRONYMS_STR, USEACRONYMS_DEFAULT);
        Slice3DDropdownOption = LoadIntPref(SHOW3DSLICE_STR, SHOW3DSLICE_DEFAULT);
        ShowSurfaceCoordinate = LoadBoolPref(SHOWSURFACECOORD_STR, SHOWSURFACECOORD_DEFAULT);
        ShowInPlaneSlice = LoadBoolPref(SHOWINPLANE_STR, SHOWINPLANE_DEFAULT);
        UseBeryl = LoadBoolPref(USEBERYL_STR, USEBERYL_DEFAULT);
        GhostInactiveProbes = LoadBoolPref(GHOSTINACTIVEPROBES_STR, GHOSTINACTIVEPROBES_DEFAULT);
        GhostInactiveAreas = LoadBoolPref(GHOSTINACTIVEAREAS_STR, GHOSTINACTIVEAREAS_DEFAULT);
        RelativeCoordinate = LoadVector3Pref(RELATIVECOORD_STR, RELCOORD_DEFAULT);
        DisplayUM = LoadBoolPref(DISPLAYUM_STR, DISPLAYUM_DEFAULT);
        AxisControl = LoadBoolPref(AXISCONTROL_STR, AXISCONTROL_DEFAULT);
        ShowAllProbePanels = LoadBoolPref(SHOWALLPROBEPANELS_STR, SHOWALLPROBEPANELS_DEFAULT);
        InvivoTransform = LoadIntPref(INVIVO_STR, INVIVO_DEFAULT);
        BregmaLambdaDistance = LoadFloatPref(BREGMALAMBDA_STR, BREGMALAMBDA_DEFAULT);

        // Probes
        ProbePanelHeight = LoadFloatPref(PROBE_PANEL_HEIGHT_STR, PROBE_PANEL_HEIGHT_DEFAULT);

        // API
        OpenEphysToggle = LoadBoolPref(OPENEPHYS_DATA_STR, OPENEPHYS_DATA_DEFAULT);
        SpikeGLXToggle = LoadBoolPref(SGLX_DATA_STR, SGLX_DATA_DEFAULT);

        // accounts
        StayLoggedIn = LoadBoolPref(LOGGEDIN_STR, LOGGEDIN_DEFAULT);

        // ephys link
        EphysLinkServerIp = LoadStringPref("ephys_link_ip", "");
        EphysLinkServerPort = LoadIntPref("ephys_link_port", 8081);
        EphysLinkRightHandedManipulators = LoadStringPref("ephys_link_right_handed_manipulators", "");
    }

    /// <summary>
    /// Apply all settings from the current data stored in the Instance.data struct
    /// </summary>
    private void ApplySettings()
    {
        // Load preferences from memory and set UI elements
        _collisionsToggle.SetIsOnWithoutNotify(DetectCollisions);

        _probeAxisToggle.SetIsOnWithoutNotify(ConvertAPML2Probe);

        _iblAngleToggle.SetIsOnWithoutNotify(UseIBLAngles);

        _acronymToggle.SetIsOnWithoutNotify(UseAcronyms);

        _slice3dDropdown.SetValueWithoutNotify(Slice3DDropdownOption);

        _surfaceToggle.SetIsOnWithoutNotify(ShowSurfaceCoordinate);

        _inplaneToggle.SetIsOnWithoutNotify(ShowInPlaneSlice);

        _useBerylToggle.SetIsOnWithoutNotify(UseBeryl);

        _ghostInactiveProbesToggle.SetIsOnWithoutNotify(GhostInactiveProbes);

        _ghostInactiveAreasToggle.SetIsOnWithoutNotify(GhostInactiveAreas);

        // Probes
        _probePanelHeightSlider.SetValueWithoutNotify(ProbePanelHeight);

        // Default to Bregma
        _displayUmToggle.SetIsOnWithoutNotify(DisplayUM);

        _axisControlToggle.SetIsOnWithoutNotify(AxisControl);

        _showAllProbePanelsToggle.SetIsOnWithoutNotify(ShowAllProbePanels);

        // Atlas
        _invivoDropdown.SetValueWithoutNotify(InvivoTransform);

        _blSlider.value = BregmaLambdaDistance;

        // Accounts
        _stayLoggedInToggle.SetIsOnWithoutNotify(StayLoggedIn);

        // API
        _openEphysDataToggle.SetIsOnWithoutNotify(OpenEphysToggle);

        // Ephys link
        _ephysLinkServerIpInput.text = data.EphysLinkServerIP;

        _ephysLinkServerPortInput.text = data.EphysLinkServerPort.ToString();
    }

    #endregion

    #region Helper functions for booleans/integers/strings

    /// <summary>
    /// Load a boolean preference
    /// </summary>
    /// <param name="prefStr">string accessor</param>
    /// <param name="defaultValue">default value if the preference is not set</param>
    /// <returns></returns>
    private bool LoadBoolPref(string prefStr, bool defaultValue)
    {
        return PlayerPrefs.HasKey(prefStr) ? PlayerPrefs.GetInt(prefStr) == 1 : defaultValue;
    }

    /// <summary>
    /// Load an integer preference
    /// </summary>
    /// <param name="prefStr">string accessor</param>
    /// <param name="defaultValue">default value if the preference is not set</param>
    /// <returns></returns>
    private int LoadIntPref(string prefStr, int defaultValue)
    {
        return PlayerPrefs.HasKey(prefStr) ? PlayerPrefs.GetInt(prefStr) : defaultValue;
    }

    /// <summary>
    /// Load a string preference
    /// </summary>
    /// <param name="prefStr">string accessor</param>
    /// <param name="defaultValue">default value if the preference is not set</param>
    /// <returns></returns>
    private string LoadStringPref(string prefStr, string defaultValue)
    {
        return PlayerPrefs.HasKey(prefStr) ? PlayerPrefs.GetString(prefStr) : defaultValue;
    }

    /// <summary>
    /// Load a float preference
    /// </summary>
    /// <param name="prefStr">string accessor</param>
    /// <param name="defaultValue">default value if the setting is blank</param>
    /// <returns></returns>
    private float LoadFloatPref(string prefStr, float defaultValue)
    {
        return PlayerPrefs.HasKey(prefStr) ? PlayerPrefs.GetFloat(prefStr) : defaultValue;
    }

    private Vector3 LoadVector3Pref(string prefStr, Vector3 defaultValue)
    {
        if (PlayerPrefs.HasKey(prefStr + "_x"))
        {
            float ap = PlayerPrefs.GetFloat(prefStr + "_x");
            float ml = PlayerPrefs.GetFloat(prefStr + "_y");
            float dv = PlayerPrefs.GetFloat(prefStr + "_z");

            return new Vector3(ap, ml, dv);
        }
        else
            return defaultValue;
    }

    private static void SaveVector3Pref(string prefStr, Vector3 value)
    {
        PlayerPrefs.SetFloat(prefStr + "_x", value.x);
        PlayerPrefs.SetFloat(prefStr + "_y", value.y);
        PlayerPrefs.SetFloat(prefStr + "_z", value.z);
    }

    #endregion

    #region Probe saving/loading

    /// <summary>
    /// Return an array with information about the positions of probes that were saved from the last session
    /// </summary>
    /// <returns></returns>
    public static string[] LoadSavedProbeData()
    {
        int probeCount = PlayerPrefs.GetInt("probecount", 0);

        string[] savedProbes = new string[probeCount];

        for (int i = 0; i < probeCount; i++)
            savedProbes[i] = PlayerPrefs.GetString($"probe_{i}");

        return savedProbes;
    }

    /// <summary>
    /// Save the data about all of the probes passed in through allProbeData
    /// </summary>
    /// <param name="allProbeData">tip position, angles, and type for probes</param>
    public static void SaveCurrentProbeData(string[] allProbeData)
    {
        for (int i = 0; i < allProbeData.Length; i++)
            PlayerPrefs.SetString($"probe_{i}", allProbeData[i]);

        PlayerPrefs.SetInt("probecount", allProbeData.Length);
        PlayerPrefs.SetString("timestamp",
            new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString("D16"));

        PlayerPrefs.Save();
    }

    #endregion


    #region Editor
#if UNITY_EDITOR
    [MenuItem("Tools/Reset PlayerPrefs")]
    static void ResetPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("(Settings) All PlayerPrefs Deleted");
    }
#endif
    #endregion


    #region Serialization

    public static string ToSaveString()
    {
        return JsonUtility.ToJson(data);
    }

    public static void RecoverFromSaveString(string settingsString)
    {
        data = JsonUtility.FromJson<InternalData>(settingsString);
        // Run the apply function to recover all settings properly
        Instance.ApplySettings();
    }

    [Serializable]
    public struct InternalData
    {
        // Graphics and UI
        public bool ShowSurfaceCoord;
        public bool ShowInPlaneSlice;

        // Transparency
        public bool GhostInactiveProbes;
        public bool GhostInactiveAreas;

        public bool UnitsInUM;

        // Probes
        public bool ShowAllProbePanels;
        public float ProbePanelHeight;
        public bool DetectCollisions;
        public bool RotateAPML2ProbeAxis;

        public int ShowAtlas3DSlices;
        public Vector3 s_relCoord;
        public int ActiveCoordinateTransformIndex;
        public float BregmaLambdaDistance;

        // Ephys link
        public string EphysLinkServerIP;
        public int EphysLinkServerPort;
        public string EphysLinkRightHandedManipulators;

        // Accounts
        public bool AccountsLoginToggle;

        // API
        public bool OpenEphysAPIToggle;
        public bool SpikeGLXAPIToggle;

        public bool s_useIBLAngles;
        public bool s_axisControl;
        public bool s_useAcronyms;
        public bool s_useBeryl;
    }
    #endregion
}