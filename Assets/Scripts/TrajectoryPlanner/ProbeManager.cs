using System;
using System.Collections;
using System.Collections.Generic;
using SensapexLink;
using TMPro;
using TrajectoryPlanner;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3D space control for Neuropixels probes in the Trajectory Planner scene
/// </summary>
public class ProbeManager : MonoBehaviour
{
    #region Movement Constants
    private const float REC_HEIGHT_SPEED = 0.1f;
    private const float MOVE_INCREMENT_TAP = 0.010f; // move 1 um per tap
    private const float MOVE_INCREMENT_TAP_FAST = 0.100f;
    private const float MOVE_INCREMENT_TAP_SLOW = 0.001f;
    private const float MOVE_INCREMENT_HOLD = 0.100f; // move 50 um per second when holding
    private const float MOVE_INCREMENT_HOLD_FAST = 1.000f;
    private const float MOVE_INCREMENT_HOLD_SLOW = 0.010f;
    private const float ROT_INCREMENT_TAP = 1f;
    private const float ROT_INCREMENT_TAP_FAST = 10f;
    private const float ROT_INCREMENT_TAP_SLOW = 0.1f;
    private const float ROT_INCREMENT_HOLD = 5f;
    private const float ROT_INCREMENT_HOLD_FAST = 25;
    private const float ROT_INCREMENT_HOLD_SLOW = 2.5f;
    #endregion

    // Internal flags that track when keys are held down
    private bool keyFast = false;
    private bool keySlow = false;
    private bool keyHeld = false; // If a key is held, we will skip re-checking the key hold delay for any other keys that are added
    private float keyPressTime = 0f;
    [SerializeField] private float keyHoldDelay = 0.300f;

    // Internal flags that track whether we are in manual control or drag/link control mode
    private bool draggingMovement = false;
    private bool _sensapexLinkMovement;

    #region Sensapex Link

    private CommunicationManager _sensapexLinkCommunicationManager;
    private readonly NeedlesTransform _neTransform = new NeedlesTransform();
    private int _manipulatorId;
    private Vector3 _probeAngles;
    private float _phiCos = 1;
    private float _phiSin;
    private Vector4 _bregmaOffset = Vector4.negativeInfinity;
    private float _brainSurfaceOffset;

    #endregion

    // Exposed fields to collect links to other components inside of the Probe prefab
    [SerializeField] private List<Collider> probeColliders;
    [SerializeField] private List<ProbeUIManager> probeUIManagers;
    [SerializeField] private Transform rotateAround;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private Renderer probeRenderer;
    [SerializeField] private List<GameObject> recordingRegionGOs;
    [SerializeField] private int probeType;
    [SerializeField] private Transform probeTipT;

    [SerializeField] private Material ghostMaterial;
    private Dictionary<GameObject, Material> defaultMaterials;

    private TrajectoryPlannerManager tpmanager;

    // in ap/ml/dv
    private Vector3 defaultStart = new Vector3(5.4f, 5.7f, 0.332f);
    private float defaultDepth = 0f;
    private Vector2 defaultAngles = new Vector2(-90f, 0f); // 0 phi is forward, default theta is 90 degrees down from horizontal, but internally this is a value of 0f

    // Text
    private int probeID;
    private TextMeshProUGUI textUI;

    // Probe positioning information
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // total position data (for dealing with coordinates)
    private ProbeInsertion insertion;
    private float minPhi = -180;
    private float maxPhi = 180f;
    private float minTheta = -90f;
    private float maxTheta = 0f;
    private float minSpin = -180f;
    private float maxSpin = 180f;

    // Offset vectors
    private GameObject probeTipOffset;
    private GameObject probeEndOffset;

    // recording region
    private float minRecordHeight;
    private float maxRecordHeight; // get this by measuring the height of the recording rectangle and subtracting from 10
    private float recordingRegionSizeY;

    // Brain surface position
    private AnnotationDataset annotationDataset;
    private bool probeInBrain;
    private Vector3 brainSurfaceWorld;

    // Colliders
    private List<GameObject> visibleProbeColliders;
    private Dictionary<GameObject, Material> visibleOtherColliders;

    // Text button
    GameObject textGO;
    Button textButton;

    #region Accessors

    /// <summary>
    /// Get the probe-type of this probe
    /// </summary>
    /// <returns>probe type</returns>
    public int GetProbeType()
    {
        return probeType;
    }

    /// <summary>
    /// Get the tip transform
    /// </summary>
    /// <returns>tip transform</returns>
    public Transform GetTipTransform()
    {
        return probeTipT;
    }
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
        // Setup some basic variables
        textGO = Instantiate(textPrefab, GameObject.Find("CoordinatePanel").transform);
        textButton = textGO.GetComponent<Button>();
        textButton.onClick.AddListener(Probe2Text);
        textUI = textGO.GetComponent<TextMeshProUGUI>();
        defaultMaterials = new Dictionary<GameObject, Material>();

        // Pull the tpmanager object and register this probe
        GameObject main = GameObject.Find("main");
        tpmanager = main.GetComponent<TrajectoryPlannerManager>();
        tpmanager.RegisterProbe(this);
        
        // Pull sensapex link communication manager
        _sensapexLinkCommunicationManager = GameObject.Find("SensapexLink").GetComponent<CommunicationManager>();

        // Get access to the annotation dataset and world-space boundaries
        annotationDataset = tpmanager.GetAnnotationDataset();

        visibleProbeColliders = new List<GameObject>();
        visibleOtherColliders = new Dictionary<GameObject, Material>();

        // Move the recording region to the base of the probe
        UpdateRecordingRegionVars();

        // Create two points offset from the tip that we'll use to interpolate where we are on the probe
        probeTipOffset = new GameObject(name + "TipOffset");
        probeTipOffset.transform.position = probeTipT.position + probeTipT.up * 0.2f;
        probeTipOffset.transform.parent = probeTipT;
        probeEndOffset = new GameObject(name + "EndOffset");
        probeEndOffset.transform.position = probeTipT.position + probeTipT.up * 10.2f;
        probeEndOffset.transform.parent = probeTipT;
    }

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        ResetPosition();

        // Reset our probe UI panels
        foreach (ProbeUIManager puimanager in probeUIManagers)
            puimanager.ProbeMoved();
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
        Destroy(textGO);
        
        // Unregister this probe from the sensapex link
        if (IsConnectedToManipulator())
        {
            SetSensapexLinkMovement(false, 0);
        }
    }

    #endregion



    /// <summary>
    /// Update the size of the recording region.
    /// </summary>
    /// <param name="newSize">New size of recording region in mm</param>
    public void ChangeRecordingRegionSize(float newSize)
    {
        recordingRegionSizeY = newSize;

        foreach (GameObject go in recordingRegionGOs)
        {
            // This is a little complicated if we want to do it right (since you can accidentally scale the recording region off the probe.
            // For now, we will just reset the y position to be back at the bottom of the probe.
            Vector3 scale = go.transform.localScale;
            scale.y = newSize;
            go.transform.localScale = scale;
            Vector3 pos = go.transform.localPosition;
            pos.y = newSize / 2f + 0.2f;
            go.transform.localPosition = pos;
        }
        // Update all the UI panels
        UpdateRecordingRegionVars();
        foreach (ProbeUIManager puimanager in probeUIManagers)
            puimanager.ProbeMoved();
    }

    /// <summary>
    /// Put this probe back at Bregma
    /// </summary>
    public void ResetPosition()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // reset UI as well
        ResetPositionTracking();
        SetProbePosition();
        UpdateText();
    }

    /// <summary>
    /// Helper function to reset all of the position data, without updating any UI elements
    /// </summary>
    private void ResetPositionTracking()
    {
        insertion = new ProbeInsertion(defaultStart, defaultAngles);
    }

    private void CheckForSpeedKeys()
    {
        keyFast = Input.GetKey(KeyCode.LeftShift);
        keySlow = Input.GetKey(KeyCode.LeftControl);
    }

    /// <summary>
    /// Move the probe with the option to check for collisions
    /// </summary>
    /// <param name="checkForCollisions">Set to true to check for collisions with rig colliders and other probes</param>
    /// <returns>Whether or not the probe moved on this frame</returns>
    public bool MoveProbe(bool checkForCollisions = false)
    {
        // Cancel movement if being controlled by SensapexLink
        if (_sensapexLinkMovement)
            return false;
        
        bool moved = false;
        bool keyHoldDelayPassed = (Time.realtimeSinceStartup - keyPressTime) > keyHoldDelay;

        CheckForSpeedKeys();
        // Handle click inputs

        // A note about key presses. In Unity on most computers with high frame rates pressing a key *once* will trigger:
        // Frame 0: KeyDown and Key
        // Frame 1: Key
        // Frame 2...N-1 : Key
        // Frame N: Key and KeyUp
        // On *really* fast computers you might get multiple frames with Key before you see the KeyUp event. This is... a pain, if we want to be able to do both smooth motion and single key taps.
        // We handle this by having a minimum "hold" time of say 50 ms before we start paying attention to the Key events

        // [TODO] There's probably a smart refactor to be done here so that key press/hold is functionally separate from calling the Move() functions
        // probably need to store the held KeyCodes in a list or something? 

        // APML movements
        if (Input.GetKeyDown(KeyCode.W))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            MoveProbeAPML(-1f, 0f, true);
        }
        else if (Input.GetKey(KeyCode.W) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeAPML(-1f, 0f, false);
        }
        if (Input.GetKeyUp(KeyCode.W))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.S))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            MoveProbeAPML(1f, 0f, true);
        }
        else if (Input.GetKey(KeyCode.S) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeAPML(1f, 0f, false);
        }
        if (Input.GetKeyUp(KeyCode.S))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.D))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            MoveProbeAPML(0f, 1f, true);
        }
        else if (Input.GetKey(KeyCode.D) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeAPML(0f, 1f, false);
        }
        if (Input.GetKeyUp(KeyCode.D))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.A))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            MoveProbeAPML(0f, -1f, true);
        }
        else if (Input.GetKey(KeyCode.A) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeAPML(0f, -1f, false);
        }
        if (Input.GetKeyUp(KeyCode.A))
            keyHeld = false;

        // DV movement

        if (Input.GetKeyDown(KeyCode.Z))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            MoveProbeDV(1f, true);
        }
        else if (Input.GetKey(KeyCode.Z) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeDV(1f, false);
        }
        if (Input.GetKeyUp(KeyCode.Z))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.X))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            MoveProbeDV(-1f, true);
        }
        else if (Input.GetKey(KeyCode.X) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeDV(-1f, false);
        }
        if (Input.GetKeyUp(KeyCode.X))
            keyHeld = false;

        // Depth movement

        //if (Input.GetKeyDown(KeyCode.Z))
        //{
        //    moved = true;
        //    keyPressTime = Time.realtimeSinceStartup;
        //    MoveProbeDepth(1f, true);
        //}
        //else if (Input.GetKey(KeyCode.Z) && (keyHeld || keyHoldDelayPassed))
        //{
        //    keyHeld = true;
        //    moved = true;
        //    MoveProbeDepth(1f, false);
        //}
        //if (Input.GetKeyUp(KeyCode.Z))
        //    keyHeld = false;

        //if (Input.GetKeyDown(KeyCode.X))
        //{
        //    moved = true;
        //    keyPressTime = Time.realtimeSinceStartup;
        //    MoveProbeDepth(-1f, true);
        //}
        //else if (Input.GetKey(KeyCode.X) && (keyHeld || keyHoldDelayPassed))
        //{
        //    keyHeld = true;
        //    moved = true;
        //    MoveProbeDepth(-1f, false);
        //}
        //if (Input.GetKeyUp(KeyCode.X))
        //    keyHeld = false;

        // Rotations

        if (Input.GetKeyDown(KeyCode.Q))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            RotateProbe(-1f, 0f, true);
        }
        else if (Input.GetKey(KeyCode.Q) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            RotateProbe(-1f, 0f, false);
        }
        if (Input.GetKeyUp(KeyCode.Q))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.E))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            RotateProbe(1f, 0f, true);
        }
        else if (Input.GetKey(KeyCode.E) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            RotateProbe(1f, 0f, false);
        }
        if (Input.GetKeyUp(KeyCode.E))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.R))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            RotateProbe(0f, 1f, true);
        }
        else if (Input.GetKey(KeyCode.R) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            RotateProbe(0f, 1f, false);
        }
        if (Input.GetKeyUp(KeyCode.R))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.F))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            RotateProbe(0f, -1f, true);
        }
        else if (Input.GetKey(KeyCode.F) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            RotateProbe(0f, -1f, false);
        }
        if (Input.GetKeyUp(KeyCode.F))
            keyHeld = false;

        // Spin controls
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            SpinProbe(-1f, true);
        }
        else if (Input.GetKey(KeyCode.Alpha1) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            SpinProbe(-1f, false);
        }
        if (Input.GetKeyUp(KeyCode.Alpha1))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            SpinProbe(1f, true);
        }
        else if (Input.GetKey(KeyCode.Alpha3) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            SpinProbe(1f, false);
        }
        if (Input.GetKeyUp(KeyCode.Alpha3))
            keyHeld = false;

        // Recording region controls
        if (Input.GetKey(KeyCode.T))
        {
            moved = true;
            ShiftRecordingRegion(1f);
        }
        if (Input.GetKey(KeyCode.G))
        {
            moved = true;
            ShiftRecordingRegion(-1f);
        }


        if (moved)
        {
            // If the probe was moved, set the new position
            SetProbePosition();

            // Check collisions if we need to
            if (checkForCollisions)
                CheckCollisions(tpmanager.GetAllNonActiveColliders());

            // Update all the UI panels
            foreach (ProbeUIManager puimanager in probeUIManagers)
                puimanager.ProbeMoved();

            return true;
        }
        else
        {
            return false;
        }
    }

    // Recording region controls

    /// <summary>
    /// Update the position of the bottom and top of the recording region
    /// </summary>
    private void UpdateRecordingRegionVars()
    {
        minRecordHeight = recordingRegionGOs[0].transform.localPosition.y;
        maxRecordHeight = minRecordHeight + (10 - recordingRegionGOs[0].transform.localScale.y);
    }

    /// <summary>
    /// Return the current size of the recording region
    /// </summary>
    /// <returns>size of the recording region</returns>
    public float GetRecordingRegionSize()
    {
        return recordingRegionSizeY;
    }

    /// <summary>
    /// Move the recording region up or down
    /// </summary>
    /// <param name="dir">-1 or 1 to indicate direction</param>
    private void ShiftRecordingRegion(float dir)
    {
        // Loop over recording regions to handle 4-shank (and 8-shank) probes
        foreach (GameObject recordingRegion in recordingRegionGOs)
        {
            Vector3 localPosition = recordingRegion.transform.localPosition;
            float localRecordHeightSpeed = Input.GetKey(KeyCode.LeftShift) ? REC_HEIGHT_SPEED * 2 : REC_HEIGHT_SPEED;
            localPosition.y = Mathf.Clamp(localPosition.y + dir * localRecordHeightSpeed, minRecordHeight, maxRecordHeight);
            recordingRegion.transform.localPosition = localPosition;
        }
    }

    /// <summary>
    /// Return the mm position of the bottom of the recording region and the height
    /// </summary>
    /// <returns>float array [0]=bottom, [1]=height</returns>
    public float[] GetRecordingRegionHeight()
    {
        float[] heightPercs = new float[2];
        heightPercs[0] = (recordingRegionGOs[0].transform.localPosition.y - minRecordHeight) / (maxRecordHeight - minRecordHeight);
        heightPercs[1] = recordingRegionSizeY;
        return heightPercs;
    }

    // MANUAL COORDINATE ENTRY + PROBE POSITION CONTROLS

    /// <summary>
    /// Set the coordinates of the probe by hand, this function assumes the input is in the current transformation and uses the IBL-relative setting
    /// 
    /// In other words: you have to input coordinates that are *matched* to the current output coordinates. Note that depth is currently ignored.
    /// </summary>
    public void ManualCoordinateEntryTransformed(float ap, float ml, float dv, float phi, float theta, float spin)
    {
        // Unconvert back to CCF space
        SetProbePositionTransformed(ap, ml, dv, phi, theta, spin);

        // Tell the tpmanager we moved and update the UI elements
        tpmanager.SetMovedThisFrame();
        foreach (ProbeUIManager puimanager in probeUIManagers)
            puimanager.ProbeMoved();
        tpmanager.UpdateInPlaneView();
    }

    /// <summary>
    ///     ManualCoordinateEntryTransformed but with vector inputs
    /// </summary>
    /// <param name="apmldv">Vector3 representation of AP, ML, DV positions</param>
    /// <param name="angles">Vector3 represetnation of probe angles</param>
    public void ManualCoordinateEntryTransformed(Vector3 apmldv, Vector3 angles)
    {
        ManualCoordinateEntryTransformed(apmldv.x, apmldv.y, apmldv.z, angles.x, angles.y, angles.z);
    }

    public void ManualCoordinateEntrySurfaceTransformed(float ap, float ml, float dv, float phi, float theta, float spin)
    {
        Debug.LogError("Not implemented");
    }

    public IEnumerator DelayedManualCoordinateEntryTransformed(float delay, float ap, float ml, float dv, float phi, float theta, float spin)
    {
        yield return new WaitForSeconds(delay);
        ManualCoordinateEntryTransformed(ap, ml, dv, phi, theta, spin);
    }

    /// <summary>
    /// Set the probe position to the current apml/depth/phi/theta/spin values
    /// </summary>
    public void SetProbePosition()
    {
        SetProbePositionCCF(insertion);

        // Check where we are relative to the surface of the brain
        UpdateSurfacePosition();
        // Update probe text
        UpdateText();
    }

    /// <summary>
    /// Set the position of the probe to match a ProbeInsertion object in CCF coordinates
    /// </summary>
    /// <param name="localInsertion">new insertion position</param>
    public void SetProbePositionCCF(ProbeInsertion localInsertion)
    {
        // Reset everything
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        ResetPositionTracking();

        // Manually adjust the coordinates and rotation
        transform.position += Utils.apmldv2world(localInsertion.apmldv);
        transform.RotateAround(rotateAround.position, transform.up, localInsertion.phi);
        transform.RotateAround(rotateAround.position, transform.forward, localInsertion.theta);
        transform.RotateAround(rotateAround.position, rotateAround.up, localInsertion.spin);

        // save the data
        insertion.SetCoordinates(localInsertion);
    }

    /// <summary>
    /// Set the probe position to match a tip position and angles in the current TRANSFORMED space, this will reverse both the bregma and CoordinateTransform settings
    /// </summary>
    public void SetProbePositionTransformed(float ap, float ml, float dv, float phi, float theta, float spin)
    {
        if (tpmanager.UseIBLAngles())
            if (tpmanager.GetInVivoTransformState())
                insertion.SetCoordinates_IBL(ap, ml, dv, phi, theta, spin, tpmanager.GetActiveCoordinateTransform());
            else
                insertion.SetCoordinates_IBL(ap, ml, dv, phi, theta, spin);
        else
            if (tpmanager.GetInVivoTransformState())
                insertion.SetCoordinates(ap, ml, dv, phi, theta, spin, tpmanager.GetActiveCoordinateTransform());
            else
                insertion.SetCoordinates(ap, ml, dv, phi, theta, spin);

        SetProbePosition();
    }


    /// <summary>
    /// Get the coordinates of the current probe in mm or um, depending on the current IBL state
    /// </summary>
    /// <returns>List of ap in um, ml in um, phi, theta, spin</returns>
    public (float, float, float, float, float, float) GetCoordinates()
    {
        if (tpmanager.GetInVivoTransformState())
            return (tpmanager.UseIBLAngles()) ? insertion.GetCoordinatesFloat_IBL(tpmanager.GetActiveCoordinateTransform()) : insertion.GetCoordinatesFloat(tpmanager.GetActiveCoordinateTransform());
        
        return (tpmanager.UseIBLAngles()) ? insertion.GetCoordinatesFloat_IBL() : insertion.GetCoordinatesFloat();
    }

    public ProbeInsertion GetInsertion()
    {
        return insertion;
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

    #region Movement Controls

    public void MoveProbeAPML(float ap, float ml, bool pressed)
    {
        float speed = pressed ? 
            keyFast ? MOVE_INCREMENT_TAP_FAST : keySlow ? MOVE_INCREMENT_TAP_SLOW : MOVE_INCREMENT_TAP : 
            keyFast ? MOVE_INCREMENT_HOLD_FAST * Time.deltaTime : keySlow ? MOVE_INCREMENT_HOLD_SLOW * Time.deltaTime : MOVE_INCREMENT_HOLD * Time.deltaTime;

        insertion.ap += ap * speed;
        insertion.ml += ml * speed;
    }

    public void MoveProbeDV(float dv, bool pressed)
    {
        float speed = pressed ?
            keyFast ? MOVE_INCREMENT_TAP_FAST : keySlow ? MOVE_INCREMENT_TAP_SLOW : MOVE_INCREMENT_TAP :
            keyFast ? MOVE_INCREMENT_HOLD_FAST * Time.deltaTime : keySlow ? MOVE_INCREMENT_HOLD_SLOW * Time.deltaTime : MOVE_INCREMENT_HOLD * Time.deltaTime;

        insertion.dv += dv * speed;
    }

    [Obsolete]
    public void MoveProbeDepth(float depth, bool pressed)
    {
        float speed = pressed ?
            keyFast ? MOVE_INCREMENT_TAP_FAST : keySlow ? MOVE_INCREMENT_TAP_SLOW : MOVE_INCREMENT_TAP :
            keyFast ? MOVE_INCREMENT_HOLD_FAST * Time.deltaTime : keySlow ? MOVE_INCREMENT_HOLD_SLOW * Time.deltaTime : MOVE_INCREMENT_HOLD * Time.deltaTime;

        //insertion.depth += depth * speed;
    }

    public void RotateProbe(float phi, float theta, bool pressed)
    {
        float speed = pressed ?
            keyFast ? ROT_INCREMENT_TAP_FAST : keySlow ? ROT_INCREMENT_TAP_SLOW : ROT_INCREMENT_TAP :
            keyFast ? ROT_INCREMENT_HOLD_FAST * Time.deltaTime : keySlow ? ROT_INCREMENT_HOLD_SLOW * Time.deltaTime : ROT_INCREMENT_HOLD * Time.deltaTime;

        insertion.phi += phi * speed;
        insertion.theta = Mathf.Clamp(insertion.theta + theta * speed, minTheta, maxTheta);
    }

    public void SpinProbe(float spin, bool pressed)
    {
        float speed = pressed ?
            keyFast ? ROT_INCREMENT_TAP_FAST : keySlow ? ROT_INCREMENT_TAP_SLOW : ROT_INCREMENT_TAP :
            keyFast ? ROT_INCREMENT_HOLD_FAST * Time.deltaTime : keySlow ? ROT_INCREMENT_HOLD_SLOW * Time.deltaTime : ROT_INCREMENT_HOLD * Time.deltaTime;

        insertion.spin += spin * speed;
    }

    // Drag movement variables
    private bool axisLockAP;
    private bool axisLockML;
    private bool axisLockDV;
    private bool axisLockDepth;
    private bool axisLockRF;
    private bool axisLockQE;

    private Vector3 origAPMLDV;
    private float origPhi;
    private float origTheta;

    // Camera variables
    private Vector3 originalClickPositionWorld;
    private float cameraDistance;

    /// <summary>
    /// Handle setting up drag movement after a user clicks on the probe
    /// </summary>
    public void DragMovementClick()
    {
        // Cancel movement if being controlled by SensapexLink
        if (_sensapexLinkMovement)
            return;

        tpmanager.SetProbeControl(true);

        axisLockAP = false;
        axisLockDV = false;
        axisLockML = false;
        axisLockDepth = false;
        axisLockRF = false;
        axisLockQE = false;

        origAPMLDV = new Vector3(insertion.ap, insertion.ml, insertion.dv);
        origPhi = insertion.phi;
        origTheta = insertion.theta;

        // Track the screenPoint that was initially clicked
        cameraDistance = Vector3.Distance(Camera.main.transform.position, gameObject.transform.position);
        originalClickPositionWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));
    }

    /// <summary>
    /// Helper function: if the user was already moving on some other axis and then we *switch* axis, or
    /// if they repeatedly tap the same axis key we shouldn't jump back to the original position the
    /// probe was in.
    /// </summary>
    private void CheckForPreviousDragClick()
    {
        if (axisLockAP || axisLockDV || axisLockML || axisLockDepth || axisLockQE || axisLockRF)
            DragMovementClick();
    }

    /// <summary>
    /// Handle probe movements when a user is dragging while keeping the mouse pressed
    /// </summary>
    public void DragMovementDrag()
    {
        // Cancel movement if being controlled by SensapexLink
        if (_sensapexLinkMovement)
            return;

        CheckForSpeedKeys();
        Vector3 curScreenPointWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S))
        {
            // If the user was previously moving on a different axis we shouldn't accidentally reset their previous motion data
            CheckForPreviousDragClick();
            axisLockAP = true;
            axisLockML = false;
            axisLockDV = false;
            axisLockDepth = false;
            axisLockQE = false;
            axisLockRF = false;
            SetAxisVisibility(true, false, false);
        }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = true;
            axisLockDV = false;
            axisLockDepth = false;
            axisLockQE = false;
            axisLockRF = false;
            SetAxisVisibility(false, true, false);
        }
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = false;
            axisLockDV = true;
            axisLockDepth = false;
            axisLockQE = false;
            axisLockRF = false;
            SetAxisVisibility(false, false, true);
        }
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.F))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = false;
            axisLockDV = false;
            axisLockDepth = false;
            axisLockQE = false;
            axisLockRF = true;
        }
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = false;
            axisLockDV = false;
            axisLockDepth = false;
            axisLockQE = true;
            axisLockRF = false;
        }

        Vector3 worldOffset = curScreenPointWorld - originalClickPositionWorld;

        bool moved = false;
        if (axisLockAP)
        {
            insertion.ap = origAPMLDV.x + worldOffset.z;
            moved = true;
        }
        if (axisLockML)
        {
            insertion.ml = origAPMLDV.y - worldOffset.x;
            moved = true;
        }
        if (axisLockDV)
        {
            insertion.dv = origAPMLDV.z - worldOffset.y;
            moved = true;
        }
        //if (axisLockDepth)
        //{
        //    insertion.depth = origDepth - 1.5f * worldOffset.y;
        //    moved = true;
        //}
        if (axisLockRF)
        {
            insertion.theta = Mathf.Clamp(origTheta + 3f * worldOffset.y, minTheta, maxTheta);
            moved = true;
        }
        if (axisLockQE)
        {
            insertion.phi = origPhi - 3f * worldOffset.x;
            moved = true;
        }


        if (moved)
        {
            if (tpmanager.GetCollisions())
                CheckCollisions(tpmanager.GetAllNonActiveColliders());

            tpmanager.UpdateInPlaneView();
            SetProbePosition();

            foreach (ProbeUIManager puimanager in probeUIManagers)
                puimanager.ProbeMoved();

            tpmanager.SetMovedThisFrame();
        }

    }


    /// <summary>
    /// Release control of mouse movements after the user releases the mouse button from a probe
    /// </summary>
    public void DragMovementRelease()
    {
        // release probe control
        SetAxisVisibility(false, false, false);
        tpmanager.SetProbeControl(false);
    }


    #endregion

    #region Text

    public void UpdateText()
    {
        float localDepth = GetLocalDepth();
        Vector3 apmldv = GetInsertionCoordinateTransformed();
        string[] apml_string = GetAPMLStr();

        (float ap, float ml, float dv, float phi, float theta, float spin) = tpmanager.UseIBLAngles() ?
            insertion.GetCoordinatesFloat_IBL() :
            insertion.GetCoordinatesFloat();

        string updateStr = string.Format("Probe #{0}: " + apml_string[0] + " {1} " + 
            apml_string[1] + " {2} " +
            apml_string[2] + " {3} Azimuth {4} Elevation {5} " + GetDepthStr() + " {6} Spin {7}",
            probeID, round0(apmldv.x * 1000), round0(apmldv.y * 1000), round0(apmldv.z * 1000), round2(Utils.CircDeg(phi, minPhi, maxPhi)), round2(theta), round0(localDepth * 1000), round2(Utils.CircDeg(spin, minSpin, maxSpin))); ;

        textUI.text = updateStr;
    }

    private void Probe2Text()
    {
        Debug.LogWarning("Text not setup right now");
        //float localDepth = GetLocalDepth();
        //Vector2 apml_local = GetTransformedTipCoord();
        //string[] apml_string = GetAPMLStr();

        //(float ap, float ml, float dv, float depth, float phi, float theta, float spin) = tpmanager.UseIBLAngles() ?
        //    insertion.GetCoordinatesFloat_IBL() :
        //    insertion.GetCoordinatesFloat();

        //string fullStr = string.Format("Probe #{0}: " + apml_string[0] + " {1} " + apml_string[1] + " {2} Azimuth {3} Elevation {4} "+ GetDepthStr()+" {5} Spin {6} Record Height {7}",
        //    probeID, apml_local.x * 1000, apml_local.y * 1000, Utils.CircDeg(phi,minPhi, maxPhi), theta, localDepth, Utils.CircDeg(spin,minSpin,maxSpin), minRecordHeight * 1000);
        //GUIUtility.systemCopyBuffer = fullStr;

        // When you copy text, also set this probe to be active
        tpmanager.SetActiveProbe(this);
    }

    public float[] Text2Probe()
    {
        float[] output = new float[7];

        // Parse the text string and re-build the probe variables. 

        return output;
    }

    private string[] GetAPMLStr()
    {
        if (tpmanager.GetInVivoTransformState())
        {
            string prefix = tpmanager.GetInVivoPrefix();
            if (tpmanager.GetConvertAPML2Probe())
            {
                return new string[] { prefix + "Forward", prefix + "Side", prefix + "DV" };
            }
            else
            {
                return new string[] { prefix + "AP", prefix + "ML", prefix + "DV" };
            }
        }
        else
        {
            if (tpmanager.GetConvertAPML2Probe())
            {
                return new string[] { "ccfForward", "ccfSide", "ccfDV" };
            }
            else
            {
                return new string[] { "ccfAP", "ccfML", "ccfDV" };
            }
        }
    }

    private string GetDepthStr()
    {
        if (tpmanager.GetInVivoTransformState())
            return tpmanager.GetInVivoPrefix() + "Depth";
        else
            return "ccfDepth";
    }

    #endregion

    /// <summary>
    /// Returns the coordinate that a user should target to insert a probe into the brain.
    /// If the probe is outside the brain we return the tip position
    /// Once the probe is in the brain we return the brain surface position
    /// </summary>
    /// <returns></returns>
    private Vector3 GetInsertionCoordinateTransformed()
    {
        Vector3 insertionCoord = probeInBrain ? Utils.world2apmldv(brainSurfaceWorld + tpmanager.GetCenterOffset()) : insertion.apmldv;
        
        // If we're in a transformed space we need to transform the coordinates
        // before we do anything else.
        if (tpmanager.GetInVivoTransformState())
            insertionCoord = tpmanager.CoordinateTransformFromCCF(insertionCoord);

        // We can rotate the ap/ml position now to account for off-coronal/sagittal manipulator angles
        if (tpmanager.GetConvertAPML2Probe())
        {
            // convert to probe angle by solving 
            float localAngleRad = insertion.phi * Mathf.PI / 180f; // our phi is 0 when it we point forward, and our angles go backwards

            float x = insertionCoord.x * Mathf.Cos(localAngleRad) + insertionCoord.y * Mathf.Sin(localAngleRad);
            float y = -insertionCoord.x * Mathf.Sin(localAngleRad) + insertionCoord.y * Mathf.Cos(localAngleRad);
            return new Vector3(x, y, insertionCoord.z);
        }
        else
        {
            // just return apml
            return insertionCoord;
        }
    }

    private float GetLocalDepth()
    {
        if (probeInBrain)
        {
            // Get the direction
            float dir = Mathf.Sign(Vector3.Dot(probeTipT.transform.position - brainSurfaceWorld, -probeTipT.transform.up));
            // Get the distance
            float distance = (tpmanager.GetInVivoTransformState()) ?
                Vector3.Distance(tpmanager.CoordinateTransformFromCCF(probeTipT.transform.position), tpmanager.CoordinateTransformFromCCF(brainSurfaceWorld)) :
                Vector3.Distance(probeTipT.transform.position, brainSurfaceWorld);

            return dir * distance;
        }
        // If the probe is not in the brain, return NaN
        return float.NaN;
    }

    public void RegisterProbeCallback(int ID, Color probeColor)
    {
        probeID = ID;
        name = "PROBE_" + probeID;
        probeRenderer.material.color = probeColor;

        var colors = textButton.colors;
        colors.highlightedColor = probeColor;
        Color probeColorTransparent = probeColor;
        probeColorTransparent.a = 0.75f;
        colors.selectedColor = probeColorTransparent;
        colors.pressedColor = probeColorTransparent;
        textButton.colors = colors;
    }


    private float round0(float input)
    {
        return Mathf.Round(input);
    }
    private float round2(float input)
    {
        return Mathf.Round(input * 100) / 100;
    }



    public (Vector3, Vector3) GetRecordingRegionCoordinatesAPDVLR()
    {
        return GetRecordingRegionCoordinatesAPDVLR(probeTipOffset.transform, probeEndOffset.transform);
    }

    /// <summary>
    /// Compute the position of the bottom and top of the recording region in AP/DV/LR coordinates
    /// </summary>
    /// <returns></returns>
    public (Vector3, Vector3) GetRecordingRegionCoordinatesAPDVLR(Transform probeTipOffsetT, Transform probeEndOffsetT)
    {
        float[] heightPerc = GetRecordingRegionHeight();
        //Debug.Log(heightPerc[0] + " " + heightPerc[1]);

        Vector3 tip_apdvlr;
        Vector3 top_apdvlr;

        if (tpmanager.RecordingRegionOnly())
        {
            float mmStartPos = heightPerc[0] * (10 - heightPerc[1]);
            float mmRecordingSize = heightPerc[1];
            float mmEndPos = mmStartPos + mmRecordingSize;
            // shift the starting tipPos up by the mmStartPos
            Vector3 tipPos = probeTipOffsetT.position + probeTipOffsetT.up * mmStartPos;
            // shift the tipPos again to get the endPos
            Vector3 endPos = tipPos + probeTipOffsetT.up * mmRecordingSize;
            //GameObject.Find("recording_bot").transform.position = tipPos;
            //GameObject.Find("recording_top").transform.position = endPos;
            tip_apdvlr = Utils.WorldSpace2apdvlr25(tipPos);
            top_apdvlr = Utils.WorldSpace2apdvlr25(endPos);
        }
        else
        {
            tip_apdvlr = Utils.WorldSpace2apdvlr25(probeTipOffsetT.position);
            top_apdvlr = Utils.WorldSpace2apdvlr25(probeEndOffsetT.position);
        }

        return (tip_apdvlr, top_apdvlr);
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

    /// <summary>
    /// Check whether the probe is in the brain.
    /// If it is, calculate the brain surface coordinate by iterating up the probe until you leave the brain.
    /// </summary>
    private void UpdateSurfacePosition()
    {
        (Vector3 surfacePosition, float depth, Vector3 angles) = CCF2Surface(probeTipT.position, insertion.angles);

        brainSurfaceWorld = surfacePosition;


        if (float.IsNaN(depth))
        {
            // not in the brain
            probeInBrain = false;
            // these debugs are really bad coding style -- tpmanager should *get* the position and set these, it shouldn't be called from here
            tpmanager.SetSurfaceDebugActive(false);
        }
        else
        {
            // in the brain
            probeInBrain = true;
            // these debugs are really bad coding style -- tpmanager should *get* the position and set these, it shouldn't be called from here
            tpmanager.SetSurfaceDebugActive(true);
            tpmanager.SetSurfaceDebugPosition(brainSurfaceWorld);
        }
    }

    /// <summary>
    /// to implement 
    /// </summary>
    public void LockProbeToArea()
    {

    }

    #region Transforms

    /// <summary>
    /// Convert from CCF insertion coordinates to brain surface/depth/angles coordinates
    /// 
    /// If the tip coordinate is outside the brain this function returns the tip coordinate as the surface, a depth of NaN, and identical angles
    /// 
    /// This function is quite expensive to run!
    /// </summary>
    /// <param name="tipPosition"></param>
    /// <param name="angles"></param>
    /// <returns></returns>
    public (Vector3, float, Vector3) CCF2Surface(Vector3 tipPosition, Vector3 angles)
    {
        Vector3 tip_apdvlr25 = Utils.WorldSpace2apdvlr25(tipPosition);

        bool crossedThroughBrain = annotationDataset.ValueAtIndex(tip_apdvlr25) > 0;
       
        // Iterate up until you exit the brain
        // if you started outside, first find when you enter
        Vector3 top = Utils.WorldSpace2apdvlr25(probeTipT.position + probeTipT.up * 10f);
        for (float perc = 0; perc <= 1f; perc += 0.0005f)
        {
            Vector3 point = Vector3.Lerp(tip_apdvlr25, top, perc);
            if (crossedThroughBrain)
            {
                if (annotationDataset.ValueAtIndex(point) <= 0)
                {
                    Vector3 surfacePosition = Utils.apdvlr25_2World(point);
                    return (surfacePosition, Vector3.Distance(tipPosition, surfacePosition), angles);
                }
            }
            else
            {
                if (annotationDataset.ValueAtIndex(point) > 0)
                    crossedThroughBrain = true;
            }
        }

        return (tipPosition, float.NaN, angles);
    }

    private Transform surfaceCalculatorT;

    /// <summary>
    /// Recover the tip position when you only know the surface position and depth.
    /// 
    /// Use the tip position to set the probe position when converting from surface coordinates.
    /// </summary>
    /// <param name="surfacePosition"></param>
    /// <param name="depth"></param>
    /// <param name="angles"></param>
    /// <returns>(tipPosition in CCF, angles)</returns>
    public (Vector3, Vector3) Surface2CCF(Vector3 surfacePosition, float depth, Vector3 angles,
        bool stereoCoordinates = false)
    {
        //// I've tried this a number of ways and can't figure it out. It's possible the way I'm applying rotations can't be done by euler angles? 
        //// I think the simplest thing for now is to create an empty gameobject and use its transform to perform this calculation.
        if (surfaceCalculatorT == null)
        {
            GameObject surfaceCalculatorGO = new GameObject("SurfaceCalculator");
            surfaceCalculatorT = surfaceCalculatorGO.transform;
        }

        // reset
        surfaceCalculatorT.position = surfacePosition;
        surfaceCalculatorT.rotation = initialRotation;

        surfaceCalculatorT.Translate(-surfaceCalculatorT.up * depth);
        surfaceCalculatorT.RotateAround(surfacePosition, surfaceCalculatorT.up, angles.x);
        surfaceCalculatorT.RotateAround(surfacePosition, surfaceCalculatorT.forward, angles.y);
        //// skip spin because it won't actually affect anything
        ////tempT.RotateAround(rotateAround.position, rotateAround.up, localInsertion.spin);
        ///
        Vector3 tipPosition = Utils.world2apmldv(surfaceCalculatorT.position);

        return (tipPosition, angles);
    }


    #endregion

    #region Sensapex Link and Control

    #region Property Getters and Setters

    /// <summary>
    /// Return if this probe is being controlled by the Sensapex Link
    /// </summary>
    /// <returns>True if movement is controlled by Sensapex Link, False otherwise</returns>
    public bool GetSensapexLinkMovement()
    {
        return _sensapexLinkMovement;
    }

    /// <summary>
    /// (un)Register a probe and begin echoing position
    /// </summary>
    /// <param name="register">To register or deregister this probe</param>
    /// <param name="manipulatorId">ID of the manipulator in real life to connect to</param>
    /// <param name="calibrated">Is the manipulator in real life calibrated</param>
    /// <param name="onSuccess">Callback function to handle a successful registration</param>
    /// <param name="onError">Callback function to handle a failed registration</param>
    public void SetSensapexLinkMovement(bool register, int manipulatorId = 0, bool calibrated = true,
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
        _sensapexLinkMovement = register;

        if (register)
            _sensapexLinkCommunicationManager.RegisterManipulator(manipulatorId, () =>
            {
                Debug.Log("Manipulator Registered");
                _manipulatorId = manipulatorId;

                if (calibrated)
                    // Bypass calibration and start echoing
                    _sensapexLinkCommunicationManager.BypassCalibration(manipulatorId, StartEchoing);
                else
                    // Enable write
                    _sensapexLinkCommunicationManager.SetCanWrite(manipulatorId, true, 1,
                        _ =>
                        {
                            // Calibrate
                            _sensapexLinkCommunicationManager.Calibrate(manipulatorId,
                                () =>
                                {
                                    // Disable write and start echoing
                                    _sensapexLinkCommunicationManager.SetCanWrite(manipulatorId, false, 0,
                                        _ => onSuccess?.Invoke());
                                });
                        });

                onSuccess?.Invoke();
            }, err => onError?.Invoke(err));
        else
            _sensapexLinkCommunicationManager.UnregisterManipulator(_manipulatorId, () =>
            {
                Debug.Log("Manipulator Unregistered");
                ResetManipulatorProperties();
                onSuccess?.Invoke();
            }, err => onError?.Invoke(err));

        // Start echoing process
        void StartEchoing()
        {
            // Read and start echoing position
            _sensapexLinkCommunicationManager.GetPos(manipulatorId, vector4 =>
            {
                if (_bregmaOffset.Equals(Vector4.negativeInfinity)) _bregmaOffset = vector4;
                Debug.Log("Bregma offset: " + _bregmaOffset);
                EchoPositionFromSensapexLink(vector4);
            });
        }
    }

    /// <summary>
    ///     Get attached manipulator ID
    /// </summary>
    /// <returns>Attached manipulator ID, 0 if none are attached</returns>
    public int GetManipulatorId()
    {
        return _manipulatorId;
    }

    /// <summary>
    ///     Set manipulator properties such as ID and positional offsets back to defaults
    /// </summary>
    public void ResetManipulatorProperties()
    {
        _manipulatorId = 0;
        _bregmaOffset = Vector4.negativeInfinity;
        _brainSurfaceOffset = 0;
    }

    /// <summary>
    ///     Return if this probe is being controlled by the Sensapex Link
    /// </summary>
    /// <returns>True if this probe is attached to a manipulator, false otherwise</returns>
    public bool IsConnectedToManipulator()
    {
        return _manipulatorId != 0;
    }

    /// <summary>
    ///     Probe angles as phi, theta, spin
    /// </summary>
    /// <returns>Angles in degrees in phi, theta, spin order</returns>
    public Vector3 GetProbeAngles()
    {
        return _probeAngles;
    }

    /// <summary>
    ///     Set probe angles in phi, theta, spin order
    /// </summary>
    /// <param name="angles">Angles in degrees in phi, theta, spin order</param>
    public void SetProbeAngles(Vector3 angles)
    {
        _probeAngles = angles;
        _phiCos = Mathf.Cos(_probeAngles.x * Mathf.Deg2Rad);
        _phiSin = Mathf.Sin(_probeAngles.x * Mathf.Deg2Rad);
    }

    /// <summary>
    ///     Manipulator space offset to bregma as X, Y, Z, Depth
    /// </summary>
    /// <returns>Manipulator space offset to bregma as X, Y, Z, Depth</returns>
    public Vector4 GetBregmaOffset()
    {
        return _bregmaOffset;
    }

    /// <summary>
    ///     Set manipulator space offset to bregma as X, Y, Z, Depth
    /// </summary>
    /// <param name="bregmaOffset">Offset from bregma as X, Y, Z, Depth</param>
    public void SetBregmaOffset(Vector4 bregmaOffset)
    {
        _bregmaOffset = bregmaOffset;
    }
    
    /// <summary>
    /// Get manipulator space offset from brain surface as Depth
    /// </summary>
    /// <returns>Manipulator space offset to brain surface</returns>
    public float GetBrainSurfaceOffset()
    {
        return _brainSurfaceOffset;
    }
    
    /// <summary>
    ///     Set manipulator space offset from brain surface as Depth from input
    /// </summary>
    /// <param name="brainSurfaceOffset">Offset from brain surface as Depth</param>
    public void SetBrainSurfaceOffset(float brainSurfaceOffset)
    {
        _brainSurfaceOffset = brainSurfaceOffset;
    }

    /// <summary>
    ///     Set manipulator space offset from brain surface as Depth from manipulator or probe coordinates
    /// </summary>
    public void SetBrainSurfaceOffset()
    {
        _sensapexLinkCommunicationManager.GetPos(_manipulatorId, pos => _brainSurfaceOffset = pos.w);
    }

    #endregion

    #region Actions

    /// <summary>
    ///     Echo given position in needles transform space to the probe
    /// </summary>
    /// <param name="pos">Position of manipulator in needles transform</param>
    public void EchoPositionFromSensapexLink(Vector4 pos)
    {
        /*
         * +x = L
         * +Y = A
         * +Z = V
         */
        
        // Convert position to CCF
        var offsetAdjustedPosition = pos - _bregmaOffset;
        
        // Phi adjustment
        var phiAdjustedX = offsetAdjustedPosition.x * _phiCos -
                           offsetAdjustedPosition.y * _phiSin;
        var phiAdjustedY = offsetAdjustedPosition.x * _phiSin +
                           offsetAdjustedPosition.y * _phiCos;
        offsetAdjustedPosition.x = phiAdjustedX;
        offsetAdjustedPosition.y = phiAdjustedY;
        
        var positionAxisAdjusted = new Vector4(offsetAdjustedPosition.y, -offsetAdjustedPosition.x,
            -offsetAdjustedPosition.z, offsetAdjustedPosition.w);

        // Drive normally when not moving depth, otherwise use surface coordinates
        if (Math.Abs(offsetAdjustedPosition.w) < 1)
        {
            ManualCoordinateEntryTransformed(positionAxisAdjusted, _probeAngles);
        }
        else
        {
            // Convert manipulator reported coordinates to world coordinates for conversion
            var tipPos = Surface2CCF(Utils.apmldv2world(positionAxisAdjusted),
                -(offsetAdjustedPosition.w - _brainSurfaceOffset),
                _probeAngles).Item1;
            insertion.SetCoordinates_IBL(tipPos.x, tipPos.y, tipPos.z,
                _probeAngles.x, _probeAngles.y, _probeAngles.z, tpmanager.GetActiveCoordinateTransform());
            SetProbePosition();

            // Tell the tpmanager we moved and update the UI elements
            tpmanager.SetMovedThisFrame();
            foreach (var puimanager in probeUIManagers)
                puimanager.ProbeMoved();
            tpmanager.UpdateInPlaneView();
        }


        // Continue echoing position
        if (_sensapexLinkMovement)
            _sensapexLinkCommunicationManager.GetPos(_manipulatorId, EchoPositionFromSensapexLink);
    }

    #endregion

    #endregion

    #region AxisControl

    private void SetAxisVisibility(bool AP, bool ML, bool DV)
    {
        tpmanager.SetAxisVisibility(AP, ML, DV, probeTipT.position);
    }

    #endregion AxisControl

    #region Materials

    /// <summary>
    /// Set all Renderer components to use the ghost material
    /// </summary>
    public void SetMaterialsTransparent()
    {
        defaultMaterials.Clear();
        foreach (Renderer renderer in transform.GetComponentsInChildren<Renderer>())
        {
            defaultMaterials.Add(renderer.gameObject, renderer.material);
            renderer.material = ghostMaterial;
        }
    }

    /// <summary>
    /// Reverse a previous call to SetMaterialsTransparent()
    /// </summary>
    public void SetMaterialsDefault()
    {
        foreach (Renderer renderer in transform.GetComponentsInChildren<Renderer>())
            if (defaultMaterials.ContainsKey(renderer.gameObject))
                renderer.material = defaultMaterials[renderer.gameObject];
    }

    #endregion
}
