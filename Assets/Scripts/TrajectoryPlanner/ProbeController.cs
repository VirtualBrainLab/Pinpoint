using System;
using System.Collections;
using System.Collections.Generic;
using TrajectoryPlanner;
using UnityEngine;
using UnityEngine.EventSystems;

public class ProbeController : MonoBehaviour
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

    #region Key hold flags
    private bool keyFast = false;
    private bool keySlow = false;
    private bool keyHeld = false; // If a key is held, we will skip re-checking the key hold delay for any other keys that are added
    private float keyPressTime = 0f;
    private const float keyHoldDelay = 0.300f;
    #endregion

    #region Angle limits
    private const float minTheta = -90f;
    private const float maxTheta = 0f;
    #endregion

    #region Recording region
    private float minRecordHeight;
    private float maxRecordHeight; // get this by measuring the height of the recording rectangle and subtracting from 10
    private float recordingRegionSizeY;
    #endregion

    #region Defaults
    // in ap/ml/dv
    private Vector3 defaultStart = new Vector3(5.4f, 5.7f, 0.332f);
    private float defaultDepth = 0f;
    private Vector2 defaultAngles = new Vector2(-90f, 0f); // 0 phi is forward, default theta is 90 degrees down from horizontal, but internally this is a value of 0f
    #endregion

    // Probe positioning information
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Transform surfaceCalculatorT;

    // total position data (for dealing with coordinates)
    private ProbeInsertion insertion;
    private float depth;

    // Offset vectors
    private GameObject probeTipOffset;

    // References
    private TrajectoryPlannerManager tpmanager;
    private ProbeManager probeManager;
    [SerializeField] private Transform probeTipT;
    [SerializeField] private List<GameObject> recordingRegionGOs;
    [SerializeField] private Transform rotateAround;

    private void Awake()
    {
        // Create two points offset from the tip that we'll use to interpolate where we are on the probe
        probeTipOffset = new GameObject(name + "TipOffset");
        probeTipOffset.transform.position = probeTipT.position + probeTipT.up * 0.2f;
        probeTipOffset.transform.parent = probeTipT;

        // Access surface calculator (just an empty transform)
        surfaceCalculatorT = GameObject.Find("SurfaceCalculator").transform;

        depth = defaultDepth;

        UpdateRecordingRegionVars();

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        ResetPositionTracking();
        ResetInsertion();
    }
    
    public void Register(TrajectoryPlannerManager tpmanager, ProbeManager probeManager)
    {
        this.tpmanager = tpmanager;
        this.probeManager = probeManager;
    }

    /// <summary>
    /// Put this probe back at Bregma
    /// </summary>
    public void ResetInsertion()
    {
        ResetPosition();
        ResetAngles();

        ResetUI();
    }

    public void ResetUI()
    {
        // reset UI as well
        // ResetPositionTracking();
        SetProbePosition();
        probeManager.UpdateText();
    }

    public void ResetPosition()
    {
        transform.position = initialPosition;
        insertion.apmldv = defaultStart;
    }

    public void ResetAngles()
    {
        transform.rotation = initialRotation;
        insertion.angles = defaultAngles;
    }

    /// <summary>
    /// Helper function to reset all of the position data, without updating any UI elements
    /// </summary>
    private void ResetPositionTracking()
    {
        insertion = new ProbeInsertion(defaultStart, defaultAngles);
    }

    #region Keyboard movement

    private void CheckForSpeedKeys()
    {
        keyFast = Input.GetKey(KeyCode.LeftShift);
        keySlow = Input.GetKey(KeyCode.LeftControl);
    }

    public bool MoveProbe_Keyboard(bool checkForCollisions)
    {
        // drag movement takes precedence
        if (dragging)
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

        if (Input.GetKeyDown(KeyCode.Q))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            probeManager.SetDropToSurfaceWithDepth(false);
            MoveProbeDV(1f, true);
        }
        else if (Input.GetKey(KeyCode.Q) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            probeManager.SetDropToSurfaceWithDepth(false);
            MoveProbeDV(1f, false);
        }
        if (Input.GetKeyUp(KeyCode.Q))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.E))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            probeManager.SetDropToSurfaceWithDepth(false);
            MoveProbeDV(-1f, true);
        }
        else if (Input.GetKey(KeyCode.E) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            probeManager.SetDropToSurfaceWithDepth(false);
            MoveProbeDV(-1f, false);
        }
        if (Input.GetKeyUp(KeyCode.E))
            keyHeld = false;

        // Depth movement

        if (Input.GetKeyDown(KeyCode.Z))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            probeManager.SetDropToSurfaceWithDepth(true);
            MoveProbeDepth(1f, true);
        }
        else if (Input.GetKey(KeyCode.Z) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            probeManager.SetDropToSurfaceWithDepth(true);
            MoveProbeDepth(1f, false);
        }
        if (Input.GetKeyUp(KeyCode.Z))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.X))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            probeManager.SetDropToSurfaceWithDepth(true);
            MoveProbeDepth(-1f, true);
        }
        else if (Input.GetKey(KeyCode.X) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            probeManager.SetDropToSurfaceWithDepth(true);
            MoveProbeDepth(-1f, false);
        }
        if (Input.GetKeyUp(KeyCode.X))
            keyHeld = false;

        // Rotations

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            RotateProbe(-1f, 0f, true);
        }
        else if (Input.GetKey(KeyCode.Alpha1) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            RotateProbe(-1f, 0f, false);
        }
        if (Input.GetKeyUp(KeyCode.Alpha1))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            RotateProbe(1f, 0f, true);
        }
        else if (Input.GetKey(KeyCode.Alpha3) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            RotateProbe(1f, 0f, false);
        }
        if (Input.GetKeyUp(KeyCode.Alpha3))
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
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            SpinProbe(-1f, true);
        }
        else if (Input.GetKey(KeyCode.Comma) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            SpinProbe(-1f, false);
        }
        if (Input.GetKeyUp(KeyCode.Comma))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.Period))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            SpinProbe(1f, true);
        }
        else if (Input.GetKey(KeyCode.Period) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            SpinProbe(1f, false);
        }
        if (Input.GetKeyUp(KeyCode.Period))
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
                probeManager.CheckCollisions(tpmanager.GetAllNonActiveColliders());

            // Update all the UI panels
            probeManager.UpdateUI();

            return true;
        }
        else
        {
            return false;
        }
    }

    #endregion


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

    public void MoveProbeDepth(float depth, bool pressed)
    {
        float speed = pressed ?
            keyFast ? MOVE_INCREMENT_TAP_FAST : keySlow ? MOVE_INCREMENT_TAP_SLOW : MOVE_INCREMENT_TAP :
            keyFast ? MOVE_INCREMENT_HOLD_FAST * Time.deltaTime : keySlow ? MOVE_INCREMENT_HOLD_SLOW * Time.deltaTime : MOVE_INCREMENT_HOLD * Time.deltaTime;

        this.depth += depth * speed;
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
    private bool axisLockTheta;
    private bool axisLockPhi;
    private bool dragging;

    private Vector3 origAPMLDV;
    private float origPhi;
    private float origTheta;

    // Camera variables
    private Vector3 originalClickPositionWorld;
    private Vector3 lastClickPositionWorld;
    private float cameraDistance;

    /// <summary>
    /// Handle setting up drag movement after a user clicks on the probe
    /// </summary>
    public void DragMovementClick()
    {
        // ignore mouse clicks if we're over a UI element
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        // Cancel movement if being controlled by SensapexLink
        if (probeManager.GetSensapexLinkMovement())
            return;

        tpmanager.SetProbeControl(true);

        axisLockAP = false;
        axisLockDV = false;
        axisLockML = false;
        axisLockDepth = false;
        axisLockTheta = false;
        axisLockPhi = false;

        origAPMLDV = new Vector3(insertion.ap, insertion.ml, insertion.dv);
        origPhi = insertion.phi;
        origTheta = insertion.theta;
        // Note: depth is special since it gets absorbed into the probe position on each frame

        // Track the screenPoint that was initially clicked
        cameraDistance = Vector3.Distance(Camera.main.transform.position, gameObject.transform.position);
        originalClickPositionWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));
        lastClickPositionWorld = originalClickPositionWorld;

        dragging = true;
    }

    /// <summary>
    /// Helper function: if the user was already moving on some other axis and then we *switch* axis, or
    /// if they repeatedly tap the same axis key we shouldn't jump back to the original position the
    /// probe was in.
    /// </summary>
    private void CheckForPreviousDragClick()
    {
        if (axisLockAP || axisLockDV || axisLockML || axisLockDepth || axisLockPhi || axisLockTheta)
            DragMovementClick();
    }

    /// <summary>
    /// Handle probe movements when a user is dragging while keeping the mouse pressed
    /// </summary>
    public void DragMovementDrag()
    {
        // Cancel movement if being controlled by SensapexLink
        if (probeManager.GetSensapexLinkMovement())
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
            axisLockPhi = false;
            axisLockTheta = false;
            probeManager.SetAxisVisibility(true, false, false, false);
        }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = true;
            axisLockDV = false;
            axisLockDepth = false;
            axisLockPhi = false;
            axisLockTheta = false;
            probeManager.SetAxisVisibility(false, true, false, false);
        }
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = false;
            axisLockDV = false;
            axisLockDepth = true;
            axisLockPhi = false;
            axisLockTheta = false;
            probeManager.SetAxisVisibility(false, false, false, true);
        }
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.F))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = false;
            axisLockDV = false;
            axisLockDepth = false;
            axisLockPhi = false;
            axisLockTheta = true;
        }
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = false;
            axisLockDV = true;
            axisLockDepth = false;
            axisLockPhi = false;
            axisLockTheta = false;
            probeManager.SetAxisVisibility(false, false, true, false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha3))
        {
            CheckForPreviousDragClick();
            axisLockAP = false;
            axisLockML = false;
            axisLockDV = false;
            axisLockDepth = false;
            axisLockPhi = true;
            axisLockTheta = false;
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
        if (axisLockDepth)
        {
            worldOffset = curScreenPointWorld - lastClickPositionWorld;
            lastClickPositionWorld = curScreenPointWorld;
            depth = -1.5f * worldOffset.y;
            moved = true;
        }
        if (axisLockTheta)
        {
            insertion.theta = Mathf.Clamp(origTheta + 3f * worldOffset.y, minTheta, maxTheta);
            moved = true;
        }
        if (axisLockPhi)
        {
            insertion.phi = origPhi - 3f * worldOffset.x;
            moved = true;
        }


        if (moved)
        {
            if (tpmanager.GetCollisions())
                probeManager.CheckCollisions(tpmanager.GetAllNonActiveColliders());

            tpmanager.UpdateInPlaneView();
            SetProbePosition();

            probeManager.UpdateUI();

            tpmanager.SetMovedThisFrame();
        }

    }

    #endregion

    /// <summary>
    /// Release control of mouse movements after the user releases the mouse button from a probe
    /// </summary>
    public void DragMovementRelease()
    {
        // release probe control
        dragging = false;
        probeManager.SetAxisVisibility(false, false, false, false);
        tpmanager.SetProbeControl(false);
    }

    #region Recording region UI
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

        UpdateRecordingRegionVars();
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

    private void UpdateRecordingRegionVars()
    {
        minRecordHeight = recordingRegionGOs[0].transform.localPosition.y;
        maxRecordHeight = minRecordHeight + (10 - recordingRegionGOs[0].transform.localScale.y);
    }

    #endregion

    #region Set Probe Position

    /// <summary>
    /// Set the probe position to the current apml/depth/phi/theta/spin values
    /// </summary>
    public void SetProbePosition(float depthOverride = 0f)
    {
        if (depthOverride != 0f)
            depth = depthOverride;
        SetProbePositionCCF(insertion);

        // Check where we are relative to the surface of the brain
        probeManager.UpdateSurfacePosition();

        // Tell the tpmanager we moved and update the UI elements
        tpmanager.SetMovedThisFrame();
        probeManager.UpdateUI();

        // Update probe text
        probeManager.UpdateText();
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

        // Compute depth transform, if needed
        if (depth != 0f)
        {
            transform.position += -transform.up * depth;
            Vector3 depthAdjustment = Utils.world2apmldv(-transform.up * depth);
            localInsertion.SetCoordinates(localInsertion.ap + depthAdjustment.x, localInsertion.ml + depthAdjustment.y, localInsertion.dv + depthAdjustment.z,
                localInsertion.angles.x, localInsertion.angles.y, localInsertion.angles.z);
            depth = 0f;
        }

        // save the data
        insertion.SetCoordinates(localInsertion);
    }
    
    public IEnumerator SetProbePositionCCF_Delayed(ProbeInsertion localInsertion, float depthOverride = 0f)
    {
        yield return new WaitForEndOfFrame();
        SetProbePositionCCF(localInsertion);
    }

    public void SetProbePositionWorld(Vector3 worldPosition)
    {
        Vector3 apmldv = Utils.world2apmldv(worldPosition + tpmanager.GetCenterOffset());
        insertion.SetCoordinates(apmldv.x, apmldv.y, apmldv.z, insertion.phi, insertion.theta, insertion.spin);
        SetProbePosition();
    }

    /// <summary>
    /// Set the probe position to match a tip position and angles in the current TRANSFORMED space, this will reverse both the bregma and CoordinateTransform settings
    /// </summary>
    public void SetProbeInsertionTransformed(float ap, float ml, float dv, float phi, float theta, float spin, float depthOverride = 0f)
    {
        if (tpmanager.GetSetting_UseIBLAngles())
            if (tpmanager.GetSetting_InVivoTransformActive())
                insertion.SetCoordinates_IBL(ap, ml, dv, phi, theta, spin, tpmanager.GetActiveCoordinateTransform());
            else
                insertion.SetCoordinates_IBL(ap, ml, dv, phi, theta, spin);
        else
            if (tpmanager.GetSetting_InVivoTransformActive())
            insertion.SetCoordinates(ap, ml, dv, phi, theta, spin, tpmanager.GetActiveCoordinateTransform());
        else
            insertion.SetCoordinates(ap, ml, dv, phi, theta, spin);

        SetProbePosition(depthOverride);
    }
    public void SetProbeInsertionTransformed(Vector3 apmldv, Vector3 angles, float depthOverride = 0f)
    {
        SetProbeInsertionTransformed(apmldv.x, apmldv.y, apmldv.z, angles.x, angles.y, angles.z, depthOverride);
    }

    public IEnumerator SetProbeInsertionTransformed_Delayed(float ap, float ml, float dv, float phi, float theta, float spin, float delay, float depthOverride = 0f)
    {
        yield return new WaitForSeconds(delay);
        // SetProbePositionTransformed(ap, ml, dv, depthOverride);
        // SetProbeAnglesTransformed(phi, theta, spin);
        SetProbeInsertionTransformed(ap, ml, dv, phi, theta, spin, depthOverride);
    }

    public void SetProbePositionTransformed(float ap, float ml, float dv, float depthOverride = 0f)
    {
        if (tpmanager.GetSetting_InVivoTransformActive())
            insertion.SetPosition(ap, ml, dv, tpmanager.GetActiveCoordinateTransform());
        else
            insertion.SetPosition(ap, ml, dv);

        SetProbePosition(depthOverride);
    }

    public void SetProbePositionTransformed(Vector4 position)
    {
        SetProbePositionTransformed(position.x, position.y, position.z, position.w);
    }

    public void SetProbeAnglesTransformed(float phi, float theta, float spin)
    {
        if (tpmanager.GetSetting_UseIBLAngles())
            insertion.SetAngles_IBL(phi, theta, spin);
        else
            insertion.SetAngles(phi, theta, spin);

        SetProbePosition();
    }

    #endregion

    #region Getters

    /// <summary>
    /// Get the tip transform
    /// </summary>
    /// <returns>tip transform</returns>
    public Transform GetTipTransform()
    {
        return probeTipT;
    }
    
    /// <summary>
    /// Get the coordinates of the current probe (tip/angles) in mm or um, depending on the current IBL state
    /// </summary>
    /// <returns>(ap, ml, dv, phi, theta, spin)</returns>
    public (float, float, float, float, float, float) GetCoordinatesTransformed()
    {
        return tpmanager.GetSetting_UseIBLAngles() ?
            insertion.GetCoordinatesFloat_IBL(tpmanager.GetActiveCoordinateTransform()) :
            insertion.GetCoordinatesFloat(tpmanager.GetActiveCoordinateTransform());
    }
    /// <summary>
    /// Get the coordinates of the surface and depth, relative to the tip position, depending on the current IBL state
    /// </summary>
    /// <returns>(ap, ml, dv, phi, theta, spin)</returns>
    public (float, float, float, float, float, float, float) GetCoordinatesSurfaceTransformed(bool probeInBrain, Vector3 brainSurfaceWorld)
    {

        CoordinateTransform coordTransform = tpmanager.GetActiveCoordinateTransform();

        Vector2 phiTheta = tpmanager.GetSetting_UseIBLAngles() ?
            Utils.World2IBL(insertion.angles) :
            insertion.angles;

        Vector3 tipCoord = insertion.apmldv;
        Vector3 entryCoord = probeInBrain ? Utils.world2apmldv(brainSurfaceWorld + tpmanager.GetCenterOffset()) : tipCoord;

        if (coordTransform != null)
        {
            entryCoord = coordTransform.FromCCF(entryCoord);
            tipCoord = coordTransform.FromCCF(tipCoord);
        }

        float depth = probeInBrain ? Vector3.Distance(tipCoord, entryCoord) : float.NaN;

        return (entryCoord.x * 1000f, entryCoord.y * 1000f, entryCoord.z * 1000f, depth * 1000f, phiTheta.x, phiTheta.y, insertion.angles.z);
    }

    public ProbeInsertion GetInsertion()
    {
        return insertion;
    }

    public (Vector3, Vector3) GetRecordingRegionCoordinatesAPDVLR()
    {
        return GetRecordingRegionCoordinatesAPDVLR(probeTipOffset.transform);
    }

    /// <summary>
    /// Compute the position of the bottom and top of the recording region in AP/DV/LR coordinates
    /// </summary>
    /// <returns></returns>
    public (Vector3, Vector3) GetRecordingRegionCoordinatesAPDVLR(Transform probeTipOffsetT)
    {
        //Debug.Log(heightPerc[0] + " " + heightPerc[1]);

        Vector3 tip_apdvlr;
        Vector3 top_apdvlr;

        if (tpmanager.GetSetting_ShowRecRegionOnly())
        {
            (float mmStartPos, float mmRecordingSize) = GetRecordingRegionHeight();

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
            top_apdvlr = Utils.WorldSpace2apdvlr25(probeTipOffsetT.position + probeTipOffsetT.up * 10.0f);
        }

        return (tip_apdvlr, top_apdvlr);
    }

    /// <summary>
    /// Return the height of the bottom in mm and the total height
    /// </summary>
    /// <returns>float array [0]=bottom, [1]=height</returns>
    public (float, float) GetRecordingRegionHeight()
    {
        return (recordingRegionGOs[0].transform.localPosition.y - minRecordHeight, recordingRegionSizeY);
    }

    /// <summary>
    /// Return the current size of the recording region
    /// </summary>
    /// <returns>size of the recording region</returns>
    public float GetRecordingRegionSize()
    {
        return recordingRegionSizeY;
    }

    #endregion

}
