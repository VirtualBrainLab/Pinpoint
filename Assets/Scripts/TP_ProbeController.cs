using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TP_ProbeController : MonoBehaviour
{
    // MOVEMENT CONTROLS
    [SerializeField] private float recordHeightSpeed = 0.1f;
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

    private bool keyFast = false;
    private bool keySlow = false;
    private bool keyHeld = false; // If a key is held, we will skip re-checking the key hold delay for any other keys that are added
    private float keyPressTime = 0f;
    [SerializeField] private float keyHoldDelay = 0.300f;

    // DRAG MOVEMENT CONTROLS
    private bool draggingMovement = false;


    [SerializeField] private List<Collider> probeColliders;
    [SerializeField] private List<TP_ProbeUIManager> probeUIManagers;
    [SerializeField] private Transform rotateAround;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private Renderer probeRenderer;
    [SerializeField] private List<GameObject> recordingRegionGOs;
    [SerializeField] private int probeType;
    [SerializeField] private Transform probeTipT;

    private TP_TrajectoryPlannerManager tpmanager;
    private Utils util;

    // in ap/ml/dv
    private Vector3 iblBregma = new Vector3(5.4f, 5.739f, 0.332f);
    private Vector2 defaultAngles = new Vector2(-90f, 0f); // 0 phi is forward, default theta is 90 degrees down from horizontal, but internally this is a value of 0f
    private Vector3 invivoConversionAPMLDV = new Vector3(1.087f, 1f, 0.952f);
    private Vector3 invivoConversionPhiThetaBeta = new Vector3(0f, 0f, 0f);

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

    // Coordinate system information
    private CCFCoordinateSystem ccfPosition;

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
    private List<TP_ProbeCollider> visibleColliders;

    // Text button
    GameObject textGO;
    Button textButton;

    private void Awake()
    {
        textGO = Instantiate(textPrefab, GameObject.Find("CoordinatePanel").transform);
        textButton = textGO.GetComponent<Button>();
        textButton.onClick.AddListener(Probe2Text);
        textUI = textGO.GetComponent<TextMeshProUGUI>();

        GameObject main = GameObject.Find("main");
        tpmanager = main.GetComponent<TP_TrajectoryPlannerManager>();
        tpmanager.RegisterProbe(this, probeColliders);

        annotationDataset = tpmanager.GetAnnotationDataset();
        ccfBounds = tpmanager.CCFCollider();

        util = main.GetComponent<Utils>();

        visibleColliders = new List<TP_ProbeCollider>();

        UpdateRecordingRegionVars();
    }

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        ResetPositionTracking();
        SetProbePosition();

        foreach (TP_ProbeUIManager puimanager in probeUIManagers)
            puimanager.ProbeMoved();
    }

    public int GetProbeType()
    {
        return probeType;
    }

    public Transform GetTipTransform()
    {
        return probeTipT;
    }

    public void Destroy()
    {
        // Delete this gameObject
        foreach (TP_ProbeUIManager puimanager in probeUIManagers)
            puimanager.Destroy();
        Destroy(textGO);
    }

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
        foreach (TP_ProbeUIManager puimanager in probeUIManagers)
            puimanager.ProbeMoved();
    }

    public void ResetPosition()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        ResetPositionTracking();
        UpdateText();
    }

    public void ResetPositionTracking()
    {
        apml = new Vector2();
        depth = 0;
        phi = defaultAngles.x;
        theta = defaultAngles.y;
        spin = 0;
    }

    public bool MoveProbe(List<Collider> otherColliders)
    {

        bool moved = false;
        bool keyHoldDelayPassed = (Time.realtimeSinceStartup - keyPressTime) > keyHoldDelay;

        keyFast = Input.GetKey(KeyCode.LeftShift);
        keySlow = Input.GetKey(KeyCode.LeftControl);

        // Save the current position information
        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;
        Vector2 preapml = apml;
        float prephi = phi;
        float predepth = depth;
        float pretheta = theta;
        float prespin = spin;

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
            SetProbePosition(apml.x, apml.y, depth, phi, theta, spin);

            bool collided = CheckCollisions(otherColliders);
            if (collided)
            {
                transform.position = originalPosition;
                transform.rotation = originalRotation;
                apml = preapml;
                depth = predepth;
                phi = prephi;
                theta = pretheta;
                spin = prespin;
            }
            else
            {
                if (visibleColliders.Count > 0)
                    ClearCollisionMesh();
                UpdateSurfacePosition();

                foreach (TP_ProbeUIManager puimanager in probeUIManagers)
                    puimanager.ProbeMoved();
            }

            return !collided;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// RECORDING REGION CONTROLS
    /// </summary>
    private void UpdateRecordingRegionVars()
    {
        minRecordHeight = recordingRegionGOs[0].transform.localPosition.y;
        maxRecordHeight = minRecordHeight + (10 - recordingRegionGOs[0].transform.localScale.y);
    }

    public float GetRecordingRegionSize()
    {
        return recordingRegionSizeY;
    }

    private void ShiftRecordingRegion(float dir)
    {
        foreach (GameObject recordingRegion in recordingRegionGOs)
        {
            Vector3 localPosition = recordingRegion.transform.localPosition;
            float localRecordHeightSpeed = Input.GetKey(KeyCode.LeftShift) ? recordHeightSpeed * 2 : recordHeightSpeed;
            localPosition.y = Mathf.Clamp(localPosition.y + dir * localRecordHeightSpeed, minRecordHeight, maxRecordHeight);
            recordingRegion.transform.localPosition = localPosition;
        }
    }

    public float[] GetRecordingRegionHeight()
    {
        float[] heightPercs = new float[2];
        heightPercs[0] = (recordingRegionGOs[0].transform.localPosition.y - minRecordHeight) / (maxRecordHeight - minRecordHeight);
        heightPercs[1] = recordingRegionSizeY;
        return heightPercs;
    }

    /// <summary>
    /// ANGLE CONVERSION FUNCTIONS
    /// Because the IBL coordinate system is different from Unity's, we need to be able to convert back and forth
    /// </summary>
    

    /// <summary>
    /// MANUAL COORDINATE ENTRY + PROBE POSITION CONTROLS
    /// </summary>

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

        Vector2 worldPhiTheta = tpmanager.IBL2World(new Vector2(phi, theta));

        SetProbePosition(ap/1000, ml / 1000, depth / 1000, worldPhiTheta.x, worldPhiTheta.y, spin);
        UpdateSurfacePosition();

        tpmanager.SetMovedThisFrame();
        foreach (TP_ProbeUIManager puimanager in probeUIManagers)
            puimanager.ProbeMoved();
    }

    public IEnumerator DelayedManualCoordinateEntry(float delay, float ap, float ml, float depth, float phi, float theta, float spin)
    {
        yield return new WaitForSeconds(delay);
        ManualCoordinateEntry(ap, ml, depth, phi, theta, spin);
    }

    public void SetProbePosition()
    {
        SetProbePosition(apml.x, apml.y, depth, phi, theta, spin);
    }

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
    /// CHECK FOR COLLISIONS
    /// </summary>
    /// <param name="otherColliders"></param>
    /// <returns></returns>
    private bool CheckCollisions(List<Collider> otherColliders)
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

    private void CreateCollisionMesh(Collider activeCollider, Collider otherCollider)
    {
        TP_ProbeCollider activeProbCollider = activeCollider.gameObject.GetComponent<TP_ProbeCollider>();
        if (activeProbCollider != null)
        {
            visibleColliders.Add(activeProbCollider);
            activeProbCollider.SetVisibility(true);
        }
        TP_ProbeCollider otherProbeCollider = otherCollider.gameObject.GetComponent<TP_ProbeCollider>();
        if (otherProbeCollider != null)
        {
            otherProbeCollider.SetVisibility(true);
            visibleColliders.Add(otherProbeCollider);
        }
    }

    private void ClearCollisionMesh()
    {
        foreach (TP_ProbeCollider probeCollider in visibleColliders)
            probeCollider.SetVisibility(false);
        visibleColliders.Clear();
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
        if (tpmanager.GetStereotaxic())
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
        if (tpmanager.GetStereotaxic())
            return "stDepth";
        else
            return "ccfDepth";
    }

    private Vector2 GetAPML()
    {
        Vector2 localAPML;
        // If we're in stereotaxic coordinates, apply the conversion factors first, then deal with rotating in 3D space
        if (tpmanager.GetStereotaxic())
        {
            localAPML = new Vector2(apml.x * invivoConversionAPMLDV.x, apml.y * invivoConversionAPMLDV.y);
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
        if (tpmanager.GetStereotaxic())
        {
            if (tpmanager.GetDepthFromBrain())
            {
                if (probeInBrain)
                {
                    float depthDir = Mathf.Sign(Vector3.Dot(probeTipT.transform.position - brainSurface, -probeTipT.transform.up));
                    // Separately obtain the x/y/z components relative to the surface point, scale them, then get the distance
                    Vector3 distanceToSurface = probeTipT.transform.position - brainSurface;
                    distanceToSurface = new Vector3(distanceToSurface.x * invivoConversionAPMLDV.y, distanceToSurface.y * invivoConversionAPMLDV.z, distanceToSurface.z * invivoConversionAPMLDV.x);

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

    private Vector2 origAPML;
    private float origDepth;
    private Vector3 originalClickPositionWorld;
    private float cameraDistance;

    public void DragMovementClick()
    {
        tpmanager.ProbeControl = true;

        axisLockAP = false;
        axisLockDV = false;
        axisLockML = false;

        origAPML = apml;
        origDepth = depth;

        // Track the screenPoint that was initially clicked
        cameraDistance = Vector3.Distance(Camera.main.transform.position, gameObject.transform.position);
        originalClickPositionWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));
    }
    public void DragMovementDrag()
    {
        Vector3 curScreenPointWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
        {
            axisLockAP = true;
            axisLockML = false;
            axisLockDV = false;
            // To make it more smooth, reset the z axis to zero when you lock the axis
            originalClickPositionWorld.z = curScreenPointWorld.z;
            origAPML = apml;
            origDepth = depth;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            axisLockAP = false;
            axisLockML = true;
            axisLockDV = false;
            originalClickPositionWorld.x = curScreenPointWorld.x;
            origAPML = apml;
            origDepth = depth;
        }
        if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.X))
        {
            axisLockAP = false;
            axisLockML = false;
            axisLockDV = true;
            originalClickPositionWorld.y = curScreenPointWorld.y;
            origAPML = apml;
            origDepth = depth;
        }

        Vector3 worldOffset = curScreenPointWorld - originalClickPositionWorld;

        // debug call:
        //GameObject.Find("world_space_probe_hit").transform.position = curScreenPointWorld;

        if (axisLockAP)
        {
            apml.x = origAPML.x;
            apml.x += -worldOffset.z;
        }
        if (axisLockML)
        {
            apml.y = origAPML.y;
            apml.y += -worldOffset.x;
        }
        if (axisLockDV)
        {
            depth = origDepth;
            depth += -worldOffset.y;
        }

        if (apml.x != origAPML.x || apml.y != origAPML.y || depth != origDepth)
        {
            tpmanager.UpdateInPlaneView();
            SetProbePosition(apml.x, apml.y, depth, phi, theta, spin);
            UpdateSurfacePosition();

            foreach (TP_ProbeUIManager puimanager in probeUIManagers)
                puimanager.ProbeMoved();

            tpmanager.SetMovedThisFrame();
        }

    }

    public void DragMovementRelease()
    {
        // release probe control
        tpmanager.ProbeControl = false;
    }

    public void ResizeProbePanel(int newPxHeight)
    {
        foreach (TP_ProbeUIManager puimanager in probeUIManagers)
        {
            puimanager.ResizeProbePanel(newPxHeight);
            puimanager.ProbeMoved();
        }
    }

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

            Vector3 entry_apdvlr = util.WorldSpace2apdvlr(entryPoint + tpmanager.GetCenterOffset());
            Vector3 exit_apdvlr = util.WorldSpace2apdvlr(exitPoint + tpmanager.GetCenterOffset());
            //GameObject.Find("entry").transform.position = util.apdvlr2World(entry_apdvlr) - tpmanager.GetCenterOffset();
            //GameObject.Find("exit").transform.position = util.apdvlr2World(exit_apdvlr) - tpmanager.GetCenterOffset();

            // This sort of has to work, but it isn't super efficient, might be ways to speed this up?
            for (float perc = 0; perc <= 1; perc += 0.002f)
            {
                Vector3 point = Vector3.Lerp(entry_apdvlr, exit_apdvlr, perc);
                if (annotationDataset.ValueAtIndex(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), Mathf.RoundToInt(point.z)) > 0)
                {
                    brainSurface = util.apdvlr2World(point) - tpmanager.GetCenterOffset();
                    probeInBrain = true;
                    //GameObject.Find("surface").transform.position = brainSurface;
                    return;
                }
            }
            // If we get here we failed to find a point
            Debug.LogWarning("There is an entry and exit but we couldn't find the brain edge");
        }
    }
}
