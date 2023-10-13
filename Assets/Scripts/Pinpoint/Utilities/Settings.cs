using System;
using System.Collections.Generic;
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
    #region Static vars / constants
    public static Settings Instance;
    private static InternalData data;

    public const string DATA_STR = "settings-data";
    #endregion

    #region Probe settings
    // Collision detection
    private const bool COLLISIONS_DEFAULT = true;
    [FormerlySerializedAs("collisionsToggle")][SerializeField] private Toggle _collisionsToggle;
    public UnityEvent DetectCollisionsChangedEvent;

    public static bool DetectCollisions
    {
        get { return data.DetectCollisions; }
        set
        {
            data.DetectCollisions = value;
            Save();
            Instance.DetectCollisionsChangedEvent.Invoke();
        }
    }

    // Convert APML rotation to the probe's axis rotation
    private const bool APML2PROBE_DEFAULT = false;
    [FormerlySerializedAs("probeAxisToggle")][SerializeField] private Toggle _probeAxisToggle;
    public UnityEvent ConvertAPML2ProbeChangedEvent;

    public static bool ConvertAPML2Probe
    {
        get { return data.RotateAPML2ProbeAxis; }
        set
        {
            data.RotateAPML2ProbeAxis = value;
            Save();
            Instance.ConvertAPML2ProbeChangedEvent.Invoke();

            // because this can be set through code, the UI has to maintain state relative to it
            Instance._probeAxisToggle.SetIsOnWithoutNotify(value);
        }
    }

    private const bool USEIBLANGLES_DEFAULT = false;
    [FormerlySerializedAs("iblAngleToggle")][SerializeField] private Toggle _iblAngleToggle;
    public UnityEvent UseIBLAnglesChangedEvent;

    public static bool UseIBLAngles
    {
        get { return data.UseIBLAngles; }
        set
        {
            data.UseIBLAngles = value;
            Save();
            Instance.UseIBLAnglesChangedEvent.Invoke();
        }
    }

    private const bool AXISCONTROL_DEFAULT = true;
    [FormerlySerializedAs("axisControlToggle")][SerializeField] private Toggle _axisControlToggle;
    public UnityEvent AxisControlChangedEvent;

    public static bool AxisControl
    {
        get { return data.AxisControl; }
        set
        {
            data.AxisControl = value;
            Save();
            Instance.AxisControlChangedEvent.Invoke();
        }
    }

    private const int PROBE_SPEED_DEFAULT = 1;
    private const int PROBE_SPEED_MAX = 3;
    private const int PROBE_SPEED_MIN = 0;
    public UnityEvent<int> ProbeSpeedChangedEvent;

    public static int ProbeSpeed
    {
        get { return data.ProbeSpeed; }
        set
        {
            data.ProbeSpeed = value > PROBE_SPEED_MAX ? PROBE_SPEED_MAX :
                value < PROBE_SPEED_MIN ? PROBE_SPEED_MIN :
                value;
            Save();
            Instance.ProbeSpeedChangedEvent.Invoke(data.ProbeSpeed);
        }
    }

    #endregion

    #region Area settings
    // Use acronyms or full areas
    private const bool USEACRONYMS_DEFAULT = true;
    [FormerlySerializedAs("acronymToggle")][SerializeField] private Toggle _acronymToggle;
    public UnityEvent<bool> UseAcronymsChangedEvent;

    public static bool UseAcronyms
    {
        get { return data.UseAcronyms; }
        set
        {
            data.UseAcronyms = value;
            Save();
            Instance.UseAcronymsChangedEvent.Invoke(data.UseAcronyms);
        }
    }

    // Use Beryl regions instead of ALL CCF regions
    private const bool USEBERYL_DEFAULT = true;
    [FormerlySerializedAs("useBerylToggle")][SerializeField] private Toggle _useBerylToggle;
    public UnityEvent<bool> UseBerylChangedEvent;

    public static bool UseBeryl
    {
        get { return data.UseBeryl; }
        set
        {
            data.UseBeryl = value;
            Save();
            Instance.UseBerylChangedEvent.Invoke(data.UseBeryl);
        }
    }

    #endregion

    #region Graphics settings

    // Show the surface coordinate sphere
    private const bool SHOWSURFACECOORD_DEFAULT = true;
    [FormerlySerializedAs("surfaceToggle")][SerializeField] private Toggle _surfaceToggle;
    public UnityEvent SurfaceCoordChangedEvent;

    public static bool ShowSurfaceCoordinate
    {
        get { return data.ShowSurfaceCoord; }
        set
        {
            data.ShowSurfaceCoord = value;
            Save();
            Instance.SurfaceCoordChangedEvent.Invoke();
        }
    }


    // Display the in-plane slice
    private const bool SHOWINPLANE_DEFAULT = true;
    [FormerlySerializedAs("inplaneToggle")][SerializeField] private Toggle _inplaneToggle;
    public UnityEvent ShowInPlaneChangedEvent;

    public static bool ShowInPlaneSlice
    {
        get { return data.ShowInPlaneSlice; }
        set
        {
            data.ShowInPlaneSlice = value;
            Save();
            Instance.ShowInPlaneChangedEvent.Invoke();
        }
    }

    private const bool GHOSTINACTIVEPROBES_DEFAULT = true;
    [FormerlySerializedAs("ghostInactiveProbesToggle")][SerializeField] private Toggle _ghostInactiveProbesToggle;
    public UnityEvent GhostInactiveProbesChangedEvent;

    public static bool GhostInactiveProbes
    {
        get { return data.GhostInactiveProbes; }
        set
        {
            data.GhostInactiveProbes = value;
            Save();
            Instance.GhostInactiveProbesChangedEvent.Invoke();
        }
    }


    private const bool GHOSTINACTIVEAREAS_DEFAULT = false;
    [FormerlySerializedAs("ghostInactiveAreasToggle")][SerializeField] private Toggle _ghostInactiveAreasToggle;
    public UnityEvent GhostInactiveAreasChangedEvent;

    public static bool GhostInactiveAreas
    {
        get { return data.GhostInactiveAreas; }
        set
        {
            data.GhostInactiveAreas = value;
            Save();
            Instance.GhostInactiveAreasChangedEvent.Invoke();
        }
    }

    private const bool DISPLAYUM_DEFAULT = true;
    [FormerlySerializedAs("displayUmToggle")][SerializeField] private Toggle _displayUmToggle;
    public UnityEvent DisplayUMChangedEvent;

    public static bool DisplayUM
    {
        get => data.UnitsInUM;
        set
        {
            data.UnitsInUM = value;
            Save();
            Instance.DisplayUMChangedEvent.Invoke();
        }
    }

    private const bool SHOWALLPROBEPANELS_DEFAULT = true;
    [FormerlySerializedAs("showAllProbePanelsToggle")][SerializeField] private Toggle _showAllProbePanelsToggle;
    public UnityEvent ShowAllProbePanelsChangedEvent;

    public static bool ShowAllProbePanels
    {
        get { return data.ShowAllProbePanels; }
        set
        {
            data.ShowAllProbePanels = value;
            Save();
            Instance.ShowAllProbePanelsChangedEvent.Invoke();
        }
    }


    private const float PROBE_PANEL_HEIGHT_DEFAULT = 1440f;
    [SerializeField] private Slider _probePanelHeightSlider;
    public UnityEvent<float> ProbePanelHeightChangedEvent;

    public static float ProbePanelHeight
    {
        get { return data.ProbePanelHeight; }
        set
        {
            data.ProbePanelHeight = value;
            Save();
            Instance.ProbePanelHeightChangedEvent.Invoke(data.ProbePanelHeight);
        }
    }

    #endregion

    #region Atlas
    private const string ATLAS_DEFAULT = "allen_mouse_25um";
    public static Action<string> AtlasChanged;

    public static string AtlasName
    {
        get { return data.AtlasName; }
        set
        {
            data.AtlasName = value;
            Save();
            AtlasChanged?.Invoke(data.AtlasName);
        }
    }


    // Display the 3D area slice
    private const int SHOW3DSLICE_DEFAULT = 0;
    [FormerlySerializedAs("slice3dDropdown")][SerializeField] private TMP_Dropdown _slice3dDropdown;
    public UnityEvent<int> Slice3DChangedEvent;

    public static int Slice3DDropdownOption
    {
        get { return data.ShowAtlas3DSlices; }
        set
        {
            data.ShowAtlas3DSlices = value;
            Save();
            Instance.Slice3DChangedEvent.Invoke(data.ShowAtlas3DSlices);
        }
    }

    private readonly Vector3 RELCOORD_DEFAULT = new Vector3(5.2f, 5.7f, 0.332f);
    public UnityEvent<Vector3> RelativeCoordinateChangedEvent;

    public static Vector3 RelativeCoordinate
    {
        get { return data.RelativeCoord; }
        set
        {
            data.RelativeCoord = value;
            Save();
            Debug.Log("Invoking relative coordinate set");
            Instance.RelativeCoordinateChangedEvent.Invoke(data.RelativeCoord);
        }
    }


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
            Save();
            Instance.InvivoTransformChangedEvent.Invoke(data.ActiveCoordinateTransformIndex);
        }
    }

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
            Save();
            Instance.BregmaLambdaChangedEvent.Invoke(data.BregmaLambdaDistance);
        }
    }

    #endregion

    #region Ephys Link

    public UnityEvent EphysLinkServerInfoLoaded;

    public UnityEvent<string> EphysLinkServerIpChangedEvent;
    public static string EphysLinkServerIp
    {
        get => data.EphysLinkServerIP;
        set
        {
            data.EphysLinkServerIP = value;
            Save();
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
            Save();
            Instance.EphysLinkServerPortChangedEvent.Invoke(value);
        }
    }
    
    [SerializeField] private TMP_InputField _ephysLinkServerIpInput;
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
    private const bool LOGGEDIN_DEFAULT = true;
    [FormerlySerializedAs("collisionsToggle")][SerializeField] private Toggle _stayLoggedInToggle;

    public static bool StayLoggedIn
    {
        get { return data.AccountsLoginToggle; }
        set
        {
            data.AccountsLoginToggle = value;
            Save();
        }
    }
    #endregion

    #region API
    private const bool OPENEPHYS_DATA_DEFAULT = false;
    [FormerlySerializedAs("collisionsToggle")][SerializeField] private Toggle _openEphysDataToggle;
    public UnityEvent<bool> OpenEphysDataToggleEvent;

    public static bool OpenEphysToggle
    {
        get { return data.OpenEphysAPIToggle; }
        set
        {
            data.OpenEphysAPIToggle = value;
            Save();
            Instance.OpenEphysDataToggleEvent.Invoke(data.OpenEphysAPIToggle);
        }
    }

    private const string OPENEPHYS_TARGET_DEFAULT = "http://localhost:37497";
    [SerializeField] private TMP_InputField _openEphysTargetInput;

    public static string OpenEphysTarget
    {
        get { return data.OpenEphysAPITarget; }
        set
        {
            data.OpenEphysAPITarget = value;
            Save();
        }
    }

    private const bool SGLX_DATA_DEFAULT = false;
    [FormerlySerializedAs("collisionsToggle")][SerializeField] private Toggle _spikeGLXDataToggle;
    public UnityEvent<bool> SpikeGLXDataToggleEvent;

    public static bool SpikeGLXToggle
    {
        get { return data.SpikeGLXAPIToggle; }
        set
        {
            data.SpikeGLXAPIToggle = value;
            Save();
            Instance.SpikeGLXDataToggleEvent.Invoke(data.SpikeGLXAPIToggle);
        }
    }

    private const string SGLX_TARGET_DEFAULT = "127.0.0.1:4142";
    [SerializeField] private TMP_InputField _spikeGLXTargetInput;

    public static string SpikeGLXTarget
    {
        get { return data.SpikeGLXAPITarget; }
        set
        {
            data.SpikeGLXAPITarget = value;
            Save();
        }
    }

    private const string SGLX_HELLO_DEFAULT = "C:\\HelloSGLX-win\\HelloSGLX.exe";
    public UnityEvent<string> SpikeGLXHelloPathEvent;

    public static string SpikeGLXHelloPath
    {
        get { return data.SpikeGLXHelloPath; }
        set
        {
            data.SpikeGLXHelloPath = value;
            Save();
            Instance.SpikeGLXHelloPathEvent.Invoke(data.SpikeGLXHelloPath);
        }
    }

    private const float API_UPDATE_RATE_DEFAULT = 10f;
    [SerializeField] private Slider _apiUpdateRateSlider;
    public UnityEvent<float> APIUpdateRateEvent;

    public static float APIUpdateRate
    {
        get { return data.APIUpdateRate; }
        set
        {
            data.APIUpdateRate = value;
            Save();
            Instance.APIUpdateRateEvent.Invoke(data.APIUpdateRate);
        }
    }

    #endregion

    #region Camera

    private const float CZOOM_DEFAULT = 5;
    public UnityEvent<float> CameraZoomChangedEvent;

    public static float CameraZoom
    {
        get { return data.CameraZoom; }
        set
        {
            data.CameraZoom = value;
            Save();
        }
    }

    private readonly Vector3 CROTATION_DEFAULT = Vector3.zero;
    public UnityEvent<Vector3> CameraRotationChangedEvent;

    public static Vector3 CameraRotation
    {
        get { return data.CameraRotation; }
        set
        {
            data.CameraRotation = value;
            Save();
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
        Load();
    }


    public static void Save()
    {
        PlayerPrefs.SetString(DATA_STR, Data2String());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load and apply settings from PlayerPrefs
    /// </summary>
    private void Load(bool fromDefaults = false)
    {
        if (!fromDefaults && PlayerPrefs.HasKey(DATA_STR))
        {
#if UNITY_EDITOR
            Debug.Log("(Settings) Loading settings from PlayerPrefs");
#endif
            data = JsonUtility.FromJson<InternalData>(PlayerPrefs.GetString(DATA_STR));
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log("(Settings) Resetting settings to defaults");
#endif
            data = new InternalData();

            // probe
            data.DetectCollisions = COLLISIONS_DEFAULT;
            data.RotateAPML2ProbeAxis = APML2PROBE_DEFAULT;
            data.UseIBLAngles = USEIBLANGLES_DEFAULT;
            data.AxisControl = AXISCONTROL_DEFAULT;
            data.ProbeSpeed = PROBE_SPEED_DEFAULT;

            // areas
            data.UseAcronyms = USEACRONYMS_DEFAULT;
            data.UseBeryl = USEBERYL_DEFAULT;

            // graphics
            data.ShowSurfaceCoord = SHOWSURFACECOORD_DEFAULT;
            data.GhostInactiveAreas = GHOSTINACTIVEAREAS_DEFAULT;
            data.GhostInactiveProbes = GHOSTINACTIVEPROBES_DEFAULT;
            data.ShowAtlas3DSlices = SHOW3DSLICE_DEFAULT;
            data.ShowInPlaneSlice = SHOWINPLANE_DEFAULT;
            data.ProbePanelHeight = PROBE_PANEL_HEIGHT_DEFAULT;
            data.UnitsInUM = DISPLAYUM_DEFAULT;
            data.ShowAllProbePanels = SHOWALLPROBEPANELS_DEFAULT;

            // atlas
            data.AtlasName = ATLAS_DEFAULT;
            data.ShowAtlas3DSlices = SHOW3DSLICE_DEFAULT;
            data.RelativeCoord = RELCOORD_DEFAULT;
            data.ActiveCoordinateTransformIndex = INVIVO_DEFAULT;
            data.BregmaLambdaDistance = BREGMALAMBDA_DEFAULT;

            // ephys link
            data.EphysLinkServerIP = "";
            data.EphysLinkServerPort = 8081;
            data.EphysLinkRightHandedManipulators = "";

            // API
            data.OpenEphysAPIToggle = OPENEPHYS_DATA_DEFAULT;
            data.OpenEphysAPITarget = OPENEPHYS_TARGET_DEFAULT;
            data.SpikeGLXAPIToggle = SGLX_DATA_DEFAULT;
            data.SpikeGLXAPITarget = SGLX_TARGET_DEFAULT;
            data.SpikeGLXHelloPath = SGLX_HELLO_DEFAULT;
            data.APIUpdateRate = API_UPDATE_RATE_DEFAULT;

            // Accounts
            data.AccountsLoginToggle = LOGGEDIN_DEFAULT;

            // Camera
            data.CameraZoom = CZOOM_DEFAULT;
            data.CameraRotation = CROTATION_DEFAULT;

            // Do an initial save so the default values are stored
            Save();
        }

        // Apply the settings so they are visible in the UI
        Apply();
    }

    public static void Load(string settingsStr)
    {
#if UNITY_EDITOR
        Debug.Log("(Settings) Loading settings from query string on WebGL");
#endif
        data = JsonUtility.FromJson<InternalData>(settingsStr);
        Instance.Apply();
    }

    /// <summary>
    /// Apply all settings from the current data stored in the Instance.data struct
    /// </summary>
    private void Apply()
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
        ProbeSpeedChangedEvent.Invoke(data.ProbeSpeed);

        // Default to Bregma
        _displayUmToggle.SetIsOnWithoutNotify(DisplayUM);

        _axisControlToggle.SetIsOnWithoutNotify(AxisControl);

        _showAllProbePanelsToggle.SetIsOnWithoutNotify(ShowAllProbePanels);

        // Atlas
        AtlasName = data.AtlasName;
        _invivoDropdown.SetValueWithoutNotify(InvivoTransform);
        // the relative coordinate actually needs to be set, since it gets propagated downstream
        RelativeCoordinate = data.RelativeCoord;
        _blSlider.SetValueWithoutNotify(BregmaLambdaDistance);

        // Accounts
        _stayLoggedInToggle.SetIsOnWithoutNotify(StayLoggedIn);

        // API
        _openEphysDataToggle.SetIsOnWithoutNotify(OpenEphysToggle);
        _openEphysTargetInput.SetTextWithoutNotify(OpenEphysTarget);
        _spikeGLXDataToggle.SetIsOnWithoutNotify(SpikeGLXToggle);
        _spikeGLXTargetInput.SetTextWithoutNotify(SpikeGLXTarget);
        SpikeGLXHelloPathEvent.Invoke(Settings.SpikeGLXHelloPath);
        _apiUpdateRateSlider.SetValueWithoutNotify(APIUpdateRate);

        // Ephys link
        _ephysLinkServerIpInput.text = data.EphysLinkServerIP;
        _ephysLinkServerPortInput.text = data.EphysLinkServerPort.ToString();
        EphysLinkServerInfoLoaded.Invoke();

        // Camera
        CameraZoomChangedEvent.Invoke(CameraZoom);
        CameraRotationChangedEvent.Invoke(CameraRotation);

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
    public static void SaveCurrentProbeData(List<string> allProbeData)
    {
        for (int i = 0; i < allProbeData.Count; i++)
            PlayerPrefs.SetString($"probe_{i}", allProbeData[i]);

        PlayerPrefs.SetInt("probecount", allProbeData.Count);
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

    public static string Data2String()
    {
        return JsonUtility.ToJson(data);
    }

    [Serializable]
    private struct InternalData
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
        public int ProbeSpeed;

        public int ShowAtlas3DSlices;
        public Vector3 RelativeCoord;
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
        public string OpenEphysAPITarget;
        public bool SpikeGLXAPIToggle;
        public string SpikeGLXAPITarget;
        public string SpikeGLXHelloPath;
        public float APIUpdateRate;

        public bool UseIBLAngles;
        public bool AxisControl;
        public bool UseAcronyms;
        public bool UseBeryl;

        // Camera
        public float CameraZoom;
        public Vector3 CameraRotation;

        public string AtlasName;
    }
    #endregion
}