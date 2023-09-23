using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EphysLink;
using TrajectoryPlanner.Probes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// 3D space control for Neuropixels probes in the Trajectory Planner scene
/// </summary>
public class ProbeManager : MonoBehaviour
{
    #region Webgl only
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void Copy2Clipboard(string str);
#endif
    #endregion

    #region Static fields
    public static readonly List<ProbeManager> Instances = new();
    public static ProbeManager ActiveProbeManager;

    // Static events
    public static readonly UnityEvent<HashSet<ProbeManager>> EphysLinkControlledProbesChangedEvent = new();
    public static readonly UnityEvent ActiveProbeUIUpdateEvent = new();
    #endregion

    #region Events

    public UnityEvent UIUpdateEvent;
    public UnityEvent ActivateProbeEvent;
    public UnityEvent EphysLinkControlChangeEvent;

    #endregion


    #region Identifiers
    public string UUID { get; private set; }
    private string _overrideName;
    public string OverrideName
    {
        get => _overrideName;
        set
        {
            _overrideName = value;
            UpdateName();
            UIUpdateEvent.Invoke();
        }
    }

    #endregion

    // Exposed fields to collect links to other components inside of the Probe prefab
    [FormerlySerializedAs("probeColliders")][SerializeField] private List<Collider> _probeColliders;
    [FormerlySerializedAs("probeUIManagers")][SerializeField] private List<ProbeUIManager> _probeUIManagers;
    [FormerlySerializedAs("probeRenderer")][SerializeField] private Renderer _probeRenderer;
    [SerializeField] private RecordingRegion _recRegion;

    private AxisControl _axisControl;
    public ProbeProperties.ProbeType ProbeType;

    [FormerlySerializedAs("probeController")][SerializeField] private ProbeController _probeController;

    [SerializeField] private Material _lineMaterial;
    [FormerlySerializedAs("ghostMaterial")][SerializeField] private Material _ghostMaterial;

    private Dictionary<GameObject, Material> _defaultMaterials;
    private HashSet<Renderer> _activeRenderers;

    #region Channel map
    public string SelectionLayerName { get; private set; }
    private float _channelMinY;
    private float _channelMaxY;
    /// <summary>
    /// Return the minimum and maximum channel position in the current selection in mm
    /// </summary>
    public (float, float) GetChannelMinMaxYCoord { get { return (_channelMinY, _channelMaxY); } }
    public ChannelMap ChannelMap { get; private set; }


    /// <summary>
    /// Get the channel map information from all active ProbeManager instances
    /// </summary>
    /// <returns>Array of "name:data" strings, including quotes</returns>
    public static string[] GetAllChannelAnnotationData()
    {
        return Instances.Select(x => $"\"{x.name}:{x.GetChannelAnnotationIDs(false)}\"").ToArray();
    }
    #endregion

    // Probe position data
    private Vector3 _recRegionBaseCoordU;
    private Vector3 _recRegionTopCoordU;

    public (Vector3 tipCoordU, Vector3 endCoordU) RecRegionCoordWorldU { get { return (_recRegionBaseCoordU, _recRegionTopCoordU); } }

    // Text
    private const float minYaw = -180;
    private const float maxYaw = 180f;
    private const float minRoll = -180f;
    private const float maxRoll = 180f;

    // Brain surface position
    private CCFAnnotationDataset annotationDataset;
    private bool probeInBrain;
    private Vector3 _brainSurface;
    private Vector3 brainSurfaceWorld;
    private Vector3 brainSurfaceWorldT;

    #region Accessors

    private ProbeDisplayType _probeDisplayType;
    public ProbeDisplayType ProbeDisplay
    {
        get
        {
            return _probeDisplayType;
        }
        set
        {
            _probeDisplayType = value;
            SetMaterials();
        }
    }

    public ProbeController ProbeController { get => _probeController;
        private set => _probeController = value;
    }

    public bool Locked
    {
        get
        {
            return _probeController.Locked;
        }
    }

    public ManipulatorBehaviorController ManipulatorBehaviorController =>
        gameObject.GetComponent<ManipulatorBehaviorController>();

    public bool IsEphysLinkControlled
    {
        get => ManipulatorBehaviorController && ManipulatorBehaviorController.enabled;
        private set
        {
            ManipulatorBehaviorController.enabled = value;
            EphysLinkControlChangeEvent.Invoke();
            EphysLinkControlledProbesChangedEvent.Invoke(Instances.Where(manager => manager.IsEphysLinkControlled).ToHashSet());
        }
    }

    public string APITarget { get; set; }

    private Color _color;
    public Color Color
    {
        get => _color;

        set
        {
            // try to return the current color
            ProbeProperties.ReturnColor(_color);

            _color = value;
            _probeRenderer.material.color = _color;

            foreach (ProbeUIManager puiManager in _probeUIManagers)
                puiManager.UpdateColors();

            UIUpdateEvent.Invoke();
        }
    }

    public void DisableAllColliders()
    {
        foreach (var probeCollider in _probeColliders)
        {
            probeCollider.enabled = false;
        }
    }

    /// <summary>
    /// Return the probe panel UI managers
    /// </summary>
    /// <returns>list of probe panel UI managers</returns>
    public List<ProbeUIManager> GetProbeUIManagers()
    {
        return _probeUIManagers;
    }

    /// <summary>
    /// When true, this Probe will be saved and re-loaded in the scene the next time Pinpoint loads
    /// </summary>
    public bool Saved { get; set; }

    #endregion

    #region Unity

    private void Awake()
    {
        Saved = true;

        UUID = Guid.NewGuid().ToString();
        UpdateName();

        // Record default materials
        _defaultMaterials = new Dictionary<GameObject, Material>();
        _activeRenderers = new();

        foreach (var childRenderer in transform.GetComponentsInChildren<Renderer>())
        {
            _defaultMaterials.Add(childRenderer.gameObject, childRenderer.material);

            // If this renderer is NOT attached to a collider gameobject, hold it as active
            if (!_probeColliders.Any(x => x.gameObject.Equals(childRenderer.gameObject)))
                _activeRenderers.Add(childRenderer);
        }

        // Pull the tpmanager object and register this probe
        _probeController.Register(this);

        // Get the channel map and selection layer
        ChannelMap = ChannelMapManager.GetChannelMap(ProbeType);
        SelectionLayerName = "default";

        // Get access to the annotation dataset and world-space boundaries
        annotationDataset = VolumeDatasetManager.AnnotationDataset;

        _axisControl = GameObject.Find("AxisControl").GetComponent<AxisControl>();

        // Set color
        if (_probeRenderer != null)
            _color = ProbeProperties.NextColor;

        _probeController.FinishedMovingEvent.AddListener(UpdateName);
        _probeController.MovedThisFrameEvent.AddListener(ProbeMoved);
    }

    private void Start()
    {
#if UNITY_EDITOR
        Debug.Log($"(ProbeManager) New probe created with UUID: {UUID}");
#endif
        UpdateSelectionLayer(SelectionLayerName);

        // Force update color
        foreach (ProbeUIManager puiManager in _probeUIManagers)
            puiManager.UpdateColors();
        if (_probeRenderer) _probeRenderer.material.color = _color;

        UIUpdateEvent.Invoke();
    }

    /// <summary>
    /// Called by Unity when this object is destroyed. 
    /// Unregisters the probe from tpmanager
    /// Removes the probe panels and the position text.
    /// </summary>
    public void Destroy()
    {
        ProbeProperties.ReturnColor(Color);

        ColliderManager.RemoveProbeColliderInstances(_probeColliders);
        
        // Force disable Ephys Link
        if (IsEphysLinkControlled)
        {
            IsEphysLinkControlled = false;
            CommunicationManager.Instance.UnregisterManipulator(ManipulatorBehaviorController.ManipulatorID);
        }
        
        // Delete this gameObject
        foreach (ProbeUIManager puimanager in _probeUIManagers)
            puimanager.Destroy();
    }

    private void OnDestroy()
    {
        // Destroy instance
        Debug.Log($"Destroying probe: {name}");

        if (ProbeInsertion.Instances.Count == 1)
            ProbeInsertion.Instances.Clear();
        else
            ProbeInsertion.Instances.Remove(ProbeController.Insertion);

        if (Instances.Count == 1)
            Instances.Clear();
        else
            Instances.Remove(this);
    }

    private void OnEnable() => Instances.Add(this);
    

    #endregion

    /// <summary>
    /// Called by the TPManager when this Probe becomes the active probe
    /// </summary>
    public void SetActive(bool active)
    {
#if UNITY_EDITOR
        Debug.Log($"{name} becoming {(active ? "active" : "inactive")}");
#endif

        ColliderManager.AddProbeColliderInstances(_probeColliders, active);
        GetComponent<ProbeController>().enabled = active;

        UIUpdateEvent.Invoke();
        _probeController.MovedThisFrameEvent.Invoke();
    }

    /// <summary>
    /// Called by ProbeColliders when this probe is clicked on
    /// </summary>
    public void MouseDown()
    {
        ActivateProbeEvent.Invoke();
    }

    public void Update2ActiveTransform()
    {
        _probeController.SetSpaceTransform(CoordinateSpaceManager.ActiveCoordinateSpace, CoordinateSpaceManager.ActiveCoordinateTransform);
    }

    public void SetUIVisibility(bool state)
    {
        foreach (ProbeUIManager puimanager in _probeUIManagers)
            puimanager.SetProbePanelVisibility(state);
    }


    public void OverrideUUID(string newUUID)
    {
        UUID = newUUID;
        UpdateName();
        UIUpdateEvent.Invoke();
    }

    /// <summary>
    /// Update the name of this probe, when it is in the brain 
    /// </summary>
    public void UpdateName()
    {
        if (OverrideName != null)
        {
            name = OverrideName;
        }
        else
        {
            // Check if this probe is in the brain
            name = probeInBrain ? $"{_probeUIManagers[0].MaxArea}-{UUID[..8]}" : UUID[..8];
        }
    }

    public void ProbeMoved()
    {
        ProbeInsertion insertion = _probeController.Insertion;
        var channelCoords = GetChannelRangemm();
        
        // Update the world coordinates for the tip position
        Vector3 startCoordWorldT = _probeController.ProbeTipT.position + _probeController.ProbeTipT.up * channelCoords.startPosmm;
        Vector3 endCoordWorldT = _probeController.ProbeTipT.position + _probeController.ProbeTipT.up * channelCoords.endPosmm;
        _recRegionBaseCoordU = insertion.CoordinateSpace.Space2World(insertion.CoordinateTransform.Transform2Space(insertion.CoordinateTransform.Space2TransformAxisChange(insertion.CoordinateSpace.World2Space(startCoordWorldT))));
        _recRegionTopCoordU = insertion.CoordinateSpace.Space2World(insertion.CoordinateTransform.Transform2Space(insertion.CoordinateTransform.Space2TransformAxisChange(insertion.CoordinateSpace.World2Space(endCoordWorldT))));
    }

    #region Channel map
    public (float startPosmm, float endPosmm, float recordingSizemm, float fullHeight) GetChannelRangemm()
    {
        (float startPosmm, float endPosmm) = GetChannelMinMaxYCoord;
        float recordingSizemm = endPosmm - startPosmm;

        return (startPosmm, endPosmm, recordingSizemm, ChannelMap.FullHeight);
    }

    public void UpdateSelectionLayer(string selectionLayerName)
    {
#if UNITY_EDITOR
        Debug.Log($"Updating selection layer to {selectionLayerName}");
#endif
        SelectionLayerName = selectionLayerName;

        UpdateChannelMap();

        UIUpdateEvent.Invoke();
        
    }

    /// <summary>
    /// Update the channel map data according to the selected channels
    /// Defaults to the first 384 channels
    /// 
    /// Sets channelMinY/channelMaxY in mm
    /// </summary>
    public void UpdateChannelMap()
    {

        _channelMinY = float.MaxValue;
        _channelMaxY = float.MinValue;

        var channelCoords = ChannelMap.GetChannelPositions(SelectionLayerName);
        Vector3 channelScale = ChannelMap.GetChannelScale();

        for (int i = 0; i < channelCoords.Count; i++)
        {
            if (channelCoords[i].y < _channelMinY)
                _channelMinY = channelCoords[i].y / 1000f; // coordinates are in um, so divide to mm
            if (channelCoords[i].y > _channelMaxY)
                _channelMaxY = channelCoords[i].y / 1000f + channelScale.y / 1000f;
        }

        if (_channelMaxY == _channelMinY)
        {
            // if the channel min/max are identical, default to the height of the channel map
            _channelMaxY = ChannelMap.FullHeight;
        }

#if UNITY_EDITOR
        Debug.Log($"(ProbeManager) Minimum channel coordinate {_channelMinY} max {_channelMaxY}");
#endif
        foreach (ProbeUIManager puiManager in _probeUIManagers)
            puiManager.UpdateChannelMap();

        if (_recRegion != null)
            _recRegion.SetSize(_channelMinY, _channelMaxY);

        // Update the in-plane slice
        ProbeController.MovedThisFrameEvent.Invoke();
    }

    /// <summary>
    /// Get a serialized representation of the depth information on each shank of this probe
    /// 
    /// SpikeGLX format
    /// [probe,shank]()
    /// </summary>
    /// <returns>List of strings, each of which has data for one shank in the scene</returns>
    public List<string> GetProbeDepthIDs()
    {
        List<string> depthIDs = new List<string>();
        if (ProbeProperties.FourShank(ProbeType))
        {
            // do something else

            for (int si = 0; si < 4; si++)
                depthIDs.Add(perShankDepthIDs(si));
        }
        else
        {
            depthIDs.Add(perShankDepthIDs(0));
        }
        return depthIDs;
    }

    private string perShankDepthIDs(int shank)
    {
        // Create a list of range, acronym color
        List<(int bot, int top, string acronym, Color color)> probeAnnotationData = new();


        ProbeUIManager uiManager = _probeUIManagers[shank];
        //Vector3 baseCoordWorldT = uiManager.ShankTipT().position + _probeController.ProbeTipT.up * _channelMinY;
        //Vector3 topCoordWorldT = uiManager.ShankTipT().position + _probeController.ProbeTipT.up * _channelMaxY;

        Vector3 baseCoordWorldT = uiManager.ShankTipT().position;
        Vector3 topCoordWorldT = uiManager.ShankTipT().position + _probeController.ProbeTipT.up * ChannelMap.FullHeight;
        //float height = _channelMaxY - _channelMinY;
        float height = ChannelMap.FullHeight;

        // convert to worldU
        ProbeInsertion insertion = _probeController.Insertion;
        Vector3 baseCoordWorldU = insertion.CoordinateSpace.Space2World(insertion.CoordinateTransform.Transform2Space(insertion.CoordinateTransform.Space2TransformAxisChange(insertion.CoordinateSpace.World2Space(baseCoordWorldT))));
        Vector3 topCoordWorldU = insertion.CoordinateSpace.Space2World(insertion.CoordinateTransform.Transform2Space(insertion.CoordinateTransform.Space2TransformAxisChange(insertion.CoordinateSpace.World2Space(topCoordWorldT))));

        // Lerp between the base and top coordinate in small steps'

        int lastID = annotationDataset.ValueAtIndex(annotationDataset.CoordinateSpace.World2Space(baseCoordWorldU));
        if (lastID < 0) lastID = -1;
        
        float curBottom = 0f;
        float _channelMinUM = 0f; // _channelMinY * 1000f;

        for (float perc = 0f; perc < 1f; perc += 0.01f)
        {
            Vector3 coordU = Vector3.Lerp(baseCoordWorldU, topCoordWorldU, perc);

            int ID = annotationDataset.ValueAtIndex(annotationDataset.CoordinateSpace.World2Space(coordU));
            if (ID < 0) ID = -1;

            if (ID != lastID)
            {
                // Save the current step
                float newHeight = perc * height * 1000f;
                probeAnnotationData.Add((Mathf.RoundToInt(curBottom + _channelMinUM), Mathf.RoundToInt(newHeight + _channelMinUM), CCFModelControl.ID2Acronym(ID), CCFModelControl.GetCCFAreaColor(ID)));
                curBottom = newHeight;
                lastID = ID;
            }
        }

        // Save the final step
        probeAnnotationData.Add((Mathf.RoundToInt(curBottom + _channelMinUM), Mathf.RoundToInt(height * 1000 + _channelMinUM), CCFModelControl.ID2Acronym(lastID), CCFModelControl.GetCCFAreaColor(lastID)));

        // Flatten the list data according to the SpikeGLX format
        // [probe, shank](startpos, endpos, r, g, b, name)
        // [0,0](0,1000,200,0,0,cortex)

        string probeStr = $"[{APITarget},{shank}]";

        foreach (var data in probeAnnotationData)
        {
            probeStr += $"({data.bot},{data.top}," +
                $"{Mathf.RoundToInt(data.color.r * 255)},{Mathf.RoundToInt(data.color.g * 255)},{Mathf.RoundToInt(data.color.b * 255)}," +
                $"{data.acronym})";
        }

        return probeStr;
    }

    /// <summary>
    /// Get a serialized representation of the channel ID data
    /// </summary>
    /// <returns></returns>
    public string GetChannelAnnotationIDs(bool collapsed = true)
    {
        // Get the channel data
        var channelMapData = ChannelMap.GetChannelPositions("all");

        List<(int idx, int ID, string acronym, string color)> channelAnnotationData = new();

        if (ProbeProperties.FourShank(ProbeType))
        {
            //channelStrings = new string[channelMapData.Count * 4];

            for (int si = 0; si < 4; si++)
            {
                ProbeUIManager uiManager = _probeUIManagers[si];
                Vector3 shankTipCoordWorldT = uiManager.ShankTipT().position;

                for (int i = 0; i < channelMapData.Count; i++)
                {
                    // For now we'll ignore x changes and just use the y coordinate, this way we don't need to calculate the forward vector for the probe
                    // note that we're ignoring depth here, this assume the probe tip is on the electrode surface (which it should be)
                    Vector3 channelCoordWorldT = shankTipCoordWorldT + _probeController.ProbeTipT.up * channelMapData[i].y / 1000f;

                    // Now transform this into WorldU
                    ProbeInsertion insertion = _probeController.Insertion;
                    Vector3 channelCoordWorldU = insertion.CoordinateSpace.Space2World(insertion.CoordinateTransform.Transform2Space(insertion.CoordinateTransform.Space2TransformAxisChange(insertion.CoordinateSpace.World2Space(channelCoordWorldT))));

                    int elecIdx = si * channelMapData.Count + i;
                    int ID = annotationDataset.ValueAtIndex(annotationDataset.CoordinateSpace.World2Space(channelCoordWorldU));
                    if (ID < 0) ID = -1;

                    string acronym = CCFModelControl.ID2Acronym(ID);
                    Color color = CCFModelControl.GetCCFAreaColor(ID);

                    channelAnnotationData.Add((elecIdx, ID, acronym, TP_Utils.Color2Hex(color)));
                }
            }
        }
        else
        {

            // Populate the data string
            Vector3 tipCoordWorldT = _probeController.ProbeTipT.position;

            for (int i = 0; i < channelMapData.Count; i++)
            {
                // For now we'll ignore x changes and just use the y coordinate, this way we don't need to calculate the forward vector for the probe
                // note that we're ignoring depth here, this assume the probe tip is on the electrode surface (which it should be)
                Vector3 channelCoordWorldT = tipCoordWorldT + _probeController.ProbeTipT.up * channelMapData[i].y / 1000f;

                // Now transform this into WorldU
                ProbeInsertion insertion = _probeController.Insertion;
                Vector3 channelCoordWorldU = insertion.CoordinateSpace.Space2World(insertion.CoordinateTransform.Transform2Space(insertion.CoordinateTransform.Space2TransformAxisChange(insertion.CoordinateSpace.World2Space(channelCoordWorldT))));

                int ID = annotationDataset.ValueAtIndex(annotationDataset.CoordinateSpace.World2Space(channelCoordWorldU));
                if (ID < 0) ID = -1;

                string acronym = CCFModelControl.ID2Acronym(ID);
                Color color = CCFModelControl.GetCCFAreaColor(ID);

                channelAnnotationData.Add((i, ID, acronym, TP_Utils.Color2Hex(color)));
            }
        }

        string[] channelStrings;

        // If the data need to be collapsed, combine all channel IDs that are the same
        if (collapsed)
        {
            List<(string idxRange, string acronym, string color)> collapsedAnnotationData = new();

            int firstIdx = 0;
            int curID = channelAnnotationData[0].ID;
            string curAcronym = channelAnnotationData[0].acronym;
            string curColor = channelAnnotationData[0].color;

            for (int curIdx = 1; curIdx < channelAnnotationData.Count; curIdx++)
            {
                var data = channelAnnotationData[curIdx];
                if (data.ID != curID)
                {
                    // save the previous indexes as a range, then start a new one
                    collapsedAnnotationData.Add(($"{firstIdx}-{curIdx-1}",curAcronym,curColor));
                    // start new
                    firstIdx = curIdx;
                    curID = channelAnnotationData[curIdx].ID;
                    curAcronym = channelAnnotationData[curIdx].acronym;
                    curColor = channelAnnotationData[curIdx].color;
                }
            }

            // make sure to get the final set of data
            if (firstIdx < (channelAnnotationData.Count-1))
                collapsedAnnotationData.Add(($"{firstIdx}-{channelAnnotationData.Count - 1}", curAcronym, curColor));

            channelStrings = collapsedAnnotationData.Select(x => $"{x.idxRange},{x.acronym},{x.color}").ToArray();
        }
        else
            channelStrings = channelAnnotationData.Select(x => $"{x.idx},{x.ID},{x.acronym},{x.color}").ToArray();

        return string.Join(";", channelStrings);
    }

    public void SetChannelVisibility(bool visible)
    {
        // TODO
    }

    #endregion

    #region Text

    public void Probe2Text()
    {
        string apStr;
        string mlStr;
        string dvStr = "DV";

        ProbeInsertion insertion = _probeController.Insertion;

        // If we are using the 
        if (Settings.ConvertAPML2Probe)
        {
            Debug.LogWarning("Not working");
            apStr = "Forward";
            mlStr = "Right";
        }
        else
        {
            apStr = "AP";
            mlStr = "ML";
        }

        float mult = Settings.DisplayUM ? 1000f : 1f;

        Vector3 tipAtlasU = insertion.PositionSpaceU() + insertion.CoordinateSpace.RelativeOffset;
        Vector3 tipAtlasT = insertion.apmldv;

        Vector3 angles = Settings.UseIBLAngles ?
            TP_Utils.World2IBL(insertion.angles) :
            insertion.angles;

        (Vector3 entryAtlasT, float depthTransformed) = GetSurfaceCoordinateT();

        Vector3 entryAtlasU = CoordinateSpaceManager.ActiveCoordinateTransform.Transform2Space(entryAtlasT) + insertion.CoordinateSpace.RelativeOffset;

        if (Settings.ConvertAPML2Probe)
        {
            float cos = Mathf.Cos(-angles.x * Mathf.Deg2Rad);
            float sin = Mathf.Sin(-angles.x * Mathf.Deg2Rad);

            float xRot = entryAtlasT.x * cos - entryAtlasT.y * sin;
            float yRot = entryAtlasT.x * sin + entryAtlasT.y * cos;

            entryAtlasT.x = xRot;
            entryAtlasT.y = yRot;

            float tipXRot = tipAtlasT.x * cos - tipAtlasT.y * sin;
            float tipYRot = tipAtlasT.x * sin + tipAtlasT.y * cos;

            tipAtlasT.x = tipXRot;
            tipAtlasT.y = tipYRot;
        }

        string dataStr = string.Format($"{name}: ReferenceAtlas {CoordinateSpaceManager.ActiveCoordinateSpace.Name}, " +
            $"AtlasTransform {CoordinateSpaceManager.ActiveCoordinateTransform.Name}, " +
            $"Entry and Tip are ({apStr}, {mlStr}, {dvStr}), " +
            $"Entry ({round0(entryAtlasT.x * mult)}, {round0(entryAtlasT.y * mult)}, {round0(entryAtlasT.z * mult)}), " +
            $"Tip ({round0(tipAtlasT.x * mult)}, {round0(tipAtlasT.y * mult)}, {round0(tipAtlasT.z * mult)}), " +
            $"Angles ({round2(TP_Utils.CircDeg(angles.x, minYaw, maxYaw))}, {round2(angles.y)}, {round2(TP_Utils.CircDeg(angles.z, minRoll, maxRoll))}), " +
            $"Depth {round0(depthTransformed * mult)}, " +
            $"CCF Entry ({round0(entryAtlasU.x * mult)}, {round0(entryAtlasU.y * mult)}, {round0(entryAtlasU.z * mult)}), " +
            $"CCF Tip ({round0(tipAtlasU.x * mult)}, {round0(tipAtlasU.y * mult)}, {round0(tipAtlasU.z * mult)}), " +
            $"CCF Depth {round0(Vector3.Distance(entryAtlasU, tipAtlasU))}");

#if UNITY_WEBGL && !UNITY_EDITOR
        Copy2Clipboard(dataStr);
#else
        GUIUtility.systemCopyBuffer = dataStr;
#endif
    }

    private float round0(float input)
    {
        return Mathf.Round(input);
    }
    private float round2(float input)
    {
        return Mathf.Round(input * 100) / 100;
    }

    #endregion


    /// <summary>
    /// Re-scale probe panels 
    /// </summary>
    /// <param name="newPxHeight">Set the probe panels of this probe to a new height</param>
    public void ResizeProbePanel(int newPxHeight)
    {
        foreach (ProbeUIManager puimanager in _probeUIManagers)
            puimanager.ResizeProbePanel(newPxHeight);

        UIUpdateEvent.Invoke();
    }

#region Brain surface coordinate

    /// <summary>
    /// Check whether the probe is in the brain.
    /// If it is, calculate the brain surface coordinate by iterating up the probe until you leave the brain.
    /// </summary>
    public void UpdateSurfacePosition()
    {
        (Vector3 tipCoordWorld, Vector3 tipUpWorld, _) = _probeController.GetTipWorldU();

        Vector3 surfacePos25 = annotationDataset.FindSurfaceCoordinate(annotationDataset.CoordinateSpace.World2Space(tipCoordWorld),
            annotationDataset.CoordinateSpace.World2SpaceAxisChange(tipUpWorld));

        if (float.IsNaN(surfacePos25.x))
        {
            // not in the brain
            probeInBrain = false;
            brainSurfaceWorld = new Vector3(float.NaN, float.NaN, float.NaN);
            brainSurfaceWorldT = new Vector3(float.NaN, float.NaN, float.NaN);
            _brainSurface = new Vector3(float.NaN, float.NaN, float.NaN);
        }
        else
        {
            // in the brain
            probeInBrain = true;
            brainSurfaceWorld = annotationDataset.CoordinateSpace.Space2World(surfacePos25);
            brainSurfaceWorldT = CoordinateSpaceManager.WorldU2WorldT(brainSurfaceWorld);
            _brainSurface = _probeController.Insertion.World2Transformed(brainSurfaceWorld);
        }
    }


    public (Vector3 surfaceCoordinateT, float depthT) GetSurfaceCoordinateT()
    {
        return (_brainSurface, Vector3.Distance(_probeController.Insertion.apmldv, _brainSurface));
    }

    public Vector3 GetSurfaceCoordinateWorldT()
    {
        return brainSurfaceWorldT;
    }

    //public Vector3 surfaceCoordinateWorldT 

    //public (Vector3 tipCoordTransformed, Vector3 entryCoordTransformed, float depthTransformed) GetSurfaceCoordinateTransformed()
    //{
    //    // Get the tip and entry coordinates in world space, transform them -> Space -> Transformed, then calculate depth
    //    Vector3 tipCoordWorld = probeController.GetTipTransform().position;
    //    Vector3 entryCoordWorld = probeInBrain ? brainSurfaceWorld : tipCoordWorld;

    //    // Convert
    //    ProbeInsertion insertion = probeController.Insertion;
    //    Vector3 tipCoordTransformed = insertion.World2Transformed(tipCoordWorld);
    //    Vector3 entryCoordTransformed = insertion.World2Transformed(entryCoordWorld);

    //    float depth = probeInBrain ? Vector3.Distance(tipCoordTransformed, entryCoordTransformed) : 0f;

    //    return (tipCoordTransformed, entryCoordTransformed, depth);
    //}

    public bool IsProbeInBrain()
    {
        return probeInBrain;
    }
    
    /// <summary>
    ///     Move probe to brain surface
    /// </summary>
    public void DropProbeToBrainSurface()
    {
        if (probeInBrain)
        {
            _probeController.SetProbePosition(_brainSurface);
        }
        else
        {
            // We need to calculate the surface coordinate ourselves
            var tipExtensionDirection =
                ManipulatorBehaviorController.IsSetToDropToSurfaceWithDepth ? _probeController.GetTipWorldU().tipUpWorldU : Vector3.up;

            var brainSurfaceCoordinate = annotationDataset.FindSurfaceCoordinate(
                annotationDataset.CoordinateSpace.World2Space(_probeController.GetTipWorldU().tipCoordWorldU - tipExtensionDirection * 5),
                annotationDataset.CoordinateSpace.World2SpaceAxisChange(tipExtensionDirection));

            if (float.IsNaN(brainSurfaceCoordinate.x))
            {
                Debug.LogWarning("Could not find brain surface! Canceling set brain offset.");
                return;
            }

            var brainSurfaceToTransformed =
                _probeController.Insertion.World2Transformed(
                    annotationDataset.CoordinateSpace.Space2World(brainSurfaceCoordinate));

            _probeController.SetProbePosition(brainSurfaceToTransformed);
        }
    }

#endregion

#region Ephys Link Control

    /// <summary>
    /// (un)Register a probe and begin echoing position.
    /// </summary>
    /// <param name="register">To register or deregister this probe</param>
    /// <param name="manipulatorId">ID of the manipulator in real life to connect to</param>
    /// <param name="calibrated">Is the manipulator in real life calibrated</param>
    /// <param name="onSuccess">Callback function to handle a successful registration</param>
    /// <param name="onError">Callback function to handle a failed registration</param>
    public void SetIsEphysLinkControlled(bool register, string manipulatorId = null, bool calibrated = true,
        Action onSuccess = null, Action<string> onError = null)
    {
        // Exit early if this was an invalid call
        switch (register)
        {
            case true when ManipulatorBehaviorController.enabled:
            case true when string.IsNullOrEmpty(manipulatorId):
            case false when !IsEphysLinkControlled:
                return;
        }

        // Set states
        UIUpdateEvent.Invoke();

        if (register)
            CommunicationManager.Instance.RegisterManipulator(manipulatorId, () =>
            {
                IsEphysLinkControlled = true;
                ManipulatorBehaviorController.Initialize(manipulatorId, calibrated);
                onSuccess?.Invoke();
            }, err => onError?.Invoke(err));
        else
            CommunicationManager.Instance.UnregisterManipulator(manipulatorId, () =>
            {
                IsEphysLinkControlled = false;
                onSuccess?.Invoke();
            }, err => onError?.Invoke(err));
    }
    
#endregion

#region AxisControl

    public void SetAxisVisibility(bool X, bool Y, bool Z, bool depth)
    {
        if (Settings.AxisControl)
        {
            _axisControl.ZAxis.enabled = Z;
            _axisControl.XAxis.enabled = X;
            _axisControl.YAxis.enabled = Y;
            _axisControl.DepthAxis.enabled = depth;
        }
        else
        {
            _axisControl.ZAxis.enabled = false;
            _axisControl.XAxis.enabled = false;
            _axisControl.YAxis.enabled = false;
            _axisControl.DepthAxis.enabled = false;
        }
    }

    public void SetAxisTransform(Transform transform)
    {
        _axisControl.transform.position = transform.position;
        _axisControl.transform.rotation = transform.rotation;
    }

    #endregion AxisControl

    #region Materials

    private void SetMaterials()
    {
        switch (_probeDisplayType)
        {
            case ProbeDisplayType.Opaque:
                SetMaterialsDefault();
                break;
            case ProbeDisplayType.Transparent:
                SetMaterialsTransparent();
                break;
            case ProbeDisplayType.Line:
                SetMaterialsLine();
                break;
        }
    }

    /// <summary>
    /// Set all Renderer components to use the ghost material
    /// </summary>
    private void SetMaterialsTransparent()
    {
#if UNITY_EDITOR
        Debug.Log($"Setting materials for {name} to transparent");
#endif
        if (_lineRenderer != null)
            _lineRenderer.enabled = false;

        var currentColorTint = new Color(Color.r, Color.g, Color.b, .2f);
        foreach (var childRenderer in _activeRenderers)
        {
            childRenderer.enabled = true;
            childRenderer.material = _ghostMaterial;

            // Apply tint to the material
            childRenderer.material.color = currentColorTint;
        }
    }

    /// <summary>
    /// Reverse a previous call to SetMaterialsTransparent()
    /// </summary>
    private void SetMaterialsDefault()
    {
#if UNITY_EDITOR
        Debug.Log($"Setting materials for {name} to default");
#endif
        if (_lineRenderer != null)
            _lineRenderer.enabled = false;

        foreach (var childRenderer in _activeRenderers)
        {
            childRenderer.enabled = true;
            childRenderer.material = _defaultMaterials[childRenderer.gameObject];
        }
    }

    private LineRenderer _lineRenderer;
    private void SetMaterialsLine()
    {
#if UNITY_EDITOR
        Debug.Log($"Setting materials for {name} to line");
#endif
        foreach (var childRenderer in _activeRenderers)
            childRenderer.enabled = false;

        if (_lineRenderer != null)
            _lineRenderer.enabled = true;
        else
        {
            GameObject probeTipGO = _probeController.ProbeTipT.gameObject;
            _lineRenderer = probeTipGO.AddComponent<LineRenderer>();
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _lineRenderer.startWidth = 0.1f;
            _lineRenderer.endWidth = 0.1f;

            _lineRenderer.material = _lineMaterial;
            _lineRenderer.material.color = Color;

            _lineRenderer.useWorldSpace = false;

            _lineRenderer.positionCount = 2;

            var channelData = GetChannelRangemm();
            _lineRenderer.SetPositions(new Vector3[] {
            Vector3.zero,
            Vector3.up * channelData.fullHeight});
        }
    }

    #endregion
}

[Serializable]
public struct ProbeData
{
    // ProbeInsertion
    public Vector3 APMLDV;
    public Vector3 Angles;

    // CoordinateSpace/Transform
    public string CoordSpaceName;
    public string CoordTransformName;
    public Vector4 ZeroCoordOffset;

    // ChannelMap
    public string SelectionLayerName;

    // Data
    public int Type;
    public Color Color;
    public string UUID;
    public string Name;

    // API
    public string APITarget;

    // Ephys Link
    public string ManipulatorType;
    public string ManipulatorID;
    public float BrainSurfaceOffset;
    public bool Drop2SurfaceWithDepth;
    public bool IsRightHanded;

    public static ProbeData ProbeManager2ProbeData(ProbeManager probeManager)
    {
        ProbeData data = new ProbeData();

        data.APMLDV = probeManager.ProbeController.Insertion.apmldv;
        data.Angles = probeManager.ProbeController.Insertion.angles;

        data.CoordSpaceName = probeManager.ProbeController.Insertion.CoordinateSpace.Name;

        if (probeManager.ProbeController.Insertion.CoordinateTransform.Name.Equals("Custom"))
            data.CoordTransformName = CoordinateSpaceManager.OriginalTransform.Name;
        else
            data.CoordTransformName = probeManager.ProbeController.Insertion.CoordinateTransform.Name;

        data.SelectionLayerName = probeManager.SelectionLayerName;

        data.Type = (int)probeManager.ProbeType;
        data.Color = probeManager.Color;
        data.UUID = probeManager.UUID;
        data.Name = probeManager.name;

        data.APITarget = probeManager.APITarget;

        // Manipulator Behavior data (if it exists)
        if (!probeManager.ManipulatorBehaviorController) return data;
        
        data.ManipulatorType = probeManager.ManipulatorBehaviorController.ManipulatorType;
        data.ManipulatorID = probeManager.ManipulatorBehaviorController.ManipulatorID;
        data.ZeroCoordOffset = probeManager.ManipulatorBehaviorController.ZeroCoordinateOffset;
        data.BrainSurfaceOffset = probeManager.ManipulatorBehaviorController.BrainSurfaceOffset;
        data.Drop2SurfaceWithDepth = probeManager.ManipulatorBehaviorController.IsSetToDropToSurfaceWithDepth;
        data.IsRightHanded = probeManager.ManipulatorBehaviorController.IsRightHanded;

        return data;
    }
}

public enum ProbeDisplayType
{
    Opaque,
    Transparent,
    Line
}