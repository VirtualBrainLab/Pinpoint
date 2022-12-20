using System;
using System.Collections.Generic;
using EphysLink;
using TMPro;
using TrajectoryPlanner;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 3D space control for Neuropixels probes in the Trajectory Planner scene
/// </summary>
public class ProbeManager : MonoBehaviour
{
    #region Static fields
    public static List<ProbeManager> instances = new List<ProbeManager>();
    void OnEnable() => instances.Add(this);
    void OnDisable() => instances.Remove(this);
    #endregion

    // Internal flags that track whether we are in manual control or drag/link control mode
    public bool IsEphysLinkControlled { get; private set; }
    public string UUID { get; private set; }

    #region Ephys Link

    private CommunicationManager _ephysLinkCommunicationManager;

    /// <summary>
    ///     Manipulator ID from Ephys Link
    /// </summary>
    public string ManipulatorId { get; private set; }
    private float _phiCos = 1f;
    private float _phiSin;
    public Vector4 ZeroCoordinateOffset { get; set; } = Vector4.negativeInfinity;
    public float BrainSurfaceOffset { get; set; }
    public bool CanChangeBrainSurfaceOffsetAxis => BrainSurfaceOffset == 0;
    public bool IsSetToDropToSurfaceWithDepth { get; private set; } = true;
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

    // Exposed fields to collect links to other components inside of the Probe prefab
    [FormerlySerializedAs("probeColliders")] [SerializeField] private List<Collider> _probeColliders;
    [FormerlySerializedAs("probeUIManagers")] [SerializeField] private List<ProbeUIManager> _probeUIManagers;
    [FormerlySerializedAs("probeRenderer")] [SerializeField] private Renderer _probeRenderer;
    [SerializeField] private int _probeType;
    public int ProbeType => _probeType;

    [FormerlySerializedAs("probeController")] [SerializeField] private ProbeController _probeController;

    [FormerlySerializedAs("ghostMaterial")] [SerializeField] private Material _ghostMaterial;
    private Dictionary<GameObject, Material> defaultMaterials;

    private TrajectoryPlannerManager tpmanager;

    // Text
    public int ID { get; set; }
    private TextMeshProUGUI textUI;
    private const float minPhi = -180;
    private const float maxPhi = 180f;
    private const float minSpin = -180f;
    private const float maxSpin = 180f;

    // Brain surface position
    private CCFAnnotationDataset annotationDataset;
    private bool probeInBrain;
    private Vector3 brainSurface;
    private Vector3 brainSurfaceWorld;
    private Vector3 brainSurfaceWorldT;

    // Colliders
    private List<GameObject> visibleProbeColliders;
    private Dictionary<GameObject, Material> visibleOtherColliders;

    #region Accessors

    public Color GetColor()
    {
        return _probeRenderer.material.color;
    }
    public List<Collider> GetProbeColliders()
    {
        return _probeColliders;
    }

    public void DisableAllColliders()
    {
        foreach (var probeCollider in _probeColliders)
        {
            probeCollider.enabled = false;
        }
    }

    /// <summary>
    ///     Get a reference to the probe's controller.
    /// </summary>
    /// <returns>Reference to this probe's controller</returns>
    public ProbeController GetProbeController()
    {
        return _probeController;
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

        defaultMaterials = new Dictionary<GameObject, Material>();

        // Pull the tpmanager object and register this probe
        GameObject main = GameObject.Find("main");
        tpmanager = main.GetComponent<TrajectoryPlannerManager>();
        tpmanager.RegisterProbe(this);
        _probeController.Register(tpmanager, this);

        // Pull ephys link communication manager
        _ephysLinkCommunicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();

        // Get access to the annotation dataset and world-space boundaries
        annotationDataset = tpmanager.GetAnnotationDataset();

        visibleProbeColliders = new List<GameObject>();
        visibleOtherColliders = new Dictionary<GameObject, Material>();
    }

    private void Start()
    {
        Debug.Log($"New probe created with UUID: {UUID}");
        // Request for ID and color if this is a normal probe
        if (IsOriginal)
        {
            ID = tpmanager.GetNextProbeId();
            name = "PROBE_" + ID;
            _probeRenderer.material.color = tpmanager.GetNextProbeColor();
            
            // Record default materials
            foreach (var childRenderer in transform.GetComponentsInChildren<Renderer>())
            {
                defaultMaterials.Add(childRenderer.gameObject, childRenderer.material);
            }
        }

        foreach (var probeUIManager in _probeUIManagers)
        {
            probeUIManager.UpdateColors();
        }
        tpmanager.UpdateQuickSettingsProbeIdText();
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
    public void SetActive()
    {
        if (_probeController.Insertion.CoordinateTransform != tpmanager.GetActiveCoordinateTransform())
        {
            TP_QuestionDialogue questionDialogue = tpmanager.GetQuestionDialogue();
            questionDialogue.SetYesCallback(ChangeTransform);
            questionDialogue.NewQuestion("The coordinate transform in the scene is mis-matched with the transform in this Probe insertion. Do you want to replace the transform?");
        }
    }

    private void ChangeTransform()
    {
        ProbeInsertion originalInsertion = _probeController.Insertion;
        Debug.LogWarning("Insertion coordinates are not being transformed!! This might not be expected behavior");
        _probeController.SetProbePosition(new ProbeInsertion(originalInsertion.apmldv, originalInsertion.angles, tpmanager.GetCoordinateSpace(), tpmanager.GetActiveCoordinateTransform()));
    }

    public void UpdateUI()
    {
        // Reset our probe UI panels
        foreach (ProbeUIManager puimanager in _probeUIManagers)
            puimanager.ProbeMoved();
    }

    public void SetUIVisibility(bool state)
    {
        foreach (ProbeUIManager puimanager in _probeUIManagers)
            puimanager.SetProbePanelVisibility(state);
    }


    public void OverrideUUID(string newUUID)
    {
#if UNITY_EDITOR
        Debug.Log(string.Format("Override UUID to {0}", newUUID));
#endif
        UUID = newUUID;
    }

    /// <summary>
    /// Update the size of the recording region.
    /// </summary>
    /// <param name="newSize">New size of recording region in mm</param>
    public void ChangeRecordingRegionSize(float newSize)
    {
        ((DefaultProbeController)_probeController).ChangeRecordingRegionSize(newSize);

        // Update all the UI panels
        UpdateUI();
    }


    /// <summary>
    /// Move the probe with the option to check for collisions
    /// </summary>
    /// <param name="checkForCollisions">Set to true to check for collisions with rig colliders and other probes</param>
    /// <returns>Whether or not the probe moved on this frame</returns>
    public bool MoveProbe(bool checkForCollisions = false)
    {
        // Cancel movement if being controlled by EphysLink
        if (IsEphysLinkControlled)
            return false;

        return ((DefaultProbeController)_probeController).MoveProbe_Keyboard(checkForCollisions);
    }


    /// <summary>
    /// Check for collisions between the probe colliders and a list of other colliders
    /// </summary>
    /// <param name="otherColliders">colliders to check against</param>
    /// <returns></returns>
    public void CheckCollisions(List<Collider> otherColliders)
    {
        if (tpmanager.GetCollisions())
        {
            bool collided = CheckCollisionsHelper(otherColliders);

            if (collided)
                tpmanager.SetCollisionPanelVisibility(true);
            else
            {
                tpmanager.SetCollisionPanelVisibility(false);
                ClearCollisionMesh();
            }
        }
        else
        {
            tpmanager.SetCollisionPanelVisibility(false);
            ClearCollisionMesh();
        }
    }

    /// <summary>
    /// Internal function to perform collision checks between Collider components
    /// </summary>
    /// <param name="otherColliders"></param>
    /// <returns></returns>
    private bool CheckCollisionsHelper(List<Collider> otherColliders)
    {
        foreach (Collider activeCollider in _probeColliders)
        {
            foreach (Collider otherCollider in otherColliders)
            {
                Vector3 dir;
                float dist;
                if (Physics.ComputePenetration(activeCollider, activeCollider.transform.position, activeCollider.transform.rotation, otherCollider, otherCollider.transform.position, otherCollider.transform.rotation, out dir, out dist))
                {
                    CreateCollisionMesh(activeCollider, otherCollider);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// When collisions occur we want to make the colliders we hit change material, but we might need to later swap them back
    /// </summary>
    /// <param name="activeCollider"></param>
    /// <param name="otherCollider"></param>
    private void CreateCollisionMesh(Collider activeCollider, Collider otherCollider)
    {
        if (!visibleProbeColliders.Contains(activeCollider.gameObject))
        {
            visibleProbeColliders.Add(activeCollider.gameObject);
            activeCollider.gameObject.GetComponent<Renderer>().enabled = true;
        }

        GameObject otherColliderGO = otherCollider.gameObject;
        if (!visibleOtherColliders.ContainsKey(otherColliderGO))
        {
            visibleOtherColliders.Add(otherColliderGO, otherColliderGO.GetComponent<Renderer>().material);
            otherColliderGO.GetComponent<Renderer>().material = tpmanager.GetCollisionMaterial();
        }
    }

    // Clear probe colliders by disabling the renderers and then clear the other colliders by swapping back their materials
    private void ClearCollisionMesh()
    {
        if (visibleProbeColliders.Count > 0 || visibleOtherColliders.Count > 0)
        {
            foreach (GameObject probeColliderGO in visibleProbeColliders)
                probeColliderGO.GetComponent<Renderer>().enabled = false;
            foreach (KeyValuePair<GameObject, Material> kvp in visibleOtherColliders)
                kvp.Key.GetComponent<Renderer>().material = kvp.Value;

            visibleProbeColliders.Clear();
            visibleOtherColliders.Clear();
        }
    }

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
        if (tpmanager.GetSetting_ConvertAPMLAxis2Probe())
        {
            Debug.LogWarning("Not working");
            apStr = "not-implemented";
            mlStr = "not-implemented";
            dvStr = "not-implemented";
            depthStr = "not-implemented";
        }
        else
        {
            apStr = prefix + "AP";
            mlStr = prefix + "ML";
            dvStr = prefix + "DV";
            depthStr = prefix + "Depth";
        }

        float mult = tpmanager.GetSetting_DisplayUM() ? 1000f : 1f;

        Vector3 apmldvS = insertion.PositionSpace() + insertion.CoordinateSpace.RelativeOffset;

        Vector3 angles = tpmanager.GetSetting_UseIBLAngles() ?
            Utils.World2IBL(insertion.angles) :
            insertion.angles;

        (Vector3 entryCoordTranformed, float depthTransformed) = GetSurfaceCoordinateT();
        

        string updateStr = string.Format("Probe #{0} Surface coordinate: " + 
            "({1}:{2}, {3}:{4}, {5}:{6})" +
            " Angles: (Az:{7}, El:{8}, Sp:{9})" + 
            " Depth: {10}:{11}" + 
            " Tip coordinate: (ccfAP:{12}, ccfML: {13}, ccfDV:{14})",
            ID, 
            apStr, round0(entryCoordTranformed.x * mult), mlStr, round0(entryCoordTranformed.y * mult), dvStr, round0(entryCoordTranformed.z * mult), 
            round2(Utils.CircDeg(angles.x, minPhi, maxPhi)), round2(angles.y), round2(Utils.CircDeg(angles.z, minSpin, maxSpin)),
            depthStr, round0(depthTransformed * mult),
            round0(apmldvS.x * mult), round0(apmldvS.y * mult), round0(apmldvS.z * mult));

        GUIUtility.systemCopyBuffer = updateStr;
    }

    #endregion


    public void RegisterProbeCallback(int ID, Color probeColor)
    {
        this.ID = ID;
        name = "PROBE_" + this.ID;
        _probeRenderer.material.color = probeColor;
    }


    private float round0(float input)
    {
        return Mathf.Round(input);
    }
    private float round2(float input)
    {
        return Mathf.Round(input * 100) / 100;
    }

    /// <summary>
    /// Re-scale probe panels 
    /// </summary>
    /// <param name="newPxHeight">Set the probe panels of this probe to a new height</param>
    public void ResizeProbePanel(int newPxHeight)
    {
        foreach (ProbeUIManager puimanager in _probeUIManagers)
        {
            puimanager.ResizeProbePanel(newPxHeight);
            puimanager.ProbeMoved();
        }
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
            brainSurface = new Vector3(float.NaN, float.NaN, float.NaN);
        }
        else
        {
            // in the brain
            probeInBrain = true;
            brainSurfaceWorld = annotationDataset.CoordinateSpace.Space2World(surfacePos25);
            brainSurfaceWorldT = tpmanager.WorldU2WorldT(brainSurfaceWorld);
            brainSurface = _probeController.Insertion.World2Transformed(brainSurfaceWorld);
        }
    }


    public (Vector3 surfaceCoordinateT, float depthT) GetSurfaceCoordinateT()
    {
        return (brainSurface, Vector3.Distance(_probeController.Insertion.apmldv, brainSurface));
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

    #endregion

    /// <summary>
    /// to implement 
    /// </summary>
    public void LockProbeToArea()
    {

    }

    #region Ephys Link and Control

    #region Property Manipulators


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
            case true when IsEphysLinkControlled:
            case true when string.IsNullOrEmpty(manipulatorId):
                return;
        }

        // Set states
        IsEphysLinkControlled = register;
        tpmanager.UpdateQuickSettings();

        if (register)
            _ephysLinkCommunicationManager.RegisterManipulator(manipulatorId, () =>
            {
                Debug.Log("Manipulator Registered");
                ManipulatorId = manipulatorId;

                // Remove insertion from targeting options
                tpmanager.TargetProbeInsertions.Remove(_probeController.Insertion);

                if (calibrated)
                    // Bypass calibration and start echoing
                    _ephysLinkCommunicationManager.BypassCalibration(manipulatorId, StartEchoing);
                else
                    // Enable write
                    _ephysLinkCommunicationManager.SetCanWrite(manipulatorId, true, 1,
                        _ =>
                        {
                            // Calibrate
                            _ephysLinkCommunicationManager.Calibrate(manipulatorId,
                                () =>
                                {
                                    // Disable write and start echoing
                                    _ephysLinkCommunicationManager.SetCanWrite(manipulatorId, false, 0,
                                        _ => StartEchoing());
                                });
                        });

                onSuccess?.Invoke();
            }, err => onError?.Invoke(err));
        else
            _ephysLinkCommunicationManager.UnregisterManipulator(ManipulatorId, () =>
            {
                Debug.Log("Manipulator Unregistered");
                ResetManipulatorProperties();
                onSuccess?.Invoke();
            }, err => onError?.Invoke(err));


        // Start echoing process
        void StartEchoing()
        {
            // Read and start echoing position
            _ephysLinkCommunicationManager.GetPos(manipulatorId, vector4 =>
            {
                if (ZeroCoordinateOffset.Equals(Vector4.negativeInfinity)) ZeroCoordinateOffset = vector4;
                EchoPositionFromEphysLink(vector4);
            });
        }
    }

    /// <summary>
    ///     Set manipulator properties such as ID and positional offsets back to defaults.
    /// </summary>
    private void ResetManipulatorProperties()
    {
        ManipulatorId = null;
        ZeroCoordinateOffset = Vector4.negativeInfinity;
        BrainSurfaceOffset = 0;
    }


    /// <summary>
    /// Set the automatic movement speed of this probe (when put under automatic control)
    /// </summary>
    /// <param name="speed">Speed in um/s</param>
    public void SetAutomaticMovementSpeed(int speed)
    {
        // Ghosts don't have automatic movement speeds
        if (IsGhost)
        {
            return;
        }

        AutomaticMovementSpeed = speed;
    }
    
    /// <summary>
    ///     Update x coordinate of manipulator space offset to zero coordinate.
    /// </summary>
    /// <param name="x">X coordinate</param>
    public void SetZeroCoordinateOffsetX(float x)
    {
        var temp = ZeroCoordinateOffset;
        temp.x = x;
        ZeroCoordinateOffset = temp;
    }

    /// <summary>
    ///     Update y coordinate of manipulator space offset to zero coordinate.
    /// </summary>
    /// <param name="y">Y coordinate</param>
    public void SetZeroCoordinateOffsetY(float y)
    {
        var temp = ZeroCoordinateOffset;
        temp.y = y;
        ZeroCoordinateOffset = temp;
    }


    /// <summary>
    ///     Update Z coordinate of manipulator space offset to zero coordinate.
    /// </summary>
    /// <param name="z">Z coordinate</param>
    public void SetZeroCoordinateOffsetZ(float z)
    {
        var temp = ZeroCoordinateOffset;
        temp.z = z;
        ZeroCoordinateOffset = temp;
    }


    /// <summary>
    ///     Update D coordinate of manipulator space offset to zero coordinate.
    /// </summary>
    /// <param name="depth">D coordinate</param>
    public void SetZeroCoordinateOffsetDepth(float depth)
    {
        var temp = ZeroCoordinateOffset;
        temp.w = depth;
        ZeroCoordinateOffset = temp;
    }
    
    /// <summary>
    ///     Set manipulator space offset from brain surface as Depth from manipulator or probe coordinates.
    /// </summary>
    public void SetBrainSurfaceOffset()
    {
        if (probeInBrain)
        {
            // Just calculate the distance from the probe tip position to the brain surface            
            if (IsEphysLinkControlled)
            {
                BrainSurfaceOffset -= Vector3.Distance(brainSurface, _probeController.Insertion.apmldv);
            }
            else
            {
                _probeController.SetProbePosition(brainSurface);
            }
        }
        else
        {
            // We need to calculate the surface coordinate ourselves
            var tipExtensionDirection =
                IsSetToDropToSurfaceWithDepth ? _probeController.GetTipWorldU().tipUpWorld : Vector3.up;

            var brainSurfaceCoordinate = annotationDataset.FindSurfaceCoordinate(
                annotationDataset.CoordinateSpace.World2Space(_probeController.GetTipWorldU().tipCoordWorld - tipExtensionDirection * 5),
                annotationDataset.CoordinateSpace.World2SpaceAxisChange(tipExtensionDirection));

            if (float.IsNaN(brainSurfaceCoordinate.x))
            {
                Debug.LogWarning("Could not find brain surface! Canceling set brain offset.");
                return;
            }

            var brainSurfaceToWorld = annotationDataset.CoordinateSpace.Space2World(brainSurfaceCoordinate);

            if (IsEphysLinkControlled)
            {
                var depth = Vector3.Distance(_probeController.Insertion.World2Transformed(brainSurfaceToWorld),
                    _probeController.Insertion.apmldv);
                BrainSurfaceOffset += depth;
                print("Depth: " + depth + " Total: " + BrainSurfaceOffset);
            }
            else
            {
                _probeController.SetProbePosition(_probeController.Insertion.World2Transformed(brainSurfaceToWorld));
            }
        }
        

    }

    /// <summary>
    ///     Manual adjustment of brain surface offset.
    /// </summary>
    /// <param name="increment">Amount to change the brain surface offset by</param>
    public void IncrementBrainSurfaceOffset(float increment)
    {
        BrainSurfaceOffset += increment;
    }

    /// <summary>
    ///     Set if the probe should be dropped to the surface with depth or with DV.
    /// </summary>
    /// <param name="dropToSurfaceWithDepth">Use depth if true, use DV if false</param>
    public void SetDropToSurfaceWithDepth(bool dropToSurfaceWithDepth)
    {
        // Only make changes to brain surface offset axis if the offset is 0
        if (!CanChangeBrainSurfaceOffsetAxis) return;
        
        // Apply change (if eligible)
        IsSetToDropToSurfaceWithDepth = dropToSurfaceWithDepth;
    }

    #endregion

    #region Actions

    /// <summary>
    ///     Echo given position in needles transform space to the probe.
    /// </summary>
    /// <param name="pos">Position of manipulator in needles transform</param>
    private void EchoPositionFromEphysLink(Vector4 pos)
    {
        // Quit early if the probe has been removed
        if (_probeController == null)
        {
            return;
        }
        // Apply zero coordinate offset
        var zeroCoordinateAdjustedManipulatorPosition = pos - ZeroCoordinateOffset;
        
        // Apply axis negations
        zeroCoordinateAdjustedManipulatorPosition.z *= -1;
        zeroCoordinateAdjustedManipulatorPosition.y *= tpmanager.IsManipulatorRightHanded(ManipulatorId) ? 1 : -1;

        // Phi adjustment
        var probePhi = -_probeController.Insertion.phi * Mathf.Deg2Rad;
        _phiCos = Mathf.Cos(probePhi);
        _phiSin = Mathf.Sin(probePhi);
        var phiAdjustedX = zeroCoordinateAdjustedManipulatorPosition.x * _phiCos -
                           zeroCoordinateAdjustedManipulatorPosition.y * _phiSin;
        var phiAdjustedY = zeroCoordinateAdjustedManipulatorPosition.x * _phiSin +
                           zeroCoordinateAdjustedManipulatorPosition.y * _phiCos;
        zeroCoordinateAdjustedManipulatorPosition.x = phiAdjustedX;
        zeroCoordinateAdjustedManipulatorPosition.y = phiAdjustedY;
        
        // Calculate last used direction (between depth and DV)
        var dvDelta = Math.Abs(zeroCoordinateAdjustedManipulatorPosition.z - _lastManipulatorPosition.z);
        var depthDelta = Math.Abs(zeroCoordinateAdjustedManipulatorPosition.w - _lastManipulatorPosition.w);
        if (dvDelta > 0.0001 || depthDelta > 0.0001) SetDropToSurfaceWithDepth(depthDelta >= dvDelta);
        _lastManipulatorPosition = zeroCoordinateAdjustedManipulatorPosition;
        
        // Brain surface adjustment
        var brainSurfaceAdjustment = float.IsNaN(BrainSurfaceOffset) ? 0 : BrainSurfaceOffset;
        if (IsSetToDropToSurfaceWithDepth)
            zeroCoordinateAdjustedManipulatorPosition.w += brainSurfaceAdjustment;
        else
            zeroCoordinateAdjustedManipulatorPosition.z -= brainSurfaceAdjustment;

        // Convert to world space
        var zeroCoordinateAdjustedWorldPosition = new Vector4(zeroCoordinateAdjustedManipulatorPosition.y,
            zeroCoordinateAdjustedManipulatorPosition.z, -zeroCoordinateAdjustedManipulatorPosition.x,
            zeroCoordinateAdjustedManipulatorPosition.w);

        // Set probe position (change axes to match probe)
        var zeroCoordinateApmldv = _probeController.Insertion.World2TransformedAxisChange(zeroCoordinateAdjustedWorldPosition);
        _probeController.SetProbePosition(new Vector4(zeroCoordinateApmldv.x, zeroCoordinateApmldv.y,
            zeroCoordinateApmldv.z, zeroCoordinateAdjustedWorldPosition.w));


        // Continue echoing position
        if (IsEphysLinkControlled)
            _ephysLinkCommunicationManager.GetPos(ManipulatorId, EchoPositionFromEphysLink);
    }

    #endregion

    #endregion

    #region AxisControl

    public void SetAxisVisibility(bool AP, bool ML, bool DV, bool depth)
    {
        Transform tipT = _probeController.ProbeTipT;
        tpmanager.SetAxisVisibility(AP, ML, DV, depth, tipT);
    }

    #endregion AxisControl

    #region Materials


    /// <summary>
    /// Set all Renderer components to use the ghost material
    /// </summary>
    public void SetMaterialsTransparent()
    {
        var currentColorTint = new Color(GetColor().r, GetColor().g, GetColor().b, .2f);
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
        foreach (var childRenderer in transform.GetComponentsInChildren<Renderer>())
            if (defaultMaterials.ContainsKey(childRenderer.gameObject))
                childRenderer.material = defaultMaterials[childRenderer.gameObject];
    }

    #endregion
}
