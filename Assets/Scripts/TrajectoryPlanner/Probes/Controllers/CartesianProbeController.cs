using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class CartesianProbeController : ProbeController
{
    #region Movement constants
    private const float MOVE_INCREMENT_TAP = 0.010f; // move 1 um per tap
    private const float MOVE_INCREMENT_TAP_ULTRA = 1.000f;
    private const float MOVE_INCREMENT_TAP_FAST = 0.100f;
    private const float MOVE_INCREMENT_TAP_SLOW = 0.001f;
    private const float MOVE_INCREMENT_HOLD = 0.100f; // move 50 um per second when holding
    private const float MOVE_INCREMENT_HOLD_ULTRA = 10.000f;
    private const float MOVE_INCREMENT_HOLD_FAST = 1.000f;
    private const float MOVE_INCREMENT_HOLD_SLOW = 0.010f;
    private const float ROT_INCREMENT_TAP = 5f;
    private const float ROT_INCREMENT_TAP_ULTRA = 90f;
    private const float ROT_INCREMENT_TAP_FAST = 15f;
    private const float ROT_INCREMENT_TAP_SLOW = 1f;
    private const float ROT_INCREMENT_HOLD = 5f;
    private const float ROT_INCREMENT_HOLD_ULTRA = 90f;
    private const float ROT_INCREMENT_HOLD_FAST = 15;
    private const float ROT_INCREMENT_HOLD_SLOW = 1f;

    private Vector4 _forwardDir = new Vector4(0f, 0f, -1f, 0f);
    private Vector4 _rightDir = new Vector4(-1f, 0f, 0f, 0f);
    private Vector4 _upDir = new Vector4(0f, 1f, 0f, 0f);
    private Vector4 _depthDir = new Vector4(0f, 0f, 0f, 1f);

    private Vector3 _yawDir = new Vector3(1f, 0f, 0f);
    private Vector3 _pitchDir = new Vector3(0f, -1f, 0f);
    private Vector3 _rollDir = new Vector3(0f, 0f, 1f);
    #endregion

    #region Key hold flags
    private bool keyUltra = false;
    private bool keyFast = false;
    private bool keySlow = false;
    private int clickKeyHeld = 0;
    private int rotateKeyHeld = 0;
    private Vector4 clickHeldVector;
    private Vector3 rotateHeldVector;

    private float clickKeyPressTime;
    private float rotateKeyPressTime;
    private float keyHoldDelay = 0.3f;
    #endregion

    #region Angle limits
    private const float minPitch = -90f;
    private const float maxPitch = 0f;
    #endregion

    #region Defaults
    // in ap/ml/dv
    private Vector3 defaultStart = Vector3.zero; // new Vector3(5.4f, 5.7f, 0.332f);
    private float defaultDepth = 0f;
    private Vector2 defaultAngles = new Vector2(-90f, 0f); // 0 yaw is forward, default pitch is 90 degrees down from horizontal, but internally this is a value of 0f
    #endregion

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private float depth;

    private bool _dirty;

    // Offset vectors
    private GameObject probeTipOffset;
    private GameObject probeTipTop;

    // Input system
    ProbeControlInputActions inputActions;

    // References
    [SerializeField] private Transform _probeTipT;
    [FormerlySerializedAs("rotateAround")] [SerializeField] private Transform _rotateAround;

    public override Transform ProbeTipT { get { return _probeTipT; } }

    #region Unity
    private void Awake()
    {
        // Create two points offset from the tip that we'll use to interpolate where we are on the probe
        probeTipOffset = new GameObject(name + "TipOffset");
        probeTipOffset.transform.parent = _probeTipT;
        probeTipOffset.transform.position = _probeTipT.position + _probeTipT.up * 0.2f;

        probeTipTop = new GameObject(name + "TipTop");
        probeTipTop.transform.parent = _probeTipT;
        probeTipTop.transform.position = _probeTipT.position + _probeTipT.up * 10.2f;

        depth = defaultDepth;

        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        inputActions = new();
        var probeControlClick = inputActions.ProbeControl;
        probeControlClick.Enable();

        // Click actions
        probeControlClick.Forward.performed += x => Click(_forwardDir);
        probeControlClick.Forward.canceled += x => CancelClick(_forwardDir);
        probeControlClick.Right.performed += x => Click(_rightDir);
        probeControlClick.Right.canceled += x => CancelClick(_rightDir);
        probeControlClick.Back.performed += x => Click(-_forwardDir);
        probeControlClick.Back.canceled += x => CancelClick(-_forwardDir);
        probeControlClick.Left.performed += x => Click(-_rightDir);
        probeControlClick.Left.canceled += x => CancelClick(-_rightDir);

        probeControlClick.Up.performed += x => Click(_upDir);
        probeControlClick.Up.canceled += x => CancelClick(_upDir);
        probeControlClick.Down.performed += x => Click(-_upDir);
        probeControlClick.Down.canceled += x => CancelClick(-_upDir);

        probeControlClick.DepthDown.performed += x => Click(_depthDir);
        probeControlClick.DepthDown.canceled += x => CancelClick(_depthDir);
        probeControlClick.DepthUp.performed += x => Click(-_depthDir);
        probeControlClick.DepthUp.canceled += x => CancelClick(-_depthDir);

        // Rotate actions
        probeControlClick.YawClockwise.performed += x => Rotate(_yawDir);
        probeControlClick.YawClockwise.canceled += x => CancelRotate(_yawDir);
        probeControlClick.YawCounter.performed += x => Rotate(-_yawDir);
        probeControlClick.YawCounter.canceled += x => CancelRotate(-_yawDir);

        probeControlClick.PitchDown.performed += x => Rotate(_pitchDir);
        probeControlClick.PitchDown.canceled += x => CancelRotate(_pitchDir);
        probeControlClick.PitchUp.performed += x => Rotate(-_pitchDir);
        probeControlClick.PitchUp.canceled += x => CancelRotate(-_pitchDir);

        probeControlClick.RollClock.performed += x => Rotate(_rollDir);
        probeControlClick.RollClock.canceled += x => CancelRotate(_rollDir);
        probeControlClick.RollCounter.performed += x => Rotate(-_rollDir);
        probeControlClick.RollCounter.canceled += x => CancelRotate(-_rollDir);

        // Modifier keys
        probeControlClick.Slow.performed += x => keySlow = true;
        probeControlClick.Slow.canceled += x => keySlow = false;
        probeControlClick.Fast.performed += x => keyFast = true;
        probeControlClick.Fast.canceled += x => keyFast = false;
        probeControlClick.Ultra.performed += x => keyUltra = true;
        probeControlClick.Ultra.canceled += x => keyUltra = false;

        Insertion = new ProbeInsertion(defaultStart, defaultAngles, CoordinateSpaceManager.ActiveCoordinateSpace, CoordinateSpaceManager.ActiveCoordinateTransform);
    }

    private void Start()
    {
        SetProbePosition();
    }

    private void Update()
    {
        // If the user is holding one or more click keys and we are past the hold delay, increment the position
        if (clickKeyHeld > 0 && (Time.realtimeSinceStartup - clickKeyPressTime) > keyHoldDelay)
            MoveProbe_XYZD(clickHeldVector, ComputeMoveSpeed_Hold());

        // If the user is holding one or more rotate keys and we are past the hold delay, increment the angles
        if (rotateKeyHeld > 0 && (Time.realtimeSinceStartup - rotateKeyPressTime) > keyHoldDelay)
            MoveProbe_YPR(rotateHeldVector, ComputeRotSpeed_Hold());
    }

    private void LateUpdate()
    {
        if (_dirty)
        {
            SetProbePosition();
            MovedThisFrameEvent.Invoke();
        }
    }

    private void OnEnable()
    {
        inputActions.ProbeControl.Enable();
    }

    private void OnDisable()
    {
        inputActions.ProbeControl.Disable();
    }

    #endregion

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

    #region Input System

    private float ComputeMoveSpeed_Tap()
    {
        return keyUltra ? MOVE_INCREMENT_TAP_ULTRA :
            keyFast ? MOVE_INCREMENT_TAP_FAST :
            keySlow ? MOVE_INCREMENT_TAP_SLOW :
            MOVE_INCREMENT_TAP;
    }

    private float ComputeMoveSpeed_Hold()
    {
        return (keyUltra ? MOVE_INCREMENT_HOLD_ULTRA :
            keyFast ? MOVE_INCREMENT_HOLD_FAST :
            keySlow ? MOVE_INCREMENT_HOLD_SLOW :
            MOVE_INCREMENT_HOLD) * Time.deltaTime;
    }

    private float ComputeRotSpeed_Tap()
    {
        return keyUltra ? ROT_INCREMENT_TAP_ULTRA :
            keyFast ? ROT_INCREMENT_TAP_FAST :
            keySlow ? ROT_INCREMENT_TAP_SLOW :
            ROT_INCREMENT_TAP;
    }

    private float ComputeRotSpeed_Hold()
    {
        return (keyUltra ? ROT_INCREMENT_HOLD_ULTRA :
            keyFast ? ROT_INCREMENT_HOLD_FAST :
            keySlow ? ROT_INCREMENT_HOLD_SLOW :
            ROT_INCREMENT_HOLD) * Time.deltaTime;
    }

    /// <summary>
    /// Move the probe along a Unity world space 
    /// </summary>
    /// <param name="dir"></param>
    public void Click(Vector4 dir)
    {
        if (dragging) return;

        MoveProbe_XYZD(dir, ComputeMoveSpeed_Tap());
        clickHeldVector += dir;

        // If this is the first key being held, reset the hold timer
        if (clickKeyHeld == 0)
            clickKeyPressTime = Time.realtimeSinceStartup;

        clickKeyHeld++;
    }

    public void CancelClick(Vector4 dir)
    {
        if (dragging) return;

        clickKeyHeld--;
        clickHeldVector -= dir;
    }

    public void Rotate(Vector3 ang)
    {
        if (dragging) return;

        MoveProbe_YPR(ang, ComputeRotSpeed_Tap());

        rotateHeldVector += ang;

        // If this is the first key being held, reset the hold timer
        if (rotateKeyHeld == 0)
            rotateKeyPressTime = Time.realtimeSinceStartup;

        rotateKeyHeld++;
    }

    public void CancelRotate(Vector3 ang)
    {
        if (dragging) return;

        rotateKeyHeld--;
        rotateHeldVector -= ang;
    }

    /// <summary>
    /// Shift the ProbeInsertion position by the Unity World vector in direction, multiplied by the speed
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="speed"></param>
    private void MoveProbe_XYZD(Vector4 direction, float speed)
    {
        // Get the positional delta
        var posDelta = direction * speed;

        if (ManipulatorKeyboardControl)
        {
            // Disable/ignore more input until movement is done
            ManipulatorKeyboardControl = false;

            // Call movement and reset keyboard control when done
            ProbeManager.ManipulatorBehaviorController.MoveByWorldSpaceDelta(posDelta,
                _ => ManipulatorKeyboardControl = true, Debug.LogError);
        }
        else
        {
            // Rotate the position delta (unity world space) into the insertion's transformed space
            // Note that we don't apply the transform beacuse we want 1um steps to = 1um steps in transformed space
            Insertion.apmldv += Insertion.World2TransformedAxisChange(posDelta);
            depth += posDelta.w;
        }

        // Set probe position and update UI
        _dirty = true;
    }

    /// <summary>
    /// Add the angles 
    /// </summary>
    /// <param name="angle">(yaw, pitch, roll)</param>
    /// <param name="speed"></param>
    private void MoveProbe_YPR(Vector3 angle, float speed)
    {
        var angleDelta = angle * speed;

        Insertion.yaw += angleDelta.x;
        Insertion.pitch = Mathf.Clamp(Insertion.pitch + angleDelta.y, minPitch, maxPitch);
        Insertion.roll += angleDelta.z;

        // Set probe position and update UI
        _dirty = true;
    }

    #endregion

    #region Movement Controls

    // Drag movement variables
    private bool axisLockZ;
    private bool axisLockX;
    private bool axisLockY;
    private bool axisLockDepth;
    private bool axisLockPitch;
    private bool axisLockYaw;
    private bool dragging;

    private Vector3 origAPMLDV;
    private float origYaw;
    private float origPitch;

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
        // Cancel movement if being controlled by EphysLink
        if (EventSystem.current.IsPointerOverGameObject() || ProbeManager.IsEphysLinkControlled || Locked)
            return;

        BrainCameraController.BlockBrainControl = true;

        axisLockZ = false;
        axisLockY = false;
        axisLockX = false;
        axisLockDepth = false;
        axisLockPitch = false;
        axisLockYaw = false;

        origAPMLDV = Insertion.apmldv;
        origYaw = Insertion.yaw;
        origPitch = Insertion.pitch;
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
        if (axisLockZ || axisLockY || axisLockX || axisLockDepth || axisLockYaw || axisLockPitch)
            DragMovementClick();
    }

    /// <summary>
    /// Handle probe movements when a user is dragging while keeping the mouse pressed
    /// </summary>
    public void DragMovementDrag()
    {
        // Cancel movement if being controlled by EphysLink
        if (ProbeManager.IsEphysLinkControlled || Locked)
            return;

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
            axisLockYaw = false;
            axisLockPitch = false;
            ProbeManager.SetAxisVisibility(false, false, true, false);
        }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
        {
            CheckForPreviousDragClick();
            axisLockZ = false;
            axisLockX = true;
            axisLockY = false;
            axisLockDepth = false;
            axisLockYaw = false;
            axisLockPitch = false;
            ProbeManager.SetAxisVisibility(true, false, false, false);
        }
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X))
        {
            CheckForPreviousDragClick();
            axisLockZ = false;
            axisLockX = false;
            axisLockY = false;
            axisLockDepth = true;
            axisLockYaw = false;
            axisLockPitch = false;
            ProbeManager.SetAxisVisibility(false, false, false, true);
        }
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.F))
        {
            CheckForPreviousDragClick();
            axisLockZ = false;
            axisLockX = false;
            axisLockY = false;
            axisLockDepth = false;
            axisLockYaw = false;
            axisLockPitch = true;
        }
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
        {
            CheckForPreviousDragClick();
            axisLockZ = false;
            axisLockX = false;
            axisLockY = true;
            axisLockDepth = false;
            axisLockYaw = false;
            axisLockPitch = false;
            ProbeManager.SetAxisVisibility(false, true, false, false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha3))
        {
            CheckForPreviousDragClick();
            axisLockZ = false;
            axisLockX = false;
            axisLockY = false;
            axisLockDepth = false;
            axisLockYaw = true;
            axisLockPitch = false;
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
            Insertion.apmldv = origAPMLDV + Insertion.World2TransformedAxisChange(newXYZ);
        }

        if (axisLockDepth)
        {
            worldOffset = curScreenPointWorld - lastClickPositionWorld;
            lastClickPositionWorld = curScreenPointWorld;
            depth = -1.5f * worldOffset.y;
            moved = true;
        }

        if (axisLockPitch)
        {
            Insertion.pitch = Mathf.Clamp(origPitch + 3f * worldOffset.y, minPitch, maxPitch);
            moved = true;
        }
        if (axisLockYaw)
        {
            Insertion.yaw = origYaw - 3f * worldOffset.x;
            moved = true;
        }


        if (moved)
        {
            SetProbePosition();

            ProbeManager.SetAxisTransform(ProbeTipT);

            ProbeManager.UIUpdateEvent.Invoke();

            MovedThisFrameEvent.Invoke();
        }

    }

    /// <summary>
    /// Release control of mouse movements after the user releases the mouse button from a probe
    /// </summary>
    public void DragMovementRelease()
    {
        // release probe control
        dragging = false;
        ProbeManager.SetAxisVisibility(false, false, false, false);
        BrainCameraController.BlockBrainControl = false;
        FinishedMovingEvent.Invoke();
    }

    #endregion

    #region Set Probe pos/angles
    
    /// <summary>
    /// Set the probe position to the current apml/depth/angles values
    /// </summary>
    public override void SetProbePosition()
    {
        SetProbePositionHelper();
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
    private void SetProbePositionHelper()
    {
        // Reset everything
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        // Manually adjust the coordinates and rotation
        transform.position += Insertion.PositionWorldT();
        transform.RotateAround(_rotateAround.position, transform.up, Insertion.yaw);
        transform.RotateAround(_rotateAround.position, transform.forward, Insertion.pitch);
        transform.RotateAround(_rotateAround.position, _rotateAround.up, Insertion.roll);

        // Compute depth transform, if needed
        if (depth != 0f)
        {
            transform.position += -transform.up * depth;
            Vector3 depthAdjustment = Insertion.World2TransformedAxisChange(-transform.up) * depth;

            Insertion.apmldv += depthAdjustment;
            depth = 0f;
        }

        // update surface position
        ProbeManager.UpdateSurfacePosition();

        // Tell the tpmanager we moved and update the UI elements
        MovedThisFrameEvent.Invoke();
        ProbeManager.UIUpdateEvent.Invoke();
    }

    //public override void SetProbePosition(ProbeInsertion localInsertion)
    //{
    //    // localInsertion gets copied to Insertion
    //    Insertion.apmldv = localInsertion.apmldv;
    //    Insertion.angles = localInsertion.angles;
    //}

    #endregion

    #region Getters

    /// <summary>
    /// Return the tip coordinates in **un-transformed** world coordinates
    /// </summary>
    /// <returns></returns>
    public override (Vector3 tipCoordWorldU, Vector3 tipUpWorldU, Vector3 tipForwardWorldU) GetTipWorldU()
    {
        Vector3 tipCoordWorldU = WorldT2WorldU(_probeTipT.position);
        Vector3 tipUpWorldU = (WorldT2WorldU(_probeTipT.position + _probeTipT.up) - tipCoordWorldU).normalized;
        Vector3 tipForwardWorldU = (WorldT2WorldU(_probeTipT.position + _probeTipT.forward) - tipCoordWorldU).normalized;

        return (tipCoordWorldU, tipUpWorldU, tipForwardWorldU);
    }

    /// <summary>
    /// Convert a transformed world coordinate into an un-transformed coordinate
    /// </summary>
    /// <param name="coordWorldT"></param>
    /// <returns></returns>
    private Vector3 WorldT2WorldU(Vector3 coordWorldT)
    {
        return Insertion.CoordinateSpace.Space2World(Insertion.CoordinateTransform.Transform2Space(Insertion.CoordinateTransform.Space2TransformAxisChange(Insertion.CoordinateSpace.World2Space(coordWorldT))));
    }


    #endregion

}
