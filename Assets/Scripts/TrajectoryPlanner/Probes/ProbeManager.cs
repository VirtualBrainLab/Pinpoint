using System;
using System.Collections.Generic;
using CoordinateSpaces;
using CoordinateTransforms;
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
    public static List<ProbeManager> Instances = new List<ProbeManager>();
    public static ProbeManager ActiveProbeManager;
    void OnEnable() => Instances.Add(this);
    void OnDestroy()
    {
        Debug.Log($"Destroying probe: {name}");
        if (Instances.Contains(this))
            Instances.Remove(this);
        // clean up the ProbeInsertion
        ProbeInsertion.Instances.Remove(ProbeController.Insertion);
        if (ProbeController.Insertion.Targetable)
            ProbeInsertion.TargetableInstances.Remove(ProbeController.Insertion);
    }

    public static HashSet<string> RightHandedManipulatorIDs { get; private set; }
    #endregion

    #region Events

    public UnityEvent UIUpdateEvent;
    public UnityEvent ActivateProbeEvent;
    public UnityEvent EphysLinkControlChangeEvent;

    #endregion


    #region Ephys Link

    private float _brainSurfaceOffset;
    public float BrainSurfaceOffset
    {
        get => _brainSurfaceOffset;
        set
        {
            _brainSurfaceOffset = value;
            BrainSurfaceOffsetChangedEvent.Invoke(value);
        }
    }
    public UnityEvent<float> BrainSurfaceOffsetChangedEvent;

    private bool _isSetToDropToSurfaceWithDepth = true;

    public bool IsSetToDropToSurfaceWithDepth
    {
        get => _isSetToDropToSurfaceWithDepth;
        set
        {
            if (BrainSurfaceOffset != 0) return;
            _isSetToDropToSurfaceWithDepth = value;
            IsSetToDropToSurfaceWithDepthChangedEvent.Invoke(value);
        }
    }

    public UnityEvent<bool> IsSetToDropToSurfaceWithDepthChangedEvent;

    private Vector4 _lastManipulatorPosition = Vector4.negativeInfinity;
    public int AutomaticMovementSpeed { get; private set; } = 500; // Default to 500 um/s

    /// <summary>
    ///     Reference to probe manager of this probe's ghost
    /// </summary>
    public ProbeManager GhostProbeManager { get; set; }

    /// <summary>
    ///     Reference to probe manager of this probe's original probe (for if this is a ghost probe)
    /// </summary>
    public ProbeManager OriginalProbeManager { get; set; }

    /// <summary>
    ///     Getter property for if this probe is a ghost probe
    /// </summary>
    public bool IsGhost => OriginalProbeManager != null;

    /// <summary>
    ///     Getter property for if this probe is the original probe
    /// </summary>
    public bool IsOriginal => OriginalProbeManager == null;

    /// <summary>
    ///     Getter property for if this probe (an original) has a ghosting probe
    /// </summary>
    public bool HasGhost => GhostProbeManager != null;

    #endregion

    #region Identifiers
    public string UUID { get; private set; }
    private string _overrideName;
    #endregion

    // Exposed fields to collect links to other components inside of the Probe prefab
    [FormerlySerializedAs("probeColliders")][SerializeField] private List<Collider> _probeColliders;
    [FormerlySerializedAs("probeUIManagers")][SerializeField] private List<ProbeUIManager> _probeUIManagers;
    [FormerlySerializedAs("probeRenderer")][SerializeField] private Renderer _probeRenderer;
    [SerializeField] private RecordingRegion _recRegion;

    private AxisControl _axisControl;
    public ProbeProperties.ProbeType ProbeType;

    [FormerlySerializedAs("probeController")][SerializeField] private ProbeController _probeController;

    [FormerlySerializedAs("ghostMaterial")][SerializeField] private Material _ghostMaterial;

    private Dictionary<GameObject, Material> defaultMaterials;

    #region Channel map
    public string SelectionLayerName { get; private set; }
    private float _channelMinY;
    private float _channelMaxY;
    /// <summary>
    /// Return the minimum and maximum channel position in the current selection in mm
    /// </summary>
    public (float, float) GetChannelMinMaxYCoord { get { return (_channelMinY, _channelMaxY); } }
    public ChannelMap ChannelMap { get; private set; }
    #endregion

    // Probe position data
    private Vector3 _recRegionBaseCoordU;
    private Vector3 _recRegionTopCoordU;

    public (Vector3 tipCoordU, Vector3 endCoordU) RecRegionCoordWorldU { get { return (_recRegionBaseCoordU, _recRegionTopCoordU); } }

    // Text
    private const float minPhi = -180;
    private const float maxPhi = 180f;
    private const float minSpin = -180f;
    private const float maxSpin = 180f;

    // Brain surface position
    private CCFAnnotationDataset annotationDataset;
    private bool probeInBrain;
    private Vector3 _brainSurface;
    private Vector3 brainSurfaceWorld;
    private Vector3 brainSurfaceWorldT;

    #region Accessors

    public ProbeController ProbeController { get => _probeController;
        private set => _probeController = value;
    }

    public ManipulatorBehaviorController ManipulatorBehaviorController =>
        gameObject.GetComponent<ManipulatorBehaviorController>();

    public bool IsEphysLinkControlled
    {
        get => ManipulatorBehaviorController.enabled;
        set
        {
            ManipulatorBehaviorController.enabled = value;
            EphysLinkControlChangeEvent.Invoke();
        }
    }

    public string APITarget { get; set; }

    public Color Color
    {
        get
        {
            if (_probeRenderer == null)
                return new Color();
            return _probeRenderer.material.color;
        }

        set
        {
            // try to return the current color
            ProbeProperties.ReturnProbeColor(_probeRenderer.material.color);
            // use up the new color (if it's a default color)
            ProbeProperties.UseColor(value);

            _probeRenderer.material.color = value;

            foreach (ProbeUIManager puiManager in _probeUIManagers)
                puiManager.UpdateColors();
        }
    }



    public void SetLock(bool locked)
    {
        _probeController.Locked = locked;
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

    #endregion

    #region Unity

    private void Awake()
    {
        UUID = Guid.NewGuid().ToString();
        UpdateName();
        APITarget = name;

        defaultMaterials = new();
        // Request for ID and color if this is a normal probe
        if (IsOriginal)
        {
            // Record default materials
            foreach (var childRenderer in transform.GetComponentsInChildren<Renderer>())
            {
                defaultMaterials.Add(childRenderer.gameObject, childRenderer.material);
            }
        }

        if (_probeRenderer != null)
            _probeRenderer.material.color = ProbeProperties.GetNextProbeColor();

        // Pull the tpmanager object and register this probe
        _probeController.Register(this);

        // Get the channel map and selection layer
        ChannelMap = ChannelMapManager.GetChannelMap(ProbeType);
        SelectionLayerName = "default";

        // Get access to the annotation dataset and world-space boundaries
        annotationDataset = VolumeDatasetManager.AnnotationDataset;

        _axisControl = GameObject.Find("AxisControl").GetComponent<AxisControl>();

        _probeController.FinishedMovingEvent.AddListener(UpdateName);
        _probeController.MovedThisFrameEvent.AddListener(ProbeMoved);
    }

    private void Start()
    {
#if UNITY_EDITOR
        Debug.Log($"(ProbeManager) New probe created with UUID: {UUID}");
#endif
        RightHandedManipulatorIDs = Settings.RightHandedManipulatorIds;
        UpdateSelectionLayer(SelectionLayerName);
    }

    /// <summary>
    /// Called by Unity when this object is destroyed. 
    /// Unregisters the probe from tpmanager
    /// Removes the probe panels and the position text.
    /// </summary>
    public void Destroy()
    {
        // Delete this gameObject
        foreach (ProbeUIManager puimanager in _probeUIManagers)
            puimanager.Destroy();

        Instances.Remove(this);
        ProbeController.Insertion.Targetable = false;

        if (IsOriginal)
            ProbeProperties.ReturnProbeColor(Color);

        ColliderManager.RemoveProbeColliderInstances(_probeColliders);
        
        // Unregister this probe from the ephys link
        if (IsEphysLinkControlled)
        {
            SetIsEphysLinkControlled(false);
        }
    }

    #endregion

    /// <summary>
    /// Called by the TPManager when this Probe becomes the active probe
    /// </summary>
    public void SetActive(bool active)
    {
#if UNITY_EDITOR
        Debug.Log($"{name} becoming {(active ? "active" : "inactive")}");
#endif

        if (active)
            ColliderManager.AddProbeColliderInstances(_probeColliders, true);
        else
            ColliderManager.AddProbeColliderInstances(_probeColliders, false);

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

    public void CheckProbeTransformState()
    {
        if (_probeController.Insertion.CoordinateTransform != CoordinateSpaceManager.ActiveCoordinateTransform)
        {
            QuestionDialogue.SetYesCallback(ChangeTransform);
            QuestionDialogue.NewQuestion("The coordinate transform in the scene is mis-matched with the transform in this Probe insertion. Do you want to replace the transform?");
        }
    }

    private void ChangeTransform()
    {
        Debug.LogWarning("Insertion coordinates are not being transformed into the new space!! This might not be expected behavior");
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
        if (_overrideName != null)
        {
            name = _overrideName;
        }
        else
        {
            // Check if this probe is in the brain
            if (probeInBrain)
            {
                name = $"{_probeUIManagers[0].MaxArea}-{UUID.Substring(0, 8)}";
            }
            else
                name = UUID.Substring(0, 8);
        }
    }

    public void OverrideName(string newName)
    {
        _overrideName = newName;
        UpdateName();
        UIUpdateEvent.Invoke();
    }


    /// <summary>
    /// Move the probe
    /// </summary>
    /// <returns>Whether or not the probe moved on this frame</returns>
    public void MoveProbe()
    {
        // Cancel movement if being controlled by EphysLink
        if (IsEphysLinkControlled)
            return;

        ((DefaultProbeController)_probeController).MoveProbe_Keyboard();
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
        if (_recRegion == null)
            return;

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
#if UNITY_EDITOR
        Debug.Log($"Minimum channel coordinate {_channelMinY} max {_channelMaxY}");
#endif
        foreach (ProbeUIManager puiManager in _probeUIManagers)
            puiManager.UpdateChannelMap();

        _recRegion.SetSize(_channelMinY, _channelMaxY);
    }

    /// <summary>
    /// Get a serialized representation of the channel ID data
    /// </summary>
    /// <returns></returns>
    public string GetChannelAnnotationIDs()
    {
        // Get the channel data
        var channelMapData = ChannelMap.GetChannelPositions("all");

        string[] channelStrings;

        if (ProbeProperties.FourShank(ProbeType))
        {
            channelStrings = new string[channelMapData.Count * 4];

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

                    channelStrings[elecIdx] = $"{elecIdx},{ID}";
                }
            }
        }
        else
        {
            channelStrings = new string[channelMapData.Count];

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

                channelStrings[i] = $"{i},{ID}";
            }
        }

        var returnString = string.Join(";", channelStrings);

        return returnString;
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
        string dvStr;
        string depthStr;

        ProbeInsertion insertion = _probeController.Insertion;
        string prefix = insertion.CoordinateTransform.Prefix;

        // If we are using the 
        if (Settings.ConvertAPML2Probe)
        {
            Debug.LogWarning("Not working");
            apStr = prefix + "Forward";
            mlStr = prefix + "Right";
        }
        else
        {
            apStr = prefix + "AP";
            mlStr = prefix + "ML";
        }
        dvStr = prefix + "DV";
        depthStr = prefix + "Depth";

        float mult = Settings.DisplayUM ? 1000f : 1f;

        Vector3 apmldvS = insertion.PositionSpaceU() + insertion.CoordinateSpace.RelativeOffset;

        Vector3 angles = Settings.UseIBLAngles ?
            TP_Utils.World2IBL(insertion.angles) :
            insertion.angles;

        (Vector3 entryCoordTranformed, float depthTransformed) = GetSurfaceCoordinateT();

        if (Settings.ConvertAPML2Probe)
        {
            float cos = Mathf.Cos(-angles.x * Mathf.Deg2Rad);
            float sin = Mathf.Sin(-angles.x * Mathf.Deg2Rad);

            float xRot = entryCoordTranformed.x * cos - entryCoordTranformed.y * sin;
            float yRot = entryCoordTranformed.x * sin + entryCoordTranformed.y * cos;

            entryCoordTranformed.x = xRot;
            entryCoordTranformed.y = yRot;
        }

        string updateStr = string.Format("{0} Surface coordinate: " + 
            "({1}:{2}, {3}:{4}, {5}:{6})" +
            " Angles: (Az:{7}, El:{8}, Sp:{9})" + 
            " Depth: {10}:{11}" + 
            " Tip coordinate: (ccfAP:{12}, ccfML: {13}, ccfDV:{14})",
            name, 
            apStr, round0(entryCoordTranformed.x * mult), mlStr, round0(entryCoordTranformed.y * mult), dvStr, round0(entryCoordTranformed.z * mult), 
            round2(TP_Utils.CircDeg(angles.x, minPhi, maxPhi)), round2(angles.y), round2(TP_Utils.CircDeg(angles.z, minSpin, maxSpin)),
            depthStr, round0(depthTransformed * mult),
            round0(apmldvS.x * mult), round0(apmldvS.y * mult), round0(apmldvS.z * mult));

#if UNITY_WEBGL && !UNITY_EDITOR
        Copy2Clipboard(updateStr);
#else
        GUIUtility.systemCopyBuffer = updateStr;
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
                IsSetToDropToSurfaceWithDepth ? _probeController.GetTipWorldU().tipUpWorldU : Vector3.up;

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
                ManipulatorBehaviorController.Disable();
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


    /// <summary>
    /// Set all Renderer components to use the ghost material
    /// </summary>
    public void SetMaterialsTransparent()
    {
#if UNITY_EDITOR
        Debug.Log($"Setting materials for {name} to transparent");
#endif
        var currentColorTint = new Color(Color.r, Color.g, Color.b, .2f);
        foreach (var childRenderer in transform.GetComponentsInChildren<Renderer>())
        {
            childRenderer.material = _ghostMaterial;

            // Tint transparent material for non-ghost probes
            if (!IsGhost) childRenderer.material.color = currentColorTint;
        }
    }

    /// <summary>
    /// Reverse a previous call to SetMaterialsTransparent()
    /// </summary>
    public void SetMaterialsDefault()
    {
#if UNITY_EDITOR
        Debug.Log($"Setting materials for {name} to default");
#endif
        foreach (var childRenderer in transform.GetComponentsInChildren<Renderer>())
            if (defaultMaterials.ContainsKey(childRenderer.gameObject))
                childRenderer.material = defaultMaterials[childRenderer.gameObject];
    }

#endregion
}

[Serializable]
public class ProbeData
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
    public string ManipulatordID;
    public float BrainSurfaceOffset;
    public bool Drop2SurfaceWithDepth;

    public static ProbeData ProbeManager2ProbeData(ProbeManager probeManager)
    {
        ProbeData data = new ProbeData();

        data.APMLDV = probeManager.ProbeController.Insertion.apmldv;
        data.Angles = probeManager.ProbeController.Insertion.angles;

        data.CoordSpaceName = probeManager.ProbeController.Insertion.CoordinateSpace.Name;
        data.CoordTransformName = probeManager.ProbeController.Insertion.CoordinateTransform.Name;

        data.SelectionLayerName = probeManager.SelectionLayerName;

        data.Type = (int)probeManager.ProbeType;
        data.Color = probeManager.Color;
        data.UUID = probeManager.UUID;
        data.Name = probeManager.name;

        data.APITarget = probeManager.APITarget;

        // Manipulator Behavior data
        data.ManipulatordID = probeManager.ManipulatorBehaviorController.ManipulatorID;
        data.ZeroCoordOffset = probeManager.ManipulatorBehaviorController.ZeroCoordinateOffset;
        data.BrainSurfaceOffset = probeManager.ManipulatorBehaviorController.BrainSurfaceOffset;
        data.Drop2SurfaceWithDepth = probeManager.IsSetToDropToSurfaceWithDepth;

        return data;
    }
}