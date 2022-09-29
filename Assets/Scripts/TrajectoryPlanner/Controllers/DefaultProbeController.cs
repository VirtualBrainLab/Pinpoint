using System;
using System.Collections;
using System.Collections.Generic;
using TrajectoryPlanner;
using UnityEngine;
using UnityEngine.EventSystems;
using CoordinateSpaces;

public class DefaultProbeController : ProbeController
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
    private Vector3 defaultStart = Vector3.zero; // new Vector3(5.4f, 5.7f, 0.332f);
    private float defaultDepth = 0f;
    private Vector2 defaultAngles = new Vector2(-90f, 0f); // 0 phi is forward, default theta is 90 degrees down from horizontal, but internally this is a value of 0f
    #endregion

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private float depth;

    // Offset vectors
    private GameObject probeTipOffset;

    // References
    [SerializeField] private Transform probeTipT;
    [SerializeField] private List<GameObject> recordingRegionGOs;
    [SerializeField] private Transform rotateAround;

    private void Awake()
    {
        // Create two points offset from the tip that we'll use to interpolate where we are on the probe
        probeTipOffset = new GameObject(name + "TipOffset");
        probeTipOffset.transform.position = probeTipT.position + probeTipT.up * 0.2f;
        probeTipOffset.transform.parent = probeTipT;

        depth = defaultDepth;

        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        UpdateRecordingRegionVars();

        Insertion = new ProbeInsertion(defaultStart, defaultAngles, TPManager.GetCoordinateSpace(), TPManager.GetActiveCoordinateTransform());
    }

    private void Start()
    {
        SetProbePosition();
    }

    /// <summary>
    /// Put this probe back at Bregma
    /// </summary>
    public override void ResetInsertion()
    {
        ResetPosition();
        ResetAngles();
        SetProbePosition();
    }

    public override void ResetPosition()
    {
        Insertion.apmldv = defaultStart;
    }

    public override void ResetAngles()
    {
        Insertion.angles = defaultAngles;
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
            MoveProbeXYZ(0f, 0f, -1f, true);
        }
        else if (Input.GetKey(KeyCode.W) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeXYZ(0f, 0f, -1f, false);
        }
        if (Input.GetKeyUp(KeyCode.W))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.S))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            MoveProbeXYZ(0f, 0f, 1f, true);
        }
        else if (Input.GetKey(KeyCode.S) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeXYZ(0f, 0f, 1f, false);
        }
        if (Input.GetKeyUp(KeyCode.S))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.D))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            MoveProbeXYZ(-1f, 0f, 0f, true);
        }
        else if (Input.GetKey(KeyCode.D) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeXYZ(-1f, 0f, 0f, false);
        }
        if (Input.GetKeyUp(KeyCode.D))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.A))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            MoveProbeXYZ(1f, 0f, 0f, true);
        }
        else if (Input.GetKey(KeyCode.A) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeXYZ(1f, 0f, 0f, false);
        }
        if (Input.GetKeyUp(KeyCode.A))
            keyHeld = false;

        // DV movement

        if (Input.GetKeyDown(KeyCode.Q))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            ProbeManager.SetDropToSurfaceWithDepth(false);
            MoveProbeXYZ(0f, -1f, 0f, true);
        }
        else if (Input.GetKey(KeyCode.Q) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeXYZ(0f, -1f, 0f, false);
        }
        if (Input.GetKeyUp(KeyCode.Q))
            keyHeld = false;

        if (Input.GetKeyDown(KeyCode.E))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            ProbeManager.SetDropToSurfaceWithDepth(false);
            MoveProbeXYZ(0f, 1f, 0f, true);
        }
        else if (Input.GetKey(KeyCode.E) && (keyHeld || keyHoldDelayPassed))
        {
            keyHeld = true;
            moved = true;
            MoveProbeXYZ(0f, 1f, 0f, false);
        }
        if (Input.GetKeyUp(KeyCode.E))
            keyHeld = false;

        // Depth movement

        if (Input.GetKeyDown(KeyCode.Z))
        {
            moved = true;
            keyPressTime = Time.realtimeSinceStartup;
            ProbeManager.SetDropToSurfaceWithDepth(true);
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
            ProbeManager.SetDropToSurfaceWithDepth(true);
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
                ProbeManager.CheckCollisions(TPManager.GetAllNonActiveColliders());

            // Update all the UI panels
            ProbeManager.UpdateUI();

            return true;
        }
        else
        {
            return false;
        }
    }

    #endregion


    #region Movement Controls

    public void MoveProbeXYZ(float x, float y, float z, bool pressed)
    {
        float speed = pressed ?
            keyFast ? MOVE_INCREMENT_TAP_FAST : keySlow ? MOVE_INCREMENT_TAP_SLOW : MOVE_INCREMENT_TAP :
            keyFast ? MOVE_INCREMENT_HOLD_FAST * Time.deltaTime : keySlow ? MOVE_INCREMENT_HOLD_SLOW * Time.deltaTime : MOVE_INCREMENT_HOLD * Time.deltaTime;

        // Get the xyz transformation
        Vector3 xyz = new Vector3(x, y, z) * speed;
        // Rotate to match the probe axis directions
        Insertion.apmldv += Insertion.World2TransformedRot(xyz);
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

        Insertion.phi += phi * speed;
        Insertion.theta = Mathf.Clamp(Insertion.theta + theta * speed, minTheta, maxTheta);
    }

    public void SpinProbe(float spin, bool pressed)
    {
        float speed = pressed ?
            keyFast ? ROT_INCREMENT_TAP_FAST : keySlow ? ROT_INCREMENT_TAP_SLOW : ROT_INCREMENT_TAP :
            keyFast ? ROT_INCREMENT_HOLD_FAST * Time.deltaTime : keySlow ? ROT_INCREMENT_HOLD_SLOW * Time.deltaTime : ROT_INCREMENT_HOLD * Time.deltaTime;

        Insertion.spin += spin * speed;
    }

    // Drag movement variables
    private bool axisLockZ;
    private bool axisLockX;
    private bool axisLockY;
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
        // Cancel movement if being controlled by EphysLink
        if (ProbeManager.GetEphysLinkMovement())
            return;

        TPManager.SetProbeControl(true);

        axisLockZ = false;
        axisLockY = false;
        axisLockX = false;
        axisLockDepth = false;
        axisLockTheta = false;
        axisLockPhi = false;

        origAPMLDV = Insertion.apmldv;
        origPhi = Insertion.phi;
        origTheta = Insertion.theta;
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
        if (axisLockZ || axisLockY || axisLockX || axisLockDepth || axisLockPhi || axisLockTheta)
            DragMovementClick();
    }

    /// <summary>
    /// Handle probe movements when a user is dragging while keeping the mouse pressed
    /// </summary>
    public void DragMovementDrag()
    {
        // Cancel movement if being controlled by EphysLink
        if (ProbeManager.GetEphysLinkMovement())
            return;

        CheckForSpeedKeys();
        Vector3 curScreenPointWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));
        Vector3 worldOffset = curScreenPointWorld - originalClickPositionWorld;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S))
        {
            // If the user was previously moving on a different axis we shouldn't accidentally reset their previous motion data
            CheckForPreviousDragClick();
            axisLockZ = true;
            axisLockX = false;
            axisLockY = false;
            axisLockDepth = false;
            axisLockPhi = false;
            axisLockTheta = false;
            ProbeManager.SetAxisVisibility(true, false, false, false);
        }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
        {
            CheckForPreviousDragClick();
            axisLockZ = false;
            axisLockX = true;
            axisLockY = false;
            axisLockDepth = false;
            axisLockPhi = false;
            axisLockTheta = false;
            ProbeManager.SetAxisVisibility(false, true, false, false);
        }
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X))
        {
            CheckForPreviousDragClick();
            axisLockZ = false;
            axisLockX = false;
            axisLockY = false;
            axisLockDepth = true;
            axisLockPhi = false;
            axisLockTheta = false;
            ProbeManager.SetAxisVisibility(false, false, false, true);
        }
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.F))
        {
            CheckForPreviousDragClick();
            axisLockZ = false;
            axisLockX = false;
            axisLockY = false;
            axisLockDepth = false;
            axisLockPhi = false;
            axisLockTheta = true;
        }
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
        {
            CheckForPreviousDragClick();
            axisLockZ = false;
            axisLockX = false;
            axisLockY = true;
            axisLockDepth = false;
            axisLockPhi = false;
            axisLockTheta = false;
            ProbeManager.SetAxisVisibility(false, false, true, false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha3))
        {
            CheckForPreviousDragClick();
            axisLockZ = false;
            axisLockX = false;
            axisLockY = false;
            axisLockDepth = false;
            axisLockPhi = true;
            axisLockTheta = false;
        }


        bool moved = false;

        Vector3 newXYZ = Vector3.zero;

        if (axisLockX)
        {
            newXYZ.x = worldOffset.x;
            moved = true;
        }
        if (axisLockY)
        {
            newXYZ.y = worldOffset.y;
            moved = true;
        }
        if (axisLockZ)
        {
            newXYZ.z = worldOffset.z;
            moved = true;
        }

        if (moved)
        {
            Insertion.apmldv = origAPMLDV + Insertion.World2TransformedRot(newXYZ);
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
            Insertion.theta = Mathf.Clamp(origTheta + 3f * worldOffset.y, minTheta, maxTheta);
            moved = true;
        }
        if (axisLockPhi)
        {
            Insertion.phi = origPhi - 3f * worldOffset.x;
            moved = true;
        }


        if (moved)
        {
            if (TPManager.GetCollisions())
                ProbeManager.CheckCollisions(TPManager.GetAllNonActiveColliders());

            TPManager.UpdateInPlaneView();
            SetProbePosition();

            ProbeManager.UpdateUI();

            TPManager.MovedThisFrame = true;
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
        ProbeManager.SetAxisVisibility(false, false, false, false);
        TPManager.SetProbeControl(false);
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

    #region Set Probe pos/angles

    /// <summary>
    /// Set the probe position to the current apml/depth/phi/theta/spin values
    /// </summary>
    public override void SetProbePosition()
    {
        SetProbePosition(Insertion);
    }

    public void SetProbePosition(float depthOverride)
    {
        depth = depthOverride;
        SetProbePosition();
    }

    public override void SetProbePosition(Vector3 position)
    {
        Insertion.apmldv = position;
        SetProbePosition();
    }

    public override void SetProbePosition(Vector4 positionDepth)
    {
        Insertion.apmldv = positionDepth;
        depth = positionDepth.w;
        SetProbePosition();
    }

    public override void SetProbeAngles(Vector3 angles)
    {
        Insertion.angles = angles;
        SetProbePosition();
    }

    /// <summary>
    /// Set the position of the probe to match a ProbeInsertion object in CCF coordinates
    /// </summary>
    /// <param name="localInsertion">new insertion position</param>
    public override void SetProbePosition(ProbeInsertion localInsertion)
    {
        // Reset everything
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        // Manually adjust the coordinates and rotation
        transform.position += localInsertion.PositionWorld();
        transform.RotateAround(rotateAround.position, transform.up, localInsertion.phi);
        transform.RotateAround(rotateAround.position, transform.forward, localInsertion.theta);
        transform.RotateAround(rotateAround.position, rotateAround.up, localInsertion.spin);

        // Compute depth transform, if needed
        if (depth != 0f)
        {
            transform.position += -transform.up * depth;
            Vector3 depthAdjustment = Insertion.World2TransformedRot(-transform.up) * depth;

            localInsertion.apmldv += depthAdjustment;
            depth = 0f;
        }

        // save the data
        Insertion = localInsertion;

        // update surface position
        ProbeManager.UpdateSurfacePosition();

        // Tell the tpmanager we moved and update the UI elements
        TPManager.MovedThisFrame = true;
        TPManager.UpdateInPlaneView();
        ProbeManager.UpdateUI();
    }

    #endregion

    #region Getters

    /// <summary>
    /// Get the tip transform
    /// </summary>
    /// <returns>tip transform</returns>
    public override Transform GetTipTransform()
    {
        return probeTipT;
    }

    public (Vector3 startCoordWorld, Vector3 endCoordWorld) GetRecordingRegionCoordinates()
    {
        return GetRecordingRegionCoordinates(probeTipOffset.transform);
    }

    /// <summary>
    /// Compute the start and end positions of the recording region in World coordinates
    /// </summary>
    /// <returns></returns>
    public (Vector3 startCoordWorld, Vector3 endCoordWorld) GetRecordingRegionCoordinates(Transform probeTipOffsetT)
    {
        if (TPManager.GetSetting_ShowRecRegionOnly())
        {
            (float mmStartPos, float mmRecordingSize) = GetRecordingRegionHeight();

            Vector3 tipCoordT = Insertion.apmldv;
            Vector3 tipUpT = Insertion.World2TransformedRot(probeTipOffsetT.up).normalized;

            Vector3 startCoordT = tipCoordT + tipUpT * mmStartPos;
            Vector3 topCoordT = startCoordT + tipUpT * mmRecordingSize;

            //// shift the starting tipPos up by the mmStartPos
            //Vector3 tipPos = probeTipOffsetT.position + probeTipOffsetT.up * mmStartPos;
            //// shift the tipPos again to get the endPos
            //Vector3 endPos = tipPos + probeTipOffsetT.up * mmRecordingSize;

            return (Insertion.Transformed2World(startCoordT), Insertion.Transformed2World(topCoordT));
        }
        else
            Debug.LogWarning("Not implemented");
        return (Vector3.zero, Vector3.zero);
            //return (probeTipOffsetT.position, probeTipOffsetT.position + probeTipOffsetT.up * 10f);
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
