using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProbeController : MonoBehaviour
{
    [SerializeField] private float recordHeightSpeed = 0.1f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float rotSpeed = 5f;
    [SerializeField] private List<Collider> probeColliders;
    [SerializeField] private List<ProbeUIManager> probeUIManagers;
    [SerializeField] private Transform rotateAround;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private Renderer probeRenderer;
    [SerializeField] private List<GameObject> recordingRegionGOs;
    [SerializeField] private int probeType;
    [SerializeField] private Transform probeTipT;

    private TrajectoryPlannerManager tpmanager;
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
    private float minPhi = -270f;
    private float maxPhi = 90f;
    private float theta; // rotation around the right axis (x)
    private float minTheta = -90f;
    private float maxTheta = 0f;
    private float spin; // rotation around the probe's own vertical axis (equivalent to phi until the probe is off angle)
    private float minSpin = -180f;
    private float maxSpin = 180f;

    // recording region
    private float minRecordHeight;
    private float maxRecordHeight; // get this by measuring the height of the recording rectangle and subtracting from 10
    private float recordingRegionSizeY;

    // Drag movement variables
    private Vector3 screenPoint;
    private Vector3 offset;

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
        tpmanager = main.GetComponent<TrajectoryPlannerManager>();
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

        foreach (ProbeUIManager puimanager in probeUIManagers)
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
        foreach (ProbeUIManager puimanager in probeUIManagers)
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
        foreach (ProbeUIManager puimanager in probeUIManagers)
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
        bool keyDown = false; // set to true if a key is actually down to move

        // Save the current position information
        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;
        Vector2 preapml = apml;
        float prephi = phi;
        float predepth = depth;
        float pretheta = theta;
        float prespin = spin;

        // Handle click inputs
        if (Input.GetKey(KeyCode.W))
        {
            keyDown = true;
            MoveProbeAPML(1f, 0f);
        }
        if (Input.GetKey(KeyCode.S))
        {
            keyDown = true;
            MoveProbeAPML(-1f, 0f);
        }
        if (Input.GetKey(KeyCode.D))
        {
            keyDown = true;
            MoveProbeAPML(0f, 1f);
        }
        if (Input.GetKey(KeyCode.A))
        {
            keyDown = true;
            MoveProbeAPML(0f, -1f);
        }
        if (Input.GetKey(KeyCode.Z))
        {
            keyDown = true;
            MoveProbeDepth(1f);
        }
        if (Input.GetKey(KeyCode.X))
        {
            keyDown = true;
            MoveProbeDepth(-1f);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            keyDown = true;
            RotateProbe(-1f, 0f);
        }
        if (Input.GetKey(KeyCode.E))
        {
            keyDown = true;
            RotateProbe(1f, 0f);
        }
        if (Input.GetKey(KeyCode.R))
        {
            keyDown = true;
            RotateProbe(0f, 1f);
        }
        if (Input.GetKey(KeyCode.F))
        {
            keyDown = true;
            RotateProbe(0f, -1f);
        }
        if (Input.GetKey(KeyCode.T))
        {
            keyDown = true;
            ShiftRecordingRegion(1f);
        }
        if (Input.GetKey(KeyCode.G))
        {
            keyDown = true;
            ShiftRecordingRegion(-1f);
        }
        if (Input.GetKey(KeyCode.Alpha1))
        {
            keyDown = true;
            SpinProbe(-1f);
        }
        if (Input.GetKey(KeyCode.Alpha3))
        {
            keyDown = true;
            SpinProbe(1f);
        }

        if (keyDown)
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

                foreach (ProbeUIManager puimanager in probeUIManagers)
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

        foreach (ProbeUIManager puimanager in probeUIManagers)
            puimanager.ProbeMoved();
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

    public void MoveProbeAPML(float ap, float ml)
    {
        Vector2 delta = Input.GetKey(KeyCode.LeftShift) ? new Vector2(ap, ml) * moveSpeed * Time.deltaTime * 5 : new Vector2(ap, ml) * moveSpeed * Time.deltaTime;
        apml += delta;
    }

    public void MoveProbeDepth(float depth)
    {
        float depthDelta = Input.GetKey(KeyCode.LeftShift) ? depth * Time.deltaTime * moveSpeed * 5 : depth * Time.deltaTime * moveSpeed;
        this.depth += depthDelta;
    }

    public void RotateProbe(float phi, float theta)
    {
        float phiDelta = Input.GetKey(KeyCode.LeftShift) ? phi * Time.deltaTime * rotSpeed * 10 : phi * Time.deltaTime * rotSpeed;
        float thetaDelta = Input.GetKey(KeyCode.LeftShift) ? theta * Time.deltaTime * rotSpeed * 10 : theta * Time.deltaTime * rotSpeed;
        this.phi = Mathf.Clamp(this.phi + phiDelta, minPhi, maxPhi);
        this.theta = Mathf.Clamp(this.theta + thetaDelta, minTheta, maxTheta);
    }

    public void SpinProbe(float spin)
    {
        float spinDelta = Input.GetKey(KeyCode.LeftShift) ? spin * Time.deltaTime * rotSpeed * 10 : spin * Time.deltaTime * rotSpeed;
        this.spin = Mathf.Clamp(this.spin + spinDelta, minSpin, maxSpin);
    }

    public void UpdateText()
    {
        float localDepth = GetLocalDepth();
        Vector2 apml_local = GetAPML();
        string[] apml_string = GetAPMLStr();

        Vector2 iblPhiTheta = tpmanager.World2IBL(new Vector2(phi, theta));

        string updateStr = string.Format("Probe #{0}: "+apml_string[0]+" {1} "+apml_string[1]+" {2} Azimuth {3} Elevation {4} "+ GetDepthStr() + " {5} Spin {6}",
            probeID, round0(apml_local.x*1000), round0(apml_local.y * 1000), round2(iblPhiTheta.x), round2(iblPhiTheta.y), round0(localDepth*1000), round2(spin));

        textUI.text = updateStr;
    }

    private void Probe2Text()
    {
        float localDepth = GetLocalDepth();
        Vector2 apml_local = GetAPML();
        string[] apml_string = GetAPMLStr();

        Vector2 iblPhiTheta = tpmanager.World2IBL(new Vector2(phi, theta));

        string fullStr = string.Format("Probe #{0}: " + apml_string[0] + " {1} " + apml_string[1] + " {2} Azimuth {3} Elevation {4} "+ GetDepthStr()+" {5} Spin {6} Record Height {7}",
            probeID, apml_local.x * 1000, apml_local.y * 1000, iblPhiTheta.x, iblPhiTheta.y, localDepth, spin, minRecordHeight * 1000);
        GUIUtility.systemCopyBuffer = fullStr;

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

    public void DragMovementClick()
    {
        tpmanager.ProbeControl = true;

        // Track the screenPoint that was initially clicked
        screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);

        offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));

    }
    public void DragMovementDrag()
    {
        // Only handle mouse drags if probe control is turned on
        if (tpmanager.ProbeControl)
        {
            Vector3 curScreenPoint = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
        }

    }

    public void DragMovementRelease()
    {
        // release probe control
        tpmanager.ProbeControl = false;
    }

    public void ResizeProbePanel(int newPxHeight)
    {
        foreach (ProbeUIManager puimanager in probeUIManagers)
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
