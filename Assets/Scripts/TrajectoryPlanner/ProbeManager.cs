using System;
using System.Collections.Generic;
using EphysLink;
using TMPro;
using TrajectoryPlanner;
using UnityEngine;

/// <summary>
/// 3D space control for Neuropixels probes in the Trajectory Planner scene
/// </summary>
public class ProbeManager : MonoBehaviour
{
    // Internal flags that track whether we are in manual control or drag/link control mode
    private bool _ephysLinkMovement;
    private string _uuid;
    public string UUID { get { return _uuid; } }

    #region Ephys Link

    private CommunicationManager _ephysLinkCommunicationManager;
    private int _manipulatorId;
    private float _phiCos = 1f;
    private float _phiSin;
    private Vector4 _zeroCoordinateOffset = Vector4.negativeInfinity;
    private float _brainSurfaceOffset;
    private bool _dropToSurfaceWithDepth = true;
    private Vector4 _lastManipulatorPosition = Vector4.negativeInfinity;
    private int _automaticMovementSpeed = 500; // Default to 500 um/s
    private ProbeManager _ghostProbeManager;
    private ProbeManager _originalProbeManager;

    #endregion

    // Exposed fields to collect links to other components inside of the Probe prefab
    [SerializeField] private List<Collider> probeColliders;
    [SerializeField] private List<ProbeUIManager> probeUIManagers;
    [SerializeField] private Renderer probeRenderer;
    [SerializeField] private int _probeType;
    public int ProbeType { get { return _probeType; } }

    [SerializeField] private ProbeController probeController;

    [SerializeField] private Material ghostMaterial;
    private Dictionary<GameObject, Material> defaultMaterials;

    private TrajectoryPlannerManager tpmanager;

    // Text
    private int probeID;
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


    public int GetID()
    {
        return probeID;
    }

    public Color GetColor()
    {
        return probeRenderer.material.color;
    }
    public List<Collider> GetProbeColliders()
    {
        return probeColliders;
    }

    public void DisableAllColliders()
    {
        foreach (var probeCollider in probeColliders)
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
        return probeController;
    }

    /// <summary>
    /// Return the probe panel UI managers
    /// </summary>
    /// <returns>list of probe panel UI managers</returns>
    public List<ProbeUIManager> GetProbeUIManagers()
    {
        return probeUIManagers;
    }

    #endregion

    #region Unity

    private void Awake()
    {
        _uuid = Guid.NewGuid().ToString();

        defaultMaterials = new Dictionary<GameObject, Material>();

        // Pull the tpmanager object and register this probe
        GameObject main = GameObject.Find("main");
        tpmanager = main.GetComponent<TrajectoryPlannerManager>();
        tpmanager.RegisterProbe(this);
        probeController.Register(tpmanager, this);

        // Pull ephys link communication manager
        _ephysLinkCommunicationManager = GameObject.Find("EphysLink").GetComponent<CommunicationManager>();

        // Get access to the annotation dataset and world-space boundaries
        annotationDataset = tpmanager.GetAnnotationDataset();

        visibleProbeColliders = new List<GameObject>();
        visibleOtherColliders = new Dictionary<GameObject, Material>();
    }

    private void Start()
    {
        Debug.Log(string.Format("New probe created with UUID: {0}", _uuid));
        // Request for ID and color if this is a normal probe
        if (_originalProbeManager == null)
        {
            probeID = tpmanager.GetNextProbeId();
            name = "PROBE_" + probeID;
            probeRenderer.material.color = tpmanager.GetNextProbeColor();
            
            // Record default materials
            foreach (var childRenderer in transform.GetComponentsInChildren<Renderer>())
            {
                defaultMaterials.Add(childRenderer.gameObject, childRenderer.material);
            }
        }
        else
        {
            // Otherwise, generate info based on the original probe
            probeID = _originalProbeManager.GetID();
            name = "GHOST_PROBE_" + probeID;
        }

        foreach (var probeUIManager in probeUIManagers)
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
        foreach (ProbeUIManager puimanager in probeUIManagers)
            puimanager.Destroy();
        
        // Unregister this probe from the ephys link
        if (IsConnectedToManipulator())
        {
            SetEphysLinkMovement(false);
        }
    }

    #endregion

    /// <summary>
    /// Called by the TPManager when this Probe becomes the active probe
    /// </summary>
    public void SetActive()
    {
        if (probeController.Insertion.CoordinateTransform != tpmanager.GetActiveCoordinateTransform())
        {
            TP_QuestionDialogue questionDialogue = tpmanager.GetQuestionDialogue();
            questionDialogue.SetYesCallback(ChangeTransform);
            questionDialogue.NewQuestion("The coordinate transform in the scene is mis-matched with the transform in this Probe insertion. Do you want to replace the transform?");
        }
    }

    private void ChangeTransform()
    {
        ProbeInsertion originalInsertion = probeController.Insertion;
        Debug.LogWarning("Insertion coordinates are not being transformed!! This might not be expected behavior");
        probeController.SetProbePosition(new ProbeInsertion(originalInsertion.apmldv, originalInsertion.angles, tpmanager.GetCoordinateSpace(), tpmanager.GetActiveCoordinateTransform()));
    }

    public void UpdateUI()
    {
        // Reset our probe UI panels
        foreach (ProbeUIManager puimanager in probeUIManagers)
            puimanager.ProbeMoved();
    }

    public void SetUIVisibility(bool state)
    {
        foreach (ProbeUIManager puimanager in probeUIManagers)
            puimanager.SetProbePanelVisibility(state);
    }


    public void OverrideUUID(string newUUID)
    {
#if UNITY_EDITOR
        Debug.Log(string.Format("Override UUID to {0}", newUUID));
#endif
        _uuid = newUUID;
    }

    /// <summary>
    /// Update the size of the recording region.
    /// </summary>
    /// <param name="newSize">New size of recording region in mm</param>
    public void ChangeRecordingRegionSize(float newSize)
    {
        ((DefaultProbeController)probeController).ChangeRecordingRegionSize(newSize);

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
        if (_ephysLinkMovement)
            return false;

        return ((DefaultProbeController)probeController).MoveProbe_Keyboard(checkForCollisions);
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
        foreach (Collider activeCollider in probeColliders)
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

        ProbeInsertion insertion = probeController.Insertion;
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
            probeID, 
            apStr, round0(entryCoordTranformed.x * mult), mlStr, round0(entryCoordTranformed.y * mult), dvStr, round0(entryCoordTranformed.z * mult), 
            round2(Utils.CircDeg(angles.x, minPhi, maxPhi)), round2(angles.y), round2(Utils.CircDeg(angles.z, minSpin, maxSpin)),
            depthStr, round0(depthTransformed * mult),
            round0(apmldvS.x * mult), round0(apmldvS.y * mult), round0(apmldvS.z * mult));

        GUIUtility.systemCopyBuffer = updateStr;
    }

    #endregion


    public void RegisterProbeCallback(int ID, Color probeColor)
    {
        probeID = ID;
        name = "PROBE_" + probeID;
        probeRenderer.material.color = probeColor;
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
        foreach (ProbeUIManager puimanager in probeUIManagers)
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
        (Vector3 tipCoordWorld, Vector3 tipUpWorld, _) = probeController.GetTipWorldU();

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
            brainSurface = probeController.Insertion.World2Transformed(brainSurfaceWorld);
        }
    }


    public (Vector3 surfaceCoordinateT, float depthT) GetSurfaceCoordinateT()
    {
        return (brainSurface, Vector3.Distance(probeController.Insertion.apmldv, brainSurface));
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

    #region Property Getters and Setters

    /// <summary>
    /// Return if this probe is being controlled by the Ephys Link.
    /// </summary>
    /// <returns>True if movement is controlled by Ephys Link, False otherwise</returns>
    public bool GetEphysLinkMovement()
    {
        return _ephysLinkMovement;
    }

    /// <summary>
    /// (un)Register a probe and begin echoing position.
    /// </summary>
    /// <param name="register">To register or deregister this probe</param>
    /// <param name="manipulatorId">ID of the manipulator in real life to connect to</param>
    /// <param name="calibrated">Is the manipulator in real life calibrated</param>
    /// <param name="onSuccess">Callback function to handle a successful registration</param>
    /// <param name="onError">Callback function to handle a failed registration</param>
    public void SetEphysLinkMovement(bool register, int manipulatorId = 0, bool calibrated = true,
        Action onSuccess = null, Action<string> onError = null)
    {
        // Exit early if this was an invalid call
        switch (register)
        {
            case true when IsConnectedToManipulator():
            case true when manipulatorId == 0:
                return;
        }

        // Set states
        _ephysLinkMovement = register;
        tpmanager.UpdateQuickSettings();

        if (register)
            _ephysLinkCommunicationManager.RegisterManipulator(manipulatorId, () =>
            {
                Debug.Log("Manipulator Registered");
                _manipulatorId = manipulatorId;

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
                                        _ => onSuccess?.Invoke());
                                });
                        });

                onSuccess?.Invoke();
            }, err => onError?.Invoke(err));
        else
            _ephysLinkCommunicationManager.UnregisterManipulator(_manipulatorId, () =>
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
                if (_zeroCoordinateOffset.Equals(Vector4.negativeInfinity)) _zeroCoordinateOffset = vector4;
                EchoPositionFromEphysLink(vector4);
            });
        }
    }

    /// <summary>
    ///     Get attached manipulator ID.
    /// </summary>
    /// <returns>Attached manipulator ID, 0 if none are attached</returns>
    public int GetManipulatorId()
    {
        return _manipulatorId;
    }

    /// <summary>
    ///     Set a reference to the probe manager of the ghost probe for moving the manipulator tied to this probe.
    /// </summary>
    /// <param name="probeManager">probe manager of the ghost probe for this probe</param>
    public void SetGhostProbeManager(ProbeManager probeManager)
    {
        _ghostProbeManager = probeManager;
    }
    
    /// <summary>
    /// Return ghost probe's probe manager
    /// </summary>
    /// <returns>Ghost probe's probe manager</returns>
    public ProbeManager GetGhostProbeManager()
    {
        return _ghostProbeManager;
    }

    /// <summary>
    /// For a ghost probe, set the original probe's probe manager
    /// </summary>
    /// <param name="probeManager">Original probe's probe manager</param>
    public void SetOriginalProbeManager(ProbeManager probeManager)
    {
        _originalProbeManager = probeManager;
    }
    
    /// <summary>
    /// Get the original probe's probe manager
    /// </summary>
    /// <returns>Original probe's probe manager</returns>
    public ProbeManager GetOriginalProbeManager()
    {
        return _originalProbeManager;
    }

    /// <summary>
    ///     Set manipulator properties such as ID and positional offsets back to defaults.
    /// </summary>
    public void ResetManipulatorProperties()
    {
        _manipulatorId = 0;
        _zeroCoordinateOffset = Vector4.negativeInfinity;
        _brainSurfaceOffset = 0;
    }

    /// <summary>
    ///     Return if this probe is being controlled by the Ephys Link.
    /// </summary>
    /// <returns>True if this probe is attached to a manipulator, false otherwise</returns>
    public bool IsConnectedToManipulator()
    {
        return _manipulatorId != 0;
    }

    /// <summary>
    ///     Return if this probe is a ghost of another probe
    /// </summary>
    /// <returns>True if this probe is a ghost, false otherwise</returns>
    public bool IsGhost()
    {
        return _originalProbeManager != null;
    }

    /// <summary>
    ///     Return if this probe has a ghost attached to it
    /// </summary>
    /// <returns>True if this probe has a ghost, false otherwise</returns>
    public bool HasGhost()
    {
        return _ghostProbeManager != null;
    }

    /// <summary>
    /// Set the automatic movement speed of this probe (when put under automatic control)
    /// </summary>
    /// <param name="speed">Speed in um/s</param>
    public void SetAutomaticMovementSpeed(int speed)
    {
        // Ghosts don't have automatic movement speeds
        if (IsGhost())
        {
            return;
        }
        _automaticMovementSpeed = speed;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public int GetAutomaticMovementSpeed()
    {
        return _automaticMovementSpeed;
    }

    /// <summary>
    ///     Manipulator space offset to zero coordinate as X, Y, Z, Depth.
    /// </summary>
    /// <returns>Manipulator space offset to zero coordinate as X, Y, Z, Depth</returns>
    public Vector4 GetZeroCoordinateOffset()
    {
        return _zeroCoordinateOffset;
    }
    
    /// <summary>
    ///     Set manipulator space offset to zero coordinate as X, Y, Z, Depth.
    /// </summary>
    /// <param name="zeroCoordinateOffset">Offset from zero coordinate as X, Y, Z, Depth</param>
    public void SetZeroCoordinateOffset(Vector4 zeroCoordinateOffset)
    {
        _zeroCoordinateOffset = zeroCoordinateOffset;
    }

    /// <summary>
    ///     Update x coordinate of manipulator space offset to zero coordinate.
    /// </summary>
    /// <param name="x">X coordinate</param>
    public void SetZeroCoordinateOffsetX(float x)
    {
        _zeroCoordinateOffset.x = x;
    }

    /// <summary>
    ///     Update y coordinate of manipulator space offset to zero coordinate.
    /// </summary>
    /// <param name="y">Y coordinate</param>
    public void SetZeroCoordinateOffsetY(float y)
    {
        _zeroCoordinateOffset.y = y;
    }


    /// <summary>
    ///     Update Z coordinate of manipulator space offset to zero coordinate.
    /// </summary>
    /// <param name="z">Z coordinate</param>
    public void SetZeroCoordinateOffsetZ(float z)
    {
        _zeroCoordinateOffset.z = z;
    }


    /// <summary>
    ///     Update D coordinate of manipulator space offset to zero coordinate.
    /// </summary>
    /// <param name="depth">D coordinate</param>
    public void SetZeroCoordinateOffsetDepth(float depth)
    {
        _zeroCoordinateOffset.w = depth;
    }

    /// <summary>
    /// Get manipulator space offset from brain surface as Depth.
    /// </summary>
    /// <returns>Manipulator space offset to brain surface</returns>
    public float GetBrainSurfaceOffset()
    {
        return _brainSurfaceOffset;
    }

    /// <summary>
    /// Check if brain surface offset axis (Depth vs DV) is allowed to be changed
    /// </summary>
    /// <returns>True if the axis is allowed to be changed, false otherwise</returns>
    public bool CanChangeBrainSurfaceOffsetAxis()
    {
        return _brainSurfaceOffset == 0;
    }
    
    /// <summary>
    ///     Set manipulator space offset from brain surface as Depth from input.
    /// </summary>
    /// <param name="brainSurfaceOffset">Offset from brain surface as Depth</param>
    public void SetBrainSurfaceOffset(float brainSurfaceOffset)
    {
        _brainSurfaceOffset = brainSurfaceOffset;
    }

    /// <summary>
    ///     Set manipulator space offset from brain surface as Depth from manipulator or probe coordinates.
    /// </summary>
    public void SetBrainSurfaceOffset()
    {
        if (probeInBrain)
        {
            // Just calculate the distance from the probe tip position to the brain surface            
            if (IsConnectedToManipulator())
            {
                _brainSurfaceOffset -= Vector3.Distance(brainSurface, probeController.Insertion.apmldv);
            }
            else
            {
                probeController.SetProbePosition(brainSurface);
            }
        }
        else
        {
            // We need to calculate the surface coordinate ourselves
            var tipExtensionDirection = _dropToSurfaceWithDepth ? probeController.GetTipWorldU().tipUpWorld : Vector3.up;

            var brainSurfaceAPDVLR = annotationDataset.FindSurfaceCoordinate(
                annotationDataset.CoordinateSpace.World2Space(probeController.GetTipWorldU().tipCoordWorld - tipExtensionDirection * 5),
                annotationDataset.CoordinateSpace.World2SpaceAxisChange(tipExtensionDirection));

            if (float.IsNaN(brainSurfaceAPDVLR.x))
            {
                Debug.LogWarning("Could not find brain surface! Canceling set brain offset.");
                return;
            }

            var brainSurfaceToWorld = annotationDataset.CoordinateSpace.Space2World(brainSurfaceAPDVLR);

            if (IsConnectedToManipulator())
            {
                var depth = Vector3.Distance(probeController.Insertion.World2Transformed(brainSurfaceToWorld),
                    probeController.Insertion.apmldv);
                _brainSurfaceOffset += depth;
                print("Depth: " + depth + " Total: " + _brainSurfaceOffset);
            }
            else
            {
                probeController.SetProbePosition(probeController.Insertion.World2Transformed(brainSurfaceToWorld));
            }
        }
        

    }

    /// <summary>
    ///     Manual adjustment of brain surface offset.
    /// </summary>
    /// <param name="increment">Amount to change the brain surface offset by</param>
    public void IncrementBrainSurfaceOffset(float increment)
    {
        _brainSurfaceOffset += increment;
    }

    /// <summary>
    ///     Set if the probe should be dropped to the surface with depth or with DV.
    /// </summary>
    /// <param name="dropToSurfaceWithDepth">Use depth if true, use DV if false</param>
    public void SetDropToSurfaceWithDepth(bool dropToSurfaceWithDepth)
    {
        // Only make changes to brain surface offset axis if the offset is 0
        if (!CanChangeBrainSurfaceOffsetAxis()) return;
        
        // Apply change (if eligible)
        _dropToSurfaceWithDepth = dropToSurfaceWithDepth;
    }

    /// <summary>
    ///     Return if this probe is currently set to drop to the surface using depth.
    /// </summary>
    /// <returns>True if dropping to surface via depth, false if using DV</returns>
    public bool IsSetToDropToSurfaceWithDepth()
    {
        return _dropToSurfaceWithDepth;
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
        if (probeController == null)
        {
            return;
        }
        // Apply zero coordinate offset
        var zeroCoordinateAdjustedManipulatorPosition = pos - _zeroCoordinateOffset;
        
        // Apply axis negations
        zeroCoordinateAdjustedManipulatorPosition.z *= -1;
        zeroCoordinateAdjustedManipulatorPosition.y *= tpmanager.IsManipulatorRightHanded(_manipulatorId) ? 1 : -1;

        // Phi adjustment
        var probePhi = -probeController.Insertion.phi * Mathf.Deg2Rad;
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
        var brainSurfaceAdjustment = float.IsNaN(_brainSurfaceOffset) ? 0 : _brainSurfaceOffset;
        if (_dropToSurfaceWithDepth)
            zeroCoordinateAdjustedManipulatorPosition.w += brainSurfaceAdjustment;
        else
            zeroCoordinateAdjustedManipulatorPosition.z -= brainSurfaceAdjustment;

        // Convert to world space
        var zeroCoordinateAdjustedWorldPosition = new Vector4(zeroCoordinateAdjustedManipulatorPosition.y,
            zeroCoordinateAdjustedManipulatorPosition.z, -zeroCoordinateAdjustedManipulatorPosition.x,
            zeroCoordinateAdjustedManipulatorPosition.w);

        // Set probe position (change axes to match probe)
        var zeroCoordinateApmldv = probeController.Insertion.World2TransformedAxisChange(zeroCoordinateAdjustedWorldPosition);
        probeController.SetProbePosition(new Vector4(zeroCoordinateApmldv.x, zeroCoordinateApmldv.y,
            zeroCoordinateApmldv.z, zeroCoordinateAdjustedWorldPosition.w));


        // Continue echoing position
        if (_ephysLinkMovement)
            _ephysLinkCommunicationManager.GetPos(_manipulatorId, EchoPositionFromEphysLink);
    }

    #endregion

    #endregion

    #region AxisControl

    public void SetAxisVisibility(bool AP, bool ML, bool DV, bool depth)
    {
        Transform tipT = probeController.ProbeTipT;
        tpmanager.SetAxisVisibility(AP, ML, DV, depth, tipT);
    }

    #endregion AxisControl

    #region Materials


    /// <summary>
    /// Set all Renderer components to use the ghost material
    /// </summary>
    public void SetMaterialsTransparent()
    {
        print("Set materials to transparent");
        var currentColorTint = new Color(GetColor().r, GetColor().g, GetColor().b, .2f);
        foreach (Renderer renderer in transform.GetComponentsInChildren<Renderer>())
        {
            renderer.material = ghostMaterial;

            // Tint transparent material for non-ghost probes
            if (!IsGhost()) renderer.material.color = currentColorTint;
        }
    }

    /// <summary>
    /// Reverse a previous call to SetMaterialsTransparent()
    /// </summary>
    public void SetMaterialsDefault()
    {
        print("Set materials to default");
        foreach (Renderer renderer in transform.GetComponentsInChildren<Renderer>())
            if (defaultMaterials.ContainsKey(renderer.gameObject))
                renderer.material = defaultMaterials[renderer.gameObject];
    }

    #endregion
}
