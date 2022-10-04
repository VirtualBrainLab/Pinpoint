using System;
using System.Collections.Generic;
using System.Linq;
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
    private bool _collisions;
    private bool _recordingRegionOnly;
    private bool _useAcronyms;
    private bool _depthFromBrain;
    private bool convertAPML2probeAxis;
    private int slice3d;
    private bool _inplane;
    private int invivoTransform;
    private bool useIBLAngles;
    private bool showSurfaceCoord;
    private string _ephysLinkServerIp;
    private int _ephysLinkServerPort;
    private bool _axisControl;
    private bool _showAllProbePanels;
    private string _rightHandedManipulatorIds;
    private bool _useBeryl;
    private bool _displayUM;
    private Vector3 _relCoord;

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
    [SerializeField] TMP_InputField ephysLinkServerIpInput;
    [SerializeField] TMP_InputField ephysLinkServerPortInput;
    [SerializeField] Toggle axisControlToggle;
    [SerializeField] Toggle showAllProbePanelsToggle;
    [SerializeField] Toggle useBerylToggle;
    [SerializeField] Toggle displayUmToggle;


    /// <summary>
    /// On Awake() load the preferences and toggle the corresponding UI elements
    /// </summary>
    void Awake()
    {
        _collisions = LoadBoolPref("collisions", true);
        collisionsToggle.isOn = _collisions;

        _recordingRegionOnly = LoadBoolPref("recording", true);
        recordingRegionToggle.isOn = _recordingRegionOnly;

        _useAcronyms = LoadBoolPref("acronyms", true);
        acronymToggle.isOn = _useAcronyms;

        _depthFromBrain = LoadBoolPref("depth", true);
        depthToggle.isOn = _depthFromBrain;

        convertAPML2probeAxis = LoadBoolPref("probeaxis", false);
        probeAxisToggle.isOn = convertAPML2probeAxis;

        slice3d = LoadIntPref("slice3d", 0);
        slice3dDropdown.SetValueWithoutNotify(slice3d);

        _inplane = LoadBoolPref("inplane", true);
        inplaneToggle.isOn = _inplane;

        invivoTransform = LoadIntPref("stereotaxic", 1);
        invivoDropdown.SetValueWithoutNotify(invivoTransform);

        useIBLAngles = LoadBoolPref("iblangle", true);
        iblAngleToggle.isOn = useIBLAngles;

        showSurfaceCoord = LoadBoolPref("surface", true);
        surfaceToggle.isOn = showSurfaceCoord;

        _ephysLinkServerIp = LoadStringPref("ephys_link_ip", "localhost");
        ephysLinkServerIpInput.text = _ephysLinkServerIp;

        _ephysLinkServerPort = LoadIntPref("ephys_link_port", 8080);
        ephysLinkServerPortInput.text = _ephysLinkServerPort.ToString();

        _axisControl = LoadBoolPref("axis_control", true);
        axisControlToggle.isOn = _axisControl;

        _showAllProbePanels = LoadBoolPref("show_all_probe_panels", true);
        showAllProbePanelsToggle.isOn = _showAllProbePanels;

        _rightHandedManipulatorIds = LoadStringPref("right_handed_manipulator_ids", "");

        _useBeryl = LoadBoolPref("use_beryl", true);
        useBerylToggle.isOn = _useBeryl;

        _displayUM = LoadBoolPref("display_um", true);
        displayUmToggle.isOn = _displayUM;

        _relCoord = LoadVector3Pref("rel_coord", new Vector3(5.4f, 5.7f, 0.332f));
    }

    #region Getters/Setters

    public void SetRelCoord(Vector3 coord)
    {
        _relCoord = coord;
        SaveVector3Pref("rel_coord", _relCoord);
    }

    public Vector3 GetRelCoord()
    {
        return _relCoord;
    }

    public void SetDisplayUm(bool state)
    {
        _displayUM = state;
        UnityEngine.PlayerPrefs.SetInt("display_um", _displayUM ? 1 : 0);
    }

    public bool GetDisplayUm()
    {
        return _displayUM;
    }

    public void SetUseBeryl(bool state)
    {
        _useBeryl = state;
        UnityEngine.PlayerPrefs.SetInt("use_beryl", _useBeryl ? 1 : 0);
    }

    public bool GetUseBeryl()
    {
        return _useBeryl;
    }

    public void SetShowAllProbePanels(bool state)
    {
        _showAllProbePanels = state;
        UnityEngine.PlayerPrefs.SetInt("show_all_probe_panels", _showAllProbePanels ? 1 : 0);
    }

    public bool GetShowAllProbePanels()
    {
        return _showAllProbePanels;
    }

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
        _inplane = state;
        UnityEngine.PlayerPrefs.SetInt("inplane", _inplane ? 1 : 0);
    }

    public bool GetInplane()
    {
        return _inplane;
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
        _depthFromBrain = state;
        UnityEngine.PlayerPrefs.SetInt("depth", _depthFromBrain ? 1 : 0);
    }

    public bool GetDepthFromBrain()
    {
        return _depthFromBrain;
    }

    public void SetAcronyms(bool state)
    {
        _useAcronyms = state;
        UnityEngine.PlayerPrefs.SetInt("acronyms", _recordingRegionOnly ? 1 : 0);
    }

    public bool GetAcronyms()
    {
        return _useAcronyms;
    }

    public void SetRecordingRegionOnly(bool state)
    {
        _recordingRegionOnly = state;
        UnityEngine.PlayerPrefs.SetInt("recording", _recordingRegionOnly ? 1 : 0);
    }

    public bool GetRecordingRegionOnly()
    {
        return _recordingRegionOnly;
    }

    public void SetCollisions(bool toggleCollisions)
    {
        _collisions = toggleCollisions;
        UnityEngine.PlayerPrefs.SetInt("collisions", _collisions ? 1 : 0);
    }

    public bool GetCollisions()
    {
        return _collisions;
    }

    /// <summary>
    ///     Return the saved Ephys Link server IP address.
    /// </summary>
    /// <returns>Saved IP address of the Ephys Link server</returns>
    public string GetServerIp()
    {
        return _ephysLinkServerIp;
    }

    /// <summary>
    ///     Return the saved Ephys Link server port.
    /// </summary>
    /// <returns>Saved server port of the Ephys Link server</returns>
    public int GetServerPort()
    {
        return _ephysLinkServerPort;
    }

    /// <summary>
    ///     Return the saved IDs of right handed manipulators.
    /// </summary>
    /// <returns>Saved IDs of right handed manipulators</returns>
    public HashSet<int> GetRightHandedManipulatorIds()
    {
        return _rightHandedManipulatorIds == "" ? new HashSet<int>() : Array.ConvertAll(_rightHandedManipulatorIds.Split(','), int.Parse).ToHashSet();
    }

    /// <summary>
    ///     Return if it has been more than 24 hours since the last launch.
    /// </summary>
    /// <returns>If it has been more than 24 hours since the last launch</returns>
    public static bool IsLinkDataExpired()
    {
        var timestampString = UnityEngine.PlayerPrefs.GetString("timestamp");
        if (timestampString == "") return false;

        return new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds() - long.Parse(timestampString) >= 86400;
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
        return UnityEngine.PlayerPrefs.HasKey(prefStr) ? UnityEngine.PlayerPrefs.GetInt(prefStr) == 1 : defaultValue;
    }

    /// <summary>
    /// Load an integer preference
    /// </summary>
    /// <param name="prefStr">string accessor</param>
    /// <param name="defaultValue">default value if the preference is not set</param>
    /// <returns></returns>
    private int LoadIntPref(string prefStr, int defaultValue)
    {
        return UnityEngine.PlayerPrefs.HasKey(prefStr) ? UnityEngine.PlayerPrefs.GetInt(prefStr) : defaultValue;
    }

    /// <summary>
    /// Load a string preference
    /// </summary>
    /// <param name="prefStr">string accessor</param>
    /// <param name="defaultValue">default value if the preference is not set</param>
    /// <returns></returns>
    private string LoadStringPref(string prefStr, string defaultValue)
    {
        return UnityEngine.PlayerPrefs.HasKey(prefStr) ? UnityEngine.PlayerPrefs.GetString(prefStr) : defaultValue;
    }

    private Vector3 LoadVector3Pref(string prefStr, Vector3 defaultValue)
    {
        if (UnityEngine.PlayerPrefs.HasKey(prefStr + "_x"))
        {
            float ap = UnityEngine.PlayerPrefs.GetFloat(prefStr + "_x");
            float ml = UnityEngine.PlayerPrefs.GetFloat(prefStr + "_y");
            float dv = UnityEngine.PlayerPrefs.GetFloat(prefStr + "_z");

            return new Vector3(ap, ml, dv);
        }
        else
            return defaultValue;
    }

    private void SaveVector3Pref(string prefStr, Vector3 value)
    {
        UnityEngine.PlayerPrefs.SetFloat(prefStr + "_x", value.x);
        UnityEngine.PlayerPrefs.SetFloat(prefStr + "_y", value.y);
        UnityEngine.PlayerPrefs.SetFloat(prefStr + "_z", value.z);
    }

    #endregion

    #region Probe saving/loading
    /// <summary>
    /// Return an array with information about the positions of probes that were saved from the last session
    /// </summary>
    /// <returns></returns>
    public (Vector3 apmldv, Vector3 angles,
                int type, int manipulatorId,
                string coordinateSpaceName, string coordinateTransformName,
                Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth)[] LoadSavedProbes()
    {
        int probeCount = UnityEngine.PlayerPrefs.GetInt("probecount", 0);

        var savedProbes =
            new (Vector3 apmldv, Vector3 angles,
                int type, int manipulatorId,
                string coordinateSpaceName, string coordinateTransformName,
                Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth)[probeCount];

        for (int i = 0; i < probeCount; i++)
        {
            float ap = UnityEngine.PlayerPrefs.GetFloat("ap" + i);
            float ml = UnityEngine.PlayerPrefs.GetFloat("ml" + i);
            float dv = UnityEngine.PlayerPrefs.GetFloat("dv" + i);
            float phi = UnityEngine.PlayerPrefs.GetFloat("phi" + i);
            float theta = UnityEngine.PlayerPrefs.GetFloat("theta" + i);
            float spin = UnityEngine.PlayerPrefs.GetFloat("spin" + i);
            int type = UnityEngine.PlayerPrefs.GetInt("type" + i);
            var manipulatorId = UnityEngine.PlayerPrefs.GetInt("manipulator_id" + i);
            string coordSpaceName = UnityEngine.PlayerPrefs.GetString("coord_space" + i);
            string coordTransName = UnityEngine.PlayerPrefs.GetString("coord_trans" + i);
            var x = UnityEngine.PlayerPrefs.GetFloat("x" + i);
            var y = UnityEngine.PlayerPrefs.GetFloat("y" + i);
            var z = UnityEngine.PlayerPrefs.GetFloat("z" + i);
            var d = UnityEngine.PlayerPrefs.GetFloat("d" + i);
            var brainSurfaceOffset = UnityEngine.PlayerPrefs.GetFloat("brain_surface_offset" + i);
            var dropToSurfaceWithDepth = UnityEngine.PlayerPrefs.GetInt("drop_to_surface_with_depth" + i) == 1;

            savedProbes[i] = (new Vector3(ap, ml, dv), new Vector3(phi, theta, spin),
                type, manipulatorId,
                coordSpaceName, coordTransName,
                new Vector4(x, y, z, d), brainSurfaceOffset, dropToSurfaceWithDepth);
        }

        return savedProbes;
    }

    /// <summary>
    /// Save the data about all of the probes passed in through allProbeData
    /// </summary>
    /// <param name="allProbeData">tip position, angles, and type for probes</param>
    public void SaveCurrentProbeData(
        (Vector3 apmldv, Vector3 angles,
                int type, int manipulatorId,
                string coordinateSpace, string coordinateTransform,
                Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth)[] allProbeData)
    {
        for (int i = 0; i < allProbeData.Length; i++)
        {
            var currentProbeData = allProbeData[i];

            UnityEngine.PlayerPrefs.SetFloat("ap" + i, currentProbeData.apmldv.x);
            UnityEngine.PlayerPrefs.SetFloat("ml" + i, currentProbeData.apmldv.y);
            UnityEngine.PlayerPrefs.SetFloat("dv" + i, currentProbeData.apmldv.z);
            UnityEngine.PlayerPrefs.SetFloat("phi" + i, currentProbeData.angles.x);
            UnityEngine.PlayerPrefs.SetFloat("theta" + i, currentProbeData.angles.y);
            UnityEngine.PlayerPrefs.SetFloat("spin" + i, currentProbeData.angles.z);
            UnityEngine.PlayerPrefs.SetInt("type" + i, currentProbeData.type);
            UnityEngine.PlayerPrefs.SetInt("manipulator_id" + i, currentProbeData.manipulatorId);
            UnityEngine.PlayerPrefs.SetString("coord_space" + i, currentProbeData.coordinateSpace);
            UnityEngine.PlayerPrefs.SetString("coord_trans" + i, currentProbeData.coordinateTransform);
            UnityEngine.PlayerPrefs.SetFloat("x" + i, currentProbeData.zeroCoordinateOffset.x);
            UnityEngine.PlayerPrefs.SetFloat("y" + i, currentProbeData.zeroCoordinateOffset.y);
            UnityEngine.PlayerPrefs.SetFloat("z" + i, currentProbeData.zeroCoordinateOffset.z);
            UnityEngine.PlayerPrefs.SetFloat("d" + i, currentProbeData.zeroCoordinateOffset.w);
            UnityEngine.PlayerPrefs.SetFloat("brain_surface_offset" + i, allProbeData[i].brainSurfaceOffset);
            UnityEngine.PlayerPrefs.SetInt("drop_to_surface_with_depth" + i,
                allProbeData[i].dropToSurfaceWithDepth ? 1 : 0);
        }
        UnityEngine.PlayerPrefs.SetInt("probecount", allProbeData.Length);
        UnityEngine.PlayerPrefs.SetString("timestamp",
            new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString("D16"));

        UnityEngine.PlayerPrefs.Save();
    }

    #endregion

    #region Ephys link

    /// <summary>
    ///     Save Ephys Link server connection information.
    /// </summary>
    /// <param name="serverIp">Server IP address</param>
    /// <param name="serverPort">Server port number</param>
    public static void SaveEphysLinkConnectionData(string serverIp, int serverPort)
    {
        UnityEngine.PlayerPrefs.SetString("ephys_link_ip", serverIp);
        UnityEngine.PlayerPrefs.SetInt("ephys_link_port", serverPort);
        UnityEngine.PlayerPrefs.Save();
    }

    /// <summary>
    ///     Save the IDs of right handed manipulators.
    /// </summary>
    /// <param name="manipulatorIds">IDs of right handed manipulators</param>
    public static void SaveRightHandedManipulatorIds(IEnumerable<int> manipulatorIds)
    {
        UnityEngine.PlayerPrefs.SetString("right_handed_manipulator_ids", string.Join(",", manipulatorIds));
        UnityEngine.PlayerPrefs.Save();
    }

    #endregion
}
