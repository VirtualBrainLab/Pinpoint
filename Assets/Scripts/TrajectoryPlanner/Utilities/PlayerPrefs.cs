using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
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
public class PlayerPrefs : MonoBehaviour
{
    // Settings
    private static bool _collisions;
    private static bool _recordingRegionOnly;
    private static bool _useAcronyms;
    private static bool _depthFromBrain;
    private static bool convertAPML2probeAxis;
    private static int slice3d;
    private static bool _inplane;
    private static int invivoTransform;
    private static bool useIBLAngles;
    private static bool showSurfaceCoord;
    private static string _ephysLinkServerIp;
    private static int _ephysLinkServerPort;
    private static bool _axisControl;
    private static bool _showAllProbePanels;
    private static string _rightHandedManipulatorIds;
    private static bool _useBeryl;
    private static bool _displayUM;
    private static Vector3 _relCoord;
    private static bool _ghostInactiveProbes;
    private static bool _ghostInactiveAreas;

    [FormerlySerializedAs("collisionsToggle")] [SerializeField] private Toggle _collisionsToggle;
    [FormerlySerializedAs("recordingRegionToggle")] [SerializeField] private Toggle _recordingRegionToggle;
    [FormerlySerializedAs("acronymToggle")] [SerializeField] private Toggle _acronymToggle;
    [FormerlySerializedAs("depthToggle")] [SerializeField] private Toggle _depthToggle;
    [FormerlySerializedAs("probeAxisToggle")] [SerializeField] private Toggle _probeAxisToggle;
    [FormerlySerializedAs("slice3dDropdown")] [SerializeField] private TMP_Dropdown _slice3dDropdown;
    [FormerlySerializedAs("inplaneToggle")] [SerializeField] private Toggle _inplaneToggle;
    [FormerlySerializedAs("invivoDropdown")] [SerializeField] private TMP_Dropdown _invivoDropdown;
    [FormerlySerializedAs("iblAngleToggle")] [SerializeField] private Toggle _iblAngleToggle;
    [FormerlySerializedAs("surfaceToggle")] [SerializeField] private Toggle _surfaceToggle;
    [FormerlySerializedAs("ephysLinkServerIpInput")] [SerializeField] private TMP_InputField _ephysLinkServerIpInput;
    [FormerlySerializedAs("ephysLinkServerPortInput")] [SerializeField] private TMP_InputField _ephysLinkServerPortInput;
    [FormerlySerializedAs("axisControlToggle")] [SerializeField] private Toggle _axisControlToggle;
    [FormerlySerializedAs("showAllProbePanelsToggle")] [SerializeField] private Toggle _showAllProbePanelsToggle;
    [FormerlySerializedAs("useBerylToggle")] [SerializeField] private Toggle _useBerylToggle;
    [FormerlySerializedAs("displayUmToggle")] [SerializeField] private Toggle _displayUmToggle;
    [FormerlySerializedAs("ghostInactiveProbesToggle")] [SerializeField] private Toggle _ghostInactiveProbesToggle;
    [FormerlySerializedAs("ghostInactiveAreasToggle")] [SerializeField] private Toggle _ghostInactiveAreasToggle;


    /// <summary>
    /// On Awake() load the preferences and toggle the corresponding UI elements
    /// </summary>
    private void Awake()
    {
        _collisions = LoadBoolPref("collisions", true);
        _collisionsToggle.isOn = _collisions;

        _recordingRegionOnly = LoadBoolPref("recording", true);
        _recordingRegionToggle.isOn = _recordingRegionOnly;

        _useAcronyms = LoadBoolPref("acronyms", true);
        _acronymToggle.isOn = _useAcronyms;

        //_depthFromBrain = LoadBoolPref("depth", true);
        //depthToggle.isOn = _depthFromBrain;

        convertAPML2probeAxis = LoadBoolPref("probeaxis", false);
        _probeAxisToggle.isOn = convertAPML2probeAxis;

        slice3d = LoadIntPref("slice3d", 0);
        _slice3dDropdown.SetValueWithoutNotify(slice3d);

        _inplane = LoadBoolPref("inplane", true);
        _inplaneToggle.isOn = _inplane;

        invivoTransform = LoadIntPref("stereotaxic", 1);
        _invivoDropdown.SetValueWithoutNotify(invivoTransform);

        useIBLAngles = LoadBoolPref("iblangle", true);
        _iblAngleToggle.isOn = useIBLAngles;

        showSurfaceCoord = LoadBoolPref("surface", true);
        _surfaceToggle.isOn = showSurfaceCoord;

        _ephysLinkServerIp = LoadStringPref("ephys_link_ip", "localhost");
        _ephysLinkServerIpInput.text = _ephysLinkServerIp;

        _ephysLinkServerPort = LoadIntPref("ephys_link_port", 8081);
        _ephysLinkServerPortInput.text = _ephysLinkServerPort.ToString();

        _axisControl = LoadBoolPref("axis_control", true);
        _axisControlToggle.isOn = _axisControl;

        _showAllProbePanels = LoadBoolPref("show_all_probe_panels", true);
        _showAllProbePanelsToggle.isOn = _showAllProbePanels;

        _rightHandedManipulatorIds = LoadStringPref("right_handed_manipulator_ids", "");

        _useBeryl = LoadBoolPref("use_beryl", true);
        _useBerylToggle.isOn = _useBeryl;

        _displayUM = LoadBoolPref("display_um", true);
        _displayUmToggle.isOn = _displayUM;

        _relCoord = LoadVector3Pref("rel_coord", new Vector3(5.4f, 5.7f, 0.332f));

        _ghostInactiveProbes = LoadBoolPref("ghost_inactive", false);
        _ghostInactiveProbesToggle.isOn = _ghostInactiveProbes;

        _ghostInactiveAreas = LoadBoolPref("ghost_areas", false);
        _ghostInactiveAreasToggle.isOn = _ghostInactiveAreas;
    }

    #region Getters/Setters

    public static void SetGhostInactiveAreas(bool ghostInactive)
    {
        _ghostInactiveAreas = ghostInactive;
        UnityEngine.PlayerPrefs.SetInt("ghost_areas", _ghostInactiveAreas ? 1 : 0);
    }

    public static bool GetGhostInactiveAreas()
    {
        return _ghostInactiveAreas;
    }

    public static void SetGhostInactiveProbes(bool ghostInactive)
    {
        _ghostInactiveProbes = ghostInactive;
        UnityEngine.PlayerPrefs.SetInt("ghost_inactive", _ghostInactiveProbes ? 1 : 0);
    }

    public static bool GetGhostInactiveProbes()
    {
        return _ghostInactiveProbes;
    }

    public static void SetRelCoord(Vector3 coord)
    {
        _relCoord = coord;
        SaveVector3Pref("rel_coord", _relCoord);
    }

    public static Vector3 GetRelCoord()
    {
        return _relCoord;
    }

    public static void SetDisplayUm(bool state)
    {
        _displayUM = state;
        UnityEngine.PlayerPrefs.SetInt("display_um", _displayUM ? 1 : 0);
    }

    public static bool GetDisplayUm()
    {
        return _displayUM;
    }

    public static void SetUseBeryl(bool state)
    {
        _useBeryl = state;
        UnityEngine.PlayerPrefs.SetInt("use_beryl", _useBeryl ? 1 : 0);
    }

    public static bool GetUseBeryl()
    {
        return _useBeryl;
    }

    public static void SetShowAllProbePanels(bool state)
    {
        _showAllProbePanels = state;
        UnityEngine.PlayerPrefs.SetInt("show_all_probe_panels", _showAllProbePanels ? 1 : 0);
    }

    public static bool GetShowAllProbePanels()
    {
        return _showAllProbePanels;
    }

    public static void SetAxisControl(bool state)
    {
        _axisControl = state;
        UnityEngine.PlayerPrefs.SetInt("axis_control", _axisControl ? 1 : 0);
    }

    public static bool GetAxisControl()
    {
        return _axisControl;
    }

    public static void SetSurfaceCoord(bool state)
    {
        showSurfaceCoord = state;
        UnityEngine.PlayerPrefs.SetInt("surface", showSurfaceCoord ? 1 : 0);
    }

    public static bool GetSurfaceCoord()
    {
        return showSurfaceCoord;
    }

    public static void SetUseIBLAngles(bool state)
    {
        useIBLAngles = state;
        UnityEngine.PlayerPrefs.SetInt("iblangle", useIBLAngles ? 1 : 0);
    }

    public static bool GetUseIBLAngles()
    {
        return useIBLAngles;
    }

    public static void SetStereotaxic(int state)
    {
        invivoTransform = state;
        UnityEngine.PlayerPrefs.SetInt("stereotaxic", invivoTransform);
    }

    public static int GetStereotaxic()
    {
        return invivoTransform;
    }

    public static void SetInplane(bool state)
    {
        _inplane = state;
        UnityEngine.PlayerPrefs.SetInt("inplane", _inplane ? 1 : 0);
    }

    public static bool GetInplane()
    {
        return _inplane;
    }

    public static void SetSlice3D(int state)
    {
        slice3d = state;
        UnityEngine.PlayerPrefs.SetInt("slice3d", slice3d);
    }

    public static int GetSlice3D()
    {
        return slice3d;
    }

    public static void SetAPML2ProbeAxis(bool state)
    {
        convertAPML2probeAxis = state;
        UnityEngine.PlayerPrefs.SetInt("probeaxis", convertAPML2probeAxis ? 1 : 0);
    }

    public static bool GetAPML2ProbeAxis()
    {
        return convertAPML2probeAxis;
    }

    public static void SetDepthFromBrain(bool state)
    {
        _depthFromBrain = state;
        UnityEngine.PlayerPrefs.SetInt("depth", _depthFromBrain ? 1 : 0);
    }

    public static bool GetDepthFromBrain()
    {
        return _depthFromBrain;
    }

    public static void SetAcronyms(bool state)
    {
        _useAcronyms = state;
        UnityEngine.PlayerPrefs.SetInt("acronyms", _recordingRegionOnly ? 1 : 0);
    }

    public static bool GetAcronyms()
    {
        return _useAcronyms;
    }

    public static void SetRecordingRegionOnly(bool state)
    {
        _recordingRegionOnly = state;
        UnityEngine.PlayerPrefs.SetInt("recording", _recordingRegionOnly ? 1 : 0);
    }

    public static bool GetRecordingRegionOnly()
    {
        return _recordingRegionOnly;
    }

    public static void SetCollisions(bool toggleCollisions)
    {
        _collisions = toggleCollisions;
        UnityEngine.PlayerPrefs.SetInt("collisions", _collisions ? 1 : 0);
    }

    public static bool GetCollisions()
    {
        return _collisions;
    }

    /// <summary>
    ///     Return the saved Ephys Link server IP address.
    /// </summary>
    /// <returns>Saved IP address of the Ephys Link server</returns>
    public static string GetServerIp()
    {
        return _ephysLinkServerIp;
    }

    /// <summary>
    ///     Return the saved Ephys Link server port.
    /// </summary>
    /// <returns>Saved server port of the Ephys Link server</returns>
    public static int GetServerPort()
    {
        return _ephysLinkServerPort;
    }

    /// <summary>
    ///     Return the saved IDs of right handed manipulators.
    /// </summary>
    /// <returns>Saved IDs of right handed manipulators</returns>
    public static HashSet<string> GetRightHandedManipulatorIds()
    {
        return _rightHandedManipulatorIds == ""
            ? new HashSet<string>()
            : _rightHandedManipulatorIds.Split(',').ToHashSet();
    }

    /// <summary>
    ///     Return if it has been more than 24 hours since the last launch.
    /// </summary>
    /// <returns>If it has been more than 24 hours since the last launch</returns>
    public static bool IsEphysLinkDataExpired()
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

    private static void SaveVector3Pref(string prefStr, Vector3 value)
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
    public static (Vector3 apmldv, Vector3 angles, int type, string manipulatorId, string coordinateSpaceName, string
        coordinateTransformName, Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth,
        string uuid)[] LoadSavedProbeData()
    {
        int probeCount = UnityEngine.PlayerPrefs.GetInt("probecount", 0);

        var savedProbes =
            new (Vector3 apmldv, Vector3 angles,
                int type, string manipulatorId,
                string coordinateSpaceName, string coordinateTransformName,
                Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth,
                string uuid)[probeCount];

        for (int i = 0; i < probeCount; i++)
        {
            float ap = UnityEngine.PlayerPrefs.GetFloat("ap" + i);
            float ml = UnityEngine.PlayerPrefs.GetFloat("ml" + i);
            float dv = UnityEngine.PlayerPrefs.GetFloat("dv" + i);
            float phi = UnityEngine.PlayerPrefs.GetFloat("phi" + i);
            float theta = UnityEngine.PlayerPrefs.GetFloat("theta" + i);
            float spin = UnityEngine.PlayerPrefs.GetFloat("spin" + i);
            int type = UnityEngine.PlayerPrefs.GetInt("type" + i);
            var manipulatorId = UnityEngine.PlayerPrefs.GetString("manipulator_id" + i);
            string coordSpaceName = UnityEngine.PlayerPrefs.GetString("coord_space" + i);
            string coordTransName = UnityEngine.PlayerPrefs.GetString("coord_trans" + i);
            var x = UnityEngine.PlayerPrefs.GetFloat("x" + i);
            var y = UnityEngine.PlayerPrefs.GetFloat("y" + i);
            var z = UnityEngine.PlayerPrefs.GetFloat("z" + i);
            var d = UnityEngine.PlayerPrefs.GetFloat("d" + i);
            var brainSurfaceOffset = UnityEngine.PlayerPrefs.GetFloat("brain_surface_offset" + i);
            var dropToSurfaceWithDepth = UnityEngine.PlayerPrefs.GetInt("drop_to_surface_with_depth" + i) == 1;
            string uuid = UnityEngine.PlayerPrefs.GetString("uuid" + i);

            savedProbes[i] = (new Vector3(ap, ml, dv), new Vector3(phi, theta, spin),
                type, manipulatorId,
                coordSpaceName, coordTransName,
                new Vector4(x, y, z, d), brainSurfaceOffset, dropToSurfaceWithDepth,
                uuid);
        }

        return savedProbes;
    }

    /// <summary>
    /// Save the data about all of the probes passed in through allProbeData
    /// </summary>
    /// <param name="allProbeData">tip position, angles, and type for probes</param>
    public void SaveCurrentProbeData(
        (Vector3 apmldv, Vector3 angles, int type, string manipulatorId, string coordinateSpace, string
            coordinateTransform, Vector4 zeroCoordinateOffset, float brainSurfaceOffset, bool dropToSurfaceWithDepth,
            string uuid)[] allProbeData)
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
            UnityEngine.PlayerPrefs.SetString("manipulator_id" + i, currentProbeData.manipulatorId);
            UnityEngine.PlayerPrefs.SetString("coord_space" + i, currentProbeData.coordinateSpace);
            UnityEngine.PlayerPrefs.SetString("coord_trans" + i, currentProbeData.coordinateTransform);
            UnityEngine.PlayerPrefs.SetFloat("x" + i, currentProbeData.zeroCoordinateOffset.x);
            UnityEngine.PlayerPrefs.SetFloat("y" + i, currentProbeData.zeroCoordinateOffset.y);
            UnityEngine.PlayerPrefs.SetFloat("z" + i, currentProbeData.zeroCoordinateOffset.z);
            UnityEngine.PlayerPrefs.SetFloat("d" + i, currentProbeData.zeroCoordinateOffset.w);
            UnityEngine.PlayerPrefs.SetFloat("brain_surface_offset" + i, allProbeData[i].brainSurfaceOffset);
            UnityEngine.PlayerPrefs.SetInt("drop_to_surface_with_depth" + i,
                allProbeData[i].dropToSurfaceWithDepth ? 1 : 0);
            UnityEngine.PlayerPrefs.SetString("uuid" + i, allProbeData[i].uuid);
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
    public static void SaveRightHandedManipulatorIds(IEnumerable<string> manipulatorIds)
    {
        UnityEngine.PlayerPrefs.SetString("right_handed_manipulator_ids", string.Join(",", manipulatorIds));
        UnityEngine.PlayerPrefs.Save();
    }

    #endregion
}