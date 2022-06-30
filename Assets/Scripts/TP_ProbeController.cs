using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3D space control for Neuropixels probes in the Trajectory Planner scene
/// </summary>
public class TP_ProbeController : MonoBehaviour
{

    // MOVEMENT CONTROL SPEEDS
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

    // Internal flags that track when keys are held down
    private bool keyFast = false;
    private bool keySlow = false;
    private bool keyHeld = false; // If a key is held, we will skip re-checking the key hold delay for any other keys that are added
    private float keyPressTime = 0f;
    [SerializeField] private float keyHoldDelay = 0.300f;

    // Internal flags that track whether we are in manual control or drag/link control mode
    private bool draggingMovement = false;
    private bool sensapexLinkMovement = false;

    // Exposed fields to collect links to other components inside of the Probe prefab
    [SerializeField] private List<Collider> probeColliders;
    [SerializeField] private List<TP_ProbeUIManager> probeUIManagers;
    [SerializeField] private Transform rotateAround;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private Renderer probeRenderer;
    [SerializeField] private List<GameObject> recordingRegionGOs;
    [SerializeField] private int probeType;
    [SerializeField] private Transform probeTipT;

    private TP_TrajectoryPlannerManager tpmanager;

    // in ap/ml/dv
    private Vector3 iblBregma = new Vector3(5.4f, 5.739f, 0.332f);
    private Vector2 defaultAngles = new Vector2(-90f, 0f); // 0 phi is forward, default theta is 90 degrees down from horizontal, but internally this is a value of 0f

    // Text
    private int probeID;
    private TextMeshProUGUI textUI;

    // Probe positioning information
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // total position data (for dealing with coordinates)
    private Vector2 apml;
    private float depth;
    private float phi; // rotation around the up axis (z)
    private float minPhi = -180;
    private float maxPhi = 180f;
    private float theta; // rotation around the right axis (x)
    private float minTheta = -90f;
    private float maxTheta = 0f;
    private float spin; // rotation around the probe's own vertical axis (equivalent to phi until the probe is off angle)
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
    private Collider ccfBounds;
    private AnnotationDataset annotationDataset;
    private bool probeInBrain;
    private Vector3 entryPoint;
    private Vector3 exitPoint;
    private Vector3 brainSurface;

    // Colliders
    private List<GameObject> visibleProbeColliders;
    private Dictionary<GameObject, Material> visibleOtherColliders;

    // Text button
    GameObject textGO;
    Button textButton;

    private void Awake()
    {
        // Setup some basic variables
        textGO = Instantiate(textPrefab, GameObject.Find("CoordinatePanel").transform);
        textButton = textGO.GetComponent<Button>();
        textButton.onClick.AddListener(Probe2Text);
        textUI = textGO.GetComponent<TextMeshProUGUI>();

        // Pull the tpmanager object and register this probe
        GameObject main = GameObject.Find("main");
        tpmanager = main.GetComponent<TP_TrajectoryPlannerManager>();
        tpmanager.RegisterProbe(this, probeColliders);

        // Get access to the annotation dataset and world-space boundaries
        annotationDataset = tpmanager.GetAnnotationDataset();
        ccfBounds = tpmanager.CCFCollider();

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
        // Save where we are in space (at bregma)
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Reset all the tracking variables
        ResetPositionTracking();
        SetProbePosition();

        // Reset our probe UI panels
        foreach (TP_ProbeUIManager puimanager in probeUIManagers)
            puimanager.ProbeMoved();
    }

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

    /// <summary>
    /// Called by Unity when this object is destroyed. Removes the probe panels and the position text.
    /// </summary>
    public void Destroy()
    {
        // Delete this gameObject
        foreach (TP_ProbeUIManager puimanager in probeUIManagers)
            puimanager.Destroy();
        Destroy(textGO);
    }

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
        foreach (TP_ProbeUIManager puimanager in probeUIManagers)
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
        apml = new Vector2();
        depth = 0;
        phi = defaultAngles.x;
        theta = defaultAngles.y;
        spin = 0;
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
            MoveProbeAPML(1f, 0f, true);
        }
        else if (Input.GetKey(KeyCode.W) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeAPML(1f, 0f, false);
        }
        if (Input.GetKeyUp(KeyCode.W))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.S))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            MoveProbeAPML(-1f, 0f, true);
        }
        else if (Input.GetKey(KeyCode.S) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeAPML(-1f, 0f, false);
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

        // Depth movement

        if (Input.GetKeyDown(KeyCode.Z))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            MoveProbeDepth(1f, true);
        }
        else if (Input.GetKey(KeyCode.Z) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeDepth(1f, false);
        }
        if (Input.GetKeyUp(KeyCode.Z))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.X))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            MoveProbeDepth(-1f, true);
        }
        else if (Input.GetKey(KeyCode.X) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeDepth(-1f, false);
        }
        if (Input.GetKeyUp(KeyCode.X))
            keyHeld = false;

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
            SetProbePosition(apml.x, apml.y, depth, phi, theta, spin);

            // Check collisions if we need to
            if (checkForCollisions)
                CheckCollisions(tpmanager.GetAllNonActiveColliders());

            // Check where we are relative to the surface of the brain
            UpdateSurfacePosition();

            // Update all the UI panels
            foreach (TP_ProbeUIManager puimanager in probeUIManagers)
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
    /// Set the coordinates of the probe by hand. Can't set depth relative to the brain surface (yet)
    /// </summary>
    /// <param name="ap"></param>
    /// <param name="ml"></param>
    /// <param name="depth"></param>
    /// <param name="phi"></param>
    /// <param name="theta"></param>
    /// <param name="spin"></param>
    public void ManualCoordinateEntry(float ap, float ml, float depth, float phi, float theta, float spin)
    {
        //[TODO: Add depth from brain as a parameter that can be set
        //if (tpmanager.GetDepthFromBrain())
        //{
        //    if (probeInBrain)
        //    {
        //        float angleRelDown = Vector3.Angle(-probeTipT.up, Vector3.down);
        //        float extraDepth = (iblBregma.y - brainSurface.y) / Mathf.Cos(angleRelDown);
        //        depth += extraDepth;
        //    }
        //    else
        //        depth = 0f;
        //}

        // Convert the IBL phi/theta angles to world space
        Vector2 worldPhiTheta = tpmanager.IBL2World(new Vector2(phi, theta));

        // Set the probe position, switching from um to mm
        SetProbePosition(ap/1000, ml / 1000, depth / 1000, worldPhiTheta.x, worldPhiTheta.y, spin);

        // Update where we are relative to the surface
        UpdateSurfacePosition();

        // Tell the tpmanager we moved and update the UI elements
        tpmanager.SetMovedThisFrame();
        foreach (TP_ProbeUIManager puimanager in probeUIManagers)
            puimanager.ProbeMoved();
        tpmanager.UpdateInPlaneView();
    }

    public IEnumerator DelayedManualCoordinateEntry(float delay, float ap, float ml, float depth, float phi, float theta, float spin)
    {
        yield return new WaitForSeconds(delay);
        ManualCoordinateEntry(ap, ml, depth, phi, theta, spin);
    }

    /// <summary>
    /// Set the probe position to the current apml/depth/phi/theta/spin values
    /// </summary>
    public void SetProbePosition()
    {
        SetProbePosition(apml.x, apml.y, depth, phi, theta, spin);
    }

    /// <summary>
    /// Set the probe position to a set of passed in values
    /// </summary>
    /// <param name="ap">position on the ap axis in mm</param>
    /// <param name="ml">position on the ml axis in mm</param>
    /// <param name="depth">depth relative to bregma (or CCF 0 if bregma is disabled)</param>
    /// <param name="phi"></param>
    /// <param name="theta"></param>
    /// <param name="spin"></param>
    public void SetProbePosition(float ap, float ml, float depth, float phi, float theta, float spin)
    {
        // Reset everything
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        ResetPositionTracking();

        // Manually adjust the coordinates and rotation
        transform.RotateAround(rotateAround.position, transform.up, phi);
        this.phi = phi;
        transform.RotateAround(rotateAround.position, transform.forward, theta);
        this.theta = theta;
        transform.RotateAround(rotateAround.position, rotateAround.up, spin);
        this.spin = spin;
        transform.position += tpmanager.GetBregma() ? new Vector3(-ml-iblBregma.y, 0f, -ap+iblBregma.x) : new Vector3(-ml, 0f, -ap);
        apml = new Vector2(ap, ml);
        if (tpmanager.GetBregma())
            transform.Translate(0f, -depth -iblBregma.z, 0f);
        else
            transform.Translate(0f, -depth, 0f);
        this.depth = depth;

        UpdateText();
    }

    /// <summary>
    /// Get the coordinates of the current probe in um
    /// </summary>
    /// <returns>List of ap in um, ml in um, depth in um, phi, theta, spin</returns>
    public List<float> GetCoordinates()
    {
        List<float> ret = new List<float>();
        ret.Add(apml.x*1000);
        ret.Add(apml.y*1000);
        ret.Add(depth*1000);
        Vector2 iblPhiTheta = tpmanager.World2IBL(new Vector2(phi, theta));
        ret.Add(iblPhiTheta.x);
        ret.Add(iblPhiTheta.y);
        ret.Add(spin);
        return ret;
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

    // MOVEMENT

    public void MoveProbeAPML(float ap, float ml, bool pressed)
    {
        float speed = pressed ? 
            keyFast ? MOVE_INCREMENT_TAP_FAST : keySlow ? MOVE_INCREMENT_TAP_SLOW : MOVE_INCREMENT_TAP : 
            keyFast ? MOVE_INCREMENT_HOLD_FAST * Time.deltaTime : keySlow ? MOVE_INCREMENT_HOLD_SLOW * Time.deltaTime : MOVE_INCREMENT_HOLD * Time.deltaTime;
        Vector2 delta = new Vector2(ap, ml) * speed;
        apml += delta;
    }

    public void MoveProbeDepth(float depth, bool pressed)
    {
        float speed = pressed ?
            keyFast ? MOVE_INCREMENT_TAP_FAST : keySlow ? MOVE_INCREMENT_TAP_SLOW : MOVE_INCREMENT_TAP :
            keyFast ? MOVE_INCREMENT_HOLD_FAST * Time.deltaTime : keySlow ? MOVE_INCREMENT_HOLD_SLOW * Time.deltaTime : MOVE_INCREMENT_HOLD * Time.deltaTime;

        float depthDelta = depth * speed;
        this.depth += depthDelta;
    }

    public void RotateProbe(float phi, float theta, bool pressed)
    {
        float speed = pressed ?
            keyFast ? ROT_INCREMENT_TAP_FAST : keySlow ? ROT_INCREMENT_TAP_SLOW : ROT_INCREMENT_TAP :
            keyFast ? ROT_INCREMENT_HOLD_FAST * Time.deltaTime : keySlow ? ROT_INCREMENT_HOLD_SLOW * Time.deltaTime : ROT_INCREMENT_HOLD * Time.deltaTime;

        float phiDelta = phi * speed;
        float thetaDelta = theta * speed;
        this.phi += phiDelta;
        this.theta = Mathf.Clamp(this.theta + thetaDelta, minTheta, maxTheta);
    }

    public void SpinProbe(float spin, bool pressed)
    {
        float speed = pressed ?
            keyFast ? ROT_INCREMENT_TAP_FAST : keySlow ? ROT_INCREMENT_TAP_SLOW : ROT_INCREMENT_TAP :
            keyFast ? ROT_INCREMENT_HOLD_FAST * Time.deltaTime : keySlow ? ROT_INCREMENT_HOLD_SLOW * Time.deltaTime : ROT_INCREMENT_HOLD * Time.deltaTime;

        float spinDelta = spin * speed;
        this.spin += spinDelta;
    }

    // TEXT UPDATES

    public void UpdateText()
    {
        float localDepth = GetLocalDepth();
        Vector2 apml_local = GetAPML();
        string[] apml_string = GetAPMLStr();

        Vector2 iblPhiTheta = tpmanager.World2IBL(new Vector2(phi, theta));

        string updateStr = string.Format("Probe #{0}: "+apml_string[0]+" {1} "+apml_string[1]+" {2} Azimuth {3} Elevation {4} "+ GetDepthStr() + " {5} Spin {6}",
            probeID, round0(apml_local.x*1000), round0(apml_local.y * 1000), round2(CircDeg(iblPhiTheta.x,minPhi, maxPhi)), round2(iblPhiTheta.y), round0(localDepth*1000), round2(CircDeg(spin,minSpin, maxSpin)));;

        textUI.text = updateStr;
    }

    private void Probe2Text()
    {
        float localDepth = GetLocalDepth();
        Vector2 apml_local = GetAPML();
        string[] apml_string = GetAPMLStr();

        Vector2 iblPhiTheta = tpmanager.World2IBL(new Vector2(phi, theta));

        string fullStr = string.Format("Probe #{0}: " + apml_string[0] + " {1} " + apml_string[1] + " {2} Azimuth {3} Elevation {4} "+ GetDepthStr()+" {5} Spin {6} Record Height {7}",
            probeID, apml_local.x * 1000, apml_local.y * 1000, CircDeg(iblPhiTheta.x,minPhi, maxPhi), iblPhiTheta.y, localDepth, CircDeg(spin,minSpin,maxSpin), minRecordHeight * 1000);
        GUIUtility.systemCopyBuffer = fullStr;

        // When you copy text, also set this probe to be active
        tpmanager.SetActiveProbe(this);
    }

    public float CircDeg(float deg, float minDeg, float maxDeg)
    {
        float diff = Mathf.Abs(maxDeg - minDeg);

        if (deg < minDeg) deg += diff;
        if (deg > maxDeg) deg -= diff;

        return deg;
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
            if (tpmanager.GetConvertAPML2Probe())
            {
                return new string[] { "stForward", "stSide" };
            }
            else
            {
                return new string[] { "stAP", "stML" };
            }
        }
        else
        {
            if (tpmanager.GetConvertAPML2Probe())
            {
                return new string[] { "ccfForward", "ccfSide" };
            }
            else
            {
                return new string[] { "ccfAP", "ccfML" };
            }
        }
    }

    private string GetDepthStr()
    {
        if (tpmanager.GetInVivoTransformState())
            return "stDepth";
        else
            return "ccfDepth";
    }

    private Vector2 GetAPML()
    {
        Vector2 localAPML;
        // If we're in stereotaxic coordinates, apply the conversion factors first, then deal with rotating in 3D space
        if (tpmanager.GetInVivoTransformState())
        {
            Debug.Log(apml);
            localAPML = tpmanager.CoordinateTransformFromCCF(new Vector3(apml.x, apml.y));
            Debug.Log(localAPML);
        }
        else
        {
            localAPML = apml;
        }

        if (tpmanager.GetConvertAPML2Probe())
        {
            // convert to probe angle by solving 
            float localAngleRad = phi * Mathf.PI / 180f; // our phi is 0 when it we point forward, and our angles go backwards

            float x = localAPML.x * Mathf.Cos(localAngleRad) + localAPML.y * Mathf.Sin(localAngleRad);
            float y = -localAPML.x * Mathf.Sin(localAngleRad) + localAPML.y * Mathf.Cos(localAngleRad);
            return new Vector2(x, y);
        }
        else
        {
            // just return apml
            return localAPML;
        }
    }

    private float GetLocalDepth()
    {
        float localDepth;
        if (tpmanager.GetInVivoTransformState())
        {
            if (tpmanager.GetDepthFromBrain())
            {
                if (probeInBrain)
                {
                    float depthDir = Mathf.Sign(Vector3.Dot(probeTipT.transform.position - brainSurface, -probeTipT.transform.up));
                    // Separately obtain the x/y/z components relative to the surface point, scale them, then get the distance
                    Vector3 distanceToSurface = probeTipT.transform.position - brainSurface;
                    Debug.LogError("IN VIVO CONVERSION IS BROKEN!!!");

                    //distanceToSurface = new Vector3(distanceToSurface.x * invivoConversionAPMLDV.y, distanceToSurface.y * invivoConversionAPMLDV.z, distanceToSurface.z * invivoConversionAPMLDV.x);

                    localDepth = depthDir * Vector3.Distance(distanceToSurface, Vector3.zero);
                }
                else
                    localDepth = float.NaN;
            }
            else
            {
                localDepth = 0f;
            }
        }
        else
        {
            if (tpmanager.GetDepthFromBrain())
            {
                if (probeInBrain)
                {
                    // The depth is correct, but if the tip is above the brainSurface we also need to know that...
                    localDepth = Mathf.Sign(Vector3.Dot(probeTipT.transform.position - brainSurface, -probeTipT.transform.up)) * Vector3.Distance(probeTipT.transform.position, brainSurface);
                }
                else
                    localDepth = float.NaN;
            }
            else
                localDepth = depth;
        }

        return localDepth;
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

    public int GetID()
    {
        return probeID;
    }

    public Color GetColor()
    {
        return probeRenderer.material.color;
    }

    private float round0(float input)
    {
        return Mathf.Round(input);
    }
    private float round2(float input)
    {
        return Mathf.Round(input * 100) / 100;
    }

    public List<Collider> GetProbeColliders()
    {
        return probeColliders;
    }

    // DRAGGING MOVEMENT
    
    // Drag movement variables
    private bool axisLockAP;
    private bool axisLockML;
    private bool axisLockDV;
    private bool axisLockRF;
    private bool axisLockQE;

    private Vector2 origAPML;
    private float origDepth;
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
        tpmanager.SetProbeControl(true);

        axisLockAP = false;
        axisLockDV = false;
        axisLockML = false;
        axisLockRF = false;
        axisLockQE = false;

        origAPML = apml;
        origDepth = depth;
        origPhi = phi;
        origTheta = theta;

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
        if (axisLockAP || axisLockDV || axisLockML || axisLockQE || axisLockRF)
            DragMovementClick();
    }

    /// <summary>
    /// Handle probe movements when a user is dragging while keeping the mouse pressed
    /// </summary>
    public void DragMovementDrag()
    {
        Vector3 curScreenPointWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S))
        {
            // If the user was previously moving on a different axis we shouldn't accidentally reset their previous motion data
            CheckForPreviousDragClick();
            axisLockAP = true;
            axisLockML = false;
            axisLockDV = false;
            axisLockQE = false;
            axisLockRF = false;
        }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = true;
            axisLockDV = false;
            axisLockQE = false;
            axisLockRF = false;
        }
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = false;
            axisLockDV = true;
            axisLockQE = false;
            axisLockRF = false;
        }
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.F))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = false;
            axisLockDV = false;
            axisLockQE = false;
            axisLockRF = true;
        }
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = false;
            axisLockDV = false;
            axisLockQE = true;
            axisLockRF = false;
        }

        Vector3 worldOffset = curScreenPointWorld - originalClickPositionWorld;

        if (axisLockAP)
        {
            apml.x = origAPML.x - worldOffset.z;
        }
        if (axisLockML)
        {
            apml.y = origAPML.y - worldOffset.x;
        }
        if (axisLockDV)
        {
            depth = origDepth - 1.5f * worldOffset.y;
        }
        if (axisLockRF)
        {
            theta = Mathf.Clamp(origTheta - 2f * worldOffset.y, minTheta, maxTheta);
        }
        if (axisLockQE)
        {
            phi = origPhi - 2f * worldOffset.x;
        }


        if (apml.x != origAPML.x || apml.y != origAPML.y || depth != origDepth || theta != origTheta || phi != origPhi)
        {
            if (tpmanager.GetCollisions())
                CheckCollisions(tpmanager.GetAllNonActiveColliders());

            tpmanager.UpdateInPlaneView();
            SetProbePosition(apml.x, apml.y, depth, phi, theta, spin);
            UpdateSurfacePosition();

            foreach (TP_ProbeUIManager puimanager in probeUIManagers)
                puimanager.ProbeMoved();

            tpmanager.SetMovedThisFrame();
        }

    }

    /// <summary>
    /// Compute the position of the bottom and top of the recording region in AP/DV/LR coordinates
    /// </summary>
    /// <returns></returns>
    public (Vector3, Vector3) GetRecordingRegionCoordinatesAPDVLR()
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
            Vector3 tipPos = probeTipOffset.transform.position + probeTipOffset.transform.up * mmStartPos;
            // shift the tipPos again to get the endPos
            Vector3 endPos = tipPos + probeTipOffset.transform.up * mmRecordingSize;
            //GameObject.Find("recording_bot").transform.position = tipPos;
            //GameObject.Find("recording_top").transform.position = endPos;
            tip_apdvlr = Utils.WorldSpace2apdvlr(tipPos + tpmanager.GetCenterOffset());
            top_apdvlr = Utils.WorldSpace2apdvlr(endPos + tpmanager.GetCenterOffset());
        }
        else
        {
            tip_apdvlr = Utils.WorldSpace2apdvlr(probeTipOffset.transform.position + probeTipOffset.transform.up + tpmanager.GetCenterOffset());
            top_apdvlr = Utils.WorldSpace2apdvlr(probeEndOffset.transform.position + tpmanager.GetCenterOffset());
        }

        return (tip_apdvlr, top_apdvlr);
    }

    /// <summary>
    /// Release control of mouse movements after the user releases the mouse button from a probe
    /// </summary>
    public void DragMovementRelease()
    {
        // release probe control
        tpmanager.SetProbeControl(false);
    }

    /// <summary>
    /// Re-scale probe panels 
    /// </summary>
    /// <param name="newPxHeight">Set the probe panels of this probe to a new height</param>
    public void ResizeProbePanel(int newPxHeight)
    {
        foreach (TP_ProbeUIManager puimanager in probeUIManagers)
        {
            puimanager.ResizeProbePanel(newPxHeight);
            puimanager.ProbeMoved();
        }
    }

    /// <summary>
    /// Do some raycasting to figure out whether the probe is inside the brain and if it is where the surface coordinate is
    /// </summary>
    private void UpdateSurfacePosition()
    {
        probeInBrain = false;
        bool exit = false; bool entry = false;

        // At least one point is in the brain. Using two rays from the tip and top find our entry and exit points into the CCF box
        RaycastHit hit;
        if (ccfBounds.Raycast(new Ray(probeTipT.position + probeTipT.up * 0.2f + -probeTipT.up * 20f, probeTipT.up), out hit, 40f))
        {
            exitPoint = hit.point;
            exit = true;
        }
        else
        {
            Debug.LogWarning("Exit point failed to be discovered");
        }
        if (ccfBounds.Raycast(new Ray(probeTipT.position + probeTipT.up * 10.2f + probeTipT.up * 20f, -probeTipT.up), out hit, 40f))
        {
            entryPoint = hit.point;
            entry = true;
        }
        else
        {
            Debug.LogWarning("Entry point failed to be discovered");
        }

        // Using the entry and exit point, find the brain surface
        if (entry && exit)
        {
            // [TODO: I'm pretty sure we can just raycast onto the mesh model for "root" to do this? not sure why I over-complicated this code
            Vector3 entry_apdvlr = Utils.WorldSpace2apdvlr(entryPoint + tpmanager.GetCenterOffset());
            Vector3 exit_apdvlr = Utils.WorldSpace2apdvlr(exitPoint + tpmanager.GetCenterOffset());
            // use this code for debugging:
            //GameObject.Find("entry").transform.position = util.apdvlr2World(entry_apdvlr) - tpmanager.GetCenterOffset();
            //GameObject.Find("exit").transform.position = util.apdvlr2World(exit_apdvlr) - tpmanager.GetCenterOffset();

            // This sort of has to work, but it isn't super efficient, might be ways to speed this up?
            // Step through the raycast until we find a point that has a non-zero annotation value. This is the entry coordinate
            for (float perc = 0; perc <= 1; perc += 0.002f)
            {
                Vector3 point = Vector3.Lerp(entry_apdvlr, exit_apdvlr, perc);
                if (annotationDataset.ValueAtIndex(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), Mathf.RoundToInt(point.z)) > 0)
                {
                    brainSurface = Utils.apdvlr2World(point) - tpmanager.GetCenterOffset();
                    probeInBrain = true;
                    // debug code
                    //GameObject.Find("surface").transform.position = brainSurface;
                    return;
                }
            }
            // If we get here we failed to find a point
            Debug.LogWarning("There is an entry and exit but we couldn't find the brain edge");
        }
    }

    /// <summary>
    /// Return the probe panel UI managers
    /// </summary>
    /// <returns>list of probe panel UI managers</returns>
    public List<TP_ProbeUIManager> GetProbeUIManagers()
    {
        return probeUIManagers;
    }
}
