using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class TP_TrajectoryPlannerManager : MonoBehaviour
{
    [SerializeField] private CCFModelControl modelControl;
    [SerializeField] private List<GameObject> probePrefabs;
    [SerializeField] private List<int> probePrefabIDs;
    [SerializeField] private Transform brainModel;
    [SerializeField] private Utils util;
    [SerializeField] private TP_RecRegionSlider recRegionSlider;
    [SerializeField] private Collider ccfCollider;
    [SerializeField] private TP_InPlaneSlice inPlaneSlice;
    [SerializeField] private TP_SliceRenderer sliceRenderer;
    [SerializeField] private TP_Search searchControl;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private GameObject CollisionPanelGO;

    [SerializeField] private TP_PlayerPrefs localPrefs;

    // Which acronym/area name set to use
    [SerializeField] TMP_Dropdown annotationAcronymDropdown;

    private TP_ProbeController activeProbeController;

    private List<TP_ProbeController> allProbes;
    private List<Collider> inactiveProbeColliders;
    private List<Collider> allProbeColliders;
    private List<Collider> rigColliders;
    private List<Collider> allNonActiveColliders;

    Color[] probeColors;

    // Values
    [SerializeField] private int probePanelAcronymTextFontSize = 14;
    [SerializeField] private int probePanelAreaTextFontSize = 10;

    // Coord data
    private Vector3 centerOffset = new Vector3(-5.7f, -4.0f, +6.6f);

    // Manual coordinate entry
    [SerializeField] private TP_CoordinateEntryPanel manualCoordinatePanel;

    // Track who got clicked on, probe, camera, or brain
    public bool ProbeControl { get; set; }
    public bool CameraControl { get; set; }
    public bool BrainControl { get; set; }

    // Track when brain areas get clicked on
    private List<int> targetedBrainAreas;

    private bool movedThisFrame;
    private bool spawnedThisFrame = false;

    private int visibleProbePanels;

    private void Awake()
    {
        ProbeControl = false;
        CameraControl = false;
        BrainControl = false;

        visibleProbePanels = 0;

        allProbes = new List<TP_ProbeController>();
        allProbeColliders = new List<Collider>();
        inactiveProbeColliders = new List<Collider>();
        rigColliders = new List<Collider>();
        allNonActiveColliders = new List<Collider>();
        targetedBrainAreas = new List<int>();
        //Physics.autoSyncTransforms = true;

        probeColors = new Color[20] { ColorFromRGB(114, 87, 242), ColorFromRGB(240, 144, 96), ColorFromRGB(71, 147, 240), ColorFromRGB(240, 217, 48), ColorFromRGB(60, 240, 227),
                                    ColorFromRGB(180, 0, 0), ColorFromRGB(0, 180, 0), ColorFromRGB(0, 0, 180), ColorFromRGB(180, 180, 0), ColorFromRGB(0, 180, 180),
                                    ColorFromRGB(180, 0, 180), ColorFromRGB(240, 144, 96), ColorFromRGB(71, 147, 240), ColorFromRGB(240, 217, 48), ColorFromRGB(60, 240, 227),
                                    ColorFromRGB(114, 87, 242), ColorFromRGB(255, 255, 255), ColorFromRGB(0, 125, 125), ColorFromRGB(125, 0, 125), ColorFromRGB(125, 125, 0)};

        modelControl.LateStart(true);

        LoadAnnotationDataset();

        DelayedModelControlStart();
    }

    private async void DelayedModelControlStart()
    {
        await modelControl.GetDefaultLoaded();

        foreach (CCFTreeNode node in modelControl.GetDefaultLoadedNodes())
        {
            node.SetNodeModelVisibility(true);
            Transform nodeT = node.GetNodeTransform();
            // I don't know why this has to happen, somewhere these are getting set incorrectly?
            nodeT.localPosition = Vector3.zero;
            nodeT.localRotation = Quaternion.identity;
        }
    }


    // Annotations
    [SerializeField] private AssetReference dataIndexes;
    private byte[] datasetIndexes_bytes;
    [SerializeField] private AssetReference annotationIndexes;
    private ushort[] annotationIndexes_shorts;
    [SerializeField] private AssetReference annotationMap;
    private uint[] annotationMap_ints;
    private int annotationDatasetWait = 0;
    private AnnotationDataset annotationDataset;

    /// <summary>
    /// Loads the annotation dataset files from their Addressable AssetReference objects
    /// 
    /// Asynchronous dependencies: inPlaneSlice, localPrefs
    /// </summary>
    private void LoadAnnotationDataset()
    {
        dataIndexes.LoadAssetAsync<TextAsset>().Completed += handle =>
        {
            datasetIndexes_bytes = handle.Result.bytes;
            annotationDatasetWait++;
            if (annotationDatasetWait >= 3)
                LoadAnnotationDatasetCompleted();
            Addressables.Release(handle);
        };
        annotationIndexes.LoadAssetAsync<TextAsset>().Completed += handle =>
        {
            annotationIndexes_shorts = new ushort[handle.Result.bytes.Length / 2];
            Buffer.BlockCopy(handle.Result.bytes, 0, annotationIndexes_shorts, 0, handle.Result.bytes.Length);
            annotationDatasetWait++;
            if (annotationDatasetWait >= 3)
                LoadAnnotationDatasetCompleted();
            Addressables.Release(handle);
        };
        annotationMap.LoadAssetAsync<TextAsset>().Completed += handle =>
        {
            annotationMap_ints = new uint[handle.Result.bytes.Length / 4];
            Buffer.BlockCopy(handle.Result.bytes, 0, annotationMap_ints, 0, handle.Result.bytes.Length);
            annotationDatasetWait++;
            if (annotationDatasetWait >= 3)
                LoadAnnotationDatasetCompleted();
            Addressables.Release(handle);
        };

        Debug.Log("Annotation dataset loading");
    }

    /// <summary>
    /// Called asynchronously when LoadAnnotationDataset loads all three indexes. Builds the dataset object and calls dependencies.
    /// </summary>
    private void LoadAnnotationDatasetCompleted()
    {
        Debug.Log("Annotation dataset loaded");
        annotationDataset = new AnnotationDataset(annotationIndexes_shorts, annotationMap_ints, datasetIndexes_bytes);
        annotationIndexes_shorts = null;
        annotationMap_ints = null;
        datasetIndexes_bytes = null;
        inPlaneSlice.StartAnnotationDataset();

        //sliceRenderer.AsyncStart();

        // Re-spawn previously active probes as the *final* step in loading
        localPrefs.AsyncStart();
    }

    public void ClickSearchArea(GameObject target)
    {
        searchControl.ClickArea(target);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public void ToggleBeryl(int value)
    {
        switch (value)
        {
            case 0:
                modelControl.SetBeryl(false);
                break;
            case 1:
                modelControl.SetBeryl(true);
                break;
            default:
                modelControl.SetBeryl(false);
                break;
        }
        foreach (TP_ProbeController probeController in allProbes)
            foreach (TP_ProbeUIManager puimanager in probeController.GetComponents<TP_ProbeUIManager>())
                puimanager.ProbeMoved();
    }

    public Collider CCFCollider()
    {
        return ccfCollider;
    }

    public int ProbePanelTextFS(bool acronym)
    {
        return acronym ? probePanelAcronymTextFontSize : probePanelAreaTextFontSize;
    }
    
    public Vector3 GetCenterOffset()
    {
        return centerOffset;
    }

    public AnnotationDataset GetAnnotationDataset()
    {
        return annotationDataset;
    }

    public int GetActiveProbeType()
    {
        return activeProbeController.GetProbeType();
    }

    // Update is called once per frame
    void Update()
    {
        movedThisFrame = false;

        if (spawnedThisFrame)
        {
            spawnedThisFrame = false;
            return;
        }

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) && Input.GetKeyDown(KeyCode.Backspace) && !manualCoordinatePanel.gameObject.activeSelf)
        {
            RecoverActiveProbeController();
            return;
        }

        if (Input.anyKey && activeProbeController != null && !searchInput.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.Backspace) && !manualCoordinatePanel.gameObject.activeSelf)
            {
                DestroyActiveProbeController();
                return;
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                manualCoordinatePanel.gameObject.SetActive(!manualCoordinatePanel.gameObject.activeSelf);
                if (manualCoordinatePanel.gameObject.activeSelf)
                    manualCoordinatePanel.SetTextValues(activeProbeController);
            }

            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(2))
            {
                movedThisFrame = localPrefs.GetCollisions() ? activeProbeController.MoveProbe(true) : activeProbeController.MoveProbe(false);
            }

            if (movedThisFrame)
                inPlaneSlice.UpdateInPlaneSlice();
        }
    }

    public List<TP_ProbeController> GetAllProbes()
    {
        return allProbes;
    }

    public List<Collider> GetAllNonActiveColliders()
    {
        return allNonActiveColliders;
    }

    public bool GetCollisions()
    {
        return localPrefs.GetCollisions();
    }

    // DESTROY AND REPLACE PROBES

    //[TODO] Replace this with some system that handles recovering probes by tracking their coordinate system or something?
    // Or maybe the probe coordinates should be an object that can be serialized?
    private List<float> prevCoordinates;
    private int prevProbeType;

    private void DestroyActiveProbeController()
    {
        prevProbeType = activeProbeController.GetProbeType();
        prevCoordinates = activeProbeController.GetCoordinates();

        Debug.Log("Destroying probe type " + prevProbeType + " with coordinates");
        foreach (float val in prevCoordinates)
            Debug.Log(val);

        activeProbeController.Destroy();
        Destroy(activeProbeController.gameObject);
        allProbes.Remove(activeProbeController);
        if (allProbes.Count > 0)
            SetActiveProbe(allProbes[0]);
        else
            activeProbeController = null;
    }

    private void RecoverActiveProbeController()
    {
        AddNewProbe(prevProbeType, prevCoordinates[0], prevCoordinates[1], prevCoordinates[2], prevCoordinates[3], prevCoordinates[4], prevCoordinates[5]);
    }

    public void ManualCoordinateEntry(float ap, float ml, float depth, float phi, float theta, float spin)
    {
        activeProbeController.ManualCoordinateEntry(ap, ml, depth, phi, theta, spin);
    }

    public void AddIBLProbes()
    {
        // Add two probes to the scene, one coming from the left and one coming from the right
        StartCoroutine(DelayedIBLProbeAdd(0, 45, 0f));
        StartCoroutine(DelayedIBLProbeAdd(180, 45, 0.2f));
    }

    IEnumerator DelayedIBLProbeAdd(float phi, float theta, float delay)
    {
        yield return new WaitForSeconds(delay);
        AddNewProbe(1);
        yield return new WaitForSeconds(0.05f);
        activeProbeController.SetProbePosition(0, 0, 0, phi, theta, 0);
    }

    IEnumerator DelayedMoveAllProbes()
    {
        yield return new WaitForSeconds(0.05f);
        movedThisFrame = true;
        MoveAllProbes();
    }

    public void AddNewProbeVoid(int probeType)
    {
        AddNewProbe(probeType);
    }
    public TP_ProbeController AddNewProbe(int probeType)
    {
        CountProbePanels();
        if (visibleProbePanels >= 16)
            return null;

        GameObject newProbe = Instantiate(probePrefabs[probePrefabIDs.FindIndex(x => x == probeType)], brainModel);
        SetActiveProbe(newProbe.GetComponent<TP_ProbeController>());
        if (visibleProbePanels > 4)
            activeProbeController.ResizeProbePanel(700);

        RecalculateProbePanels();

        spawnedThisFrame = true;
        StartCoroutine(DelayedMoveAllProbes());

        return newProbe.GetComponent<TP_ProbeController>();
    }
    public TP_ProbeController AddNewProbe(int probeType, float ap, float ml, float depth, float phi, float theta, float spin)
    {
        TP_ProbeController probeController = AddNewProbe(probeType);
        StartCoroutine(probeController.DelayedManualCoordinateEntry(0.1f, ap, ml, depth, phi, theta, spin));

        return probeController;
    }

    private void CountProbePanels()
    {
        visibleProbePanels = GameObject.FindGameObjectsWithTag("ProbePanel").Length;
    }

    private void RecalculateProbePanels()
    {
        CountProbePanels();

        if (visibleProbePanels > 8)
        {
            // Increase the layout to have 8 columns and two rows
            GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>().constraintCount = 8;
        }
        else if (visibleProbePanels > 4)
        {
            // Increase the layout to have two rows, by shrinking all the ProbePanel objects to be 500 pixels tall
            GridLayoutGroup probePanelParent = GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>();
            Vector2 cellSize = probePanelParent.cellSize;
            cellSize.y = 700;
            probePanelParent.cellSize = cellSize;

            // now resize all existing probeUIs to be 700 tall
            foreach (TP_ProbeController probeController in allProbes)
            {
                probeController.ResizeProbePanel(700);
            }
        }
    }

    public void RegisterProbe(TP_ProbeController probeController, List<Collider> colliders)
    {
        Debug.Log("Registering probe: " + probeController.gameObject.name);
        allProbes.Add(probeController);
        probeController.RegisterProbeCallback(allProbes.Count, probeColors[allProbes.Count-1]);
        foreach (Collider collider in colliders)
            allProbeColliders.Add(collider);
    }

    public void SetActiveProbe(TP_ProbeController newActiveProbeController)
    {
        Debug.Log("Setting active probe to: " + newActiveProbeController.gameObject.name);
        activeProbeController = newActiveProbeController;

        foreach (TP_ProbeUIManager puimanager in activeProbeController.gameObject.GetComponents<TP_ProbeUIManager>())
            puimanager.ProbeSelected(true);

        foreach (TP_ProbeController pcontroller in allProbes)
            if (pcontroller != activeProbeController)
                foreach (TP_ProbeUIManager puimanager in pcontroller.gameObject.GetComponents<TP_ProbeUIManager>())
                    puimanager.ProbeSelected(false);

        inactiveProbeColliders = new List<Collider>();
        List<Collider> activeProbeColliders = activeProbeController.GetProbeColliders();
        foreach (Collider collider in allProbeColliders)
            if (!activeProbeColliders.Contains(collider))
                inactiveProbeColliders.Add(collider);
        UpdateNonActiveColliders();

        // Also update the recording region size slider
        recRegionSlider.SliderValueChanged(activeProbeController.GetRecordingRegionSize());
    }

    public void ResetActiveProbe()
    {
        activeProbeController.ResetPosition();
    }

    public Color GetProbeColor(int probeID)
    {
        return probeColors[probeID];
    }

    public TP_ProbeController GetActiveProbeController()
    {
        return activeProbeController;
    }

    public bool MovedThisFrame()
    {
        return movedThisFrame;
    }

    public void SetMovedThisFrame()
    {
        movedThisFrame = true;
    }

    public void UpdateInPlaneView()
    {
        inPlaneSlice.UpdateInPlaneSlice();
    }

    public void UpdateRigColliders(List<Collider> newRigColliders, bool keep)
    {
        if (keep)
            foreach (Collider collider in newRigColliders)
                rigColliders.Add(collider);
        else
            foreach (Collider collider in newRigColliders)
                rigColliders.Remove(collider);
        UpdateNonActiveColliders();
    }

    private void UpdateNonActiveColliders()
    {
        allNonActiveColliders.Clear();
        foreach (Collider collider in inactiveProbeColliders)
            allNonActiveColliders.Add(collider);
        foreach (Collider collider in rigColliders)
            allNonActiveColliders.Add(collider);
    }

    private void MoveAllProbes()
    {
        foreach(TP_ProbeController probeController in allProbes)
            foreach (TP_ProbeUIManager puimanager in probeController.GetComponents<TP_ProbeUIManager>())
                puimanager.ProbeMoved();
    }


    public void SelectBrainArea(int id)
    {
        if (targetedBrainAreas.Contains(id))
        {
            ClearTargetedBrainArea(id);
            targetedBrainAreas.Remove(id);
        }
        else
        {
            TargetBrainArea(id);
            targetedBrainAreas.Add(id);
        }
    }

    private void TargetBrainArea(int id)
    {
        modelControl.ChangeMaterial(id, "lit");
    }

    private void ClearTargetedBrainArea(int id)
    {
        modelControl.ChangeMaterial(id, "default");
    }


    ///
    /// HELPER FUNCTIONS
    /// 
    public Color ColorFromRGB(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    public Vector2 World2IBL(Vector2 phiTheta)
    {
        float iblPhi = -phiTheta.x - 90f;
        float iblTheta = -phiTheta.y;
        return new Vector2(iblPhi, iblTheta);
    }

    public Vector2 IBL2World(Vector2 iblPhiTheta)
    {
        float worldPhi = -iblPhiTheta.x - 90f;
        float worldTheta = -iblPhiTheta.y;
        return new Vector2(worldPhi, worldTheta);
    }

    ///
    /// SETTINGS
    /// 

    public void SetBackgroundWhite(bool state)
    {
        if (state)
            Camera.main.backgroundColor = Color.white;
        else
            Camera.main.backgroundColor = Color.black;
    }

    public void SetRecordingRegion(bool state)
    {
        localPrefs.SetRecordingRegionOnly(state);
        MoveAllProbes();
    }

    public bool RecordingRegionOnly()
    {
        return localPrefs.GetRecordingRegionOnly();
    }

    public void SetAcronyms(bool state)
    {
        localPrefs.SetAcronyms(state);
        searchControl.RefreshSearchWindow();
        // move probes to update state
        MoveAllProbes();
    }

    public bool UseAcronyms()
    {
        return localPrefs.GetAcronyms();
    }

    public void SetDepth(bool state)
    {
        localPrefs.SetDepthFromBrain(state);
        foreach (TP_ProbeController probeController in allProbes)
            probeController.UpdateText();
    }
    public bool GetDepthFromBrain()
    {
        return localPrefs.GetDepthFromBrain();
    }

    public void SetConvertToProbe(bool state)
    {
        localPrefs.SetAPML2ProbeAxis(state);
        foreach (TP_ProbeController probeController in allProbes)
            probeController.UpdateText();
    }

    public bool GetConvertAPML2Probe()
    {
        return localPrefs.GetAPML2ProbeAxis();
    }

    public void SetCollisions(bool toggleCollisions)
    {
        localPrefs.SetCollisions(toggleCollisions);
    }

    public void SetCollisionPanelVisibility(bool visibility)
    {
        CollisionPanelGO.SetActive(visibility);
    }

    public void SetBregma(bool useBregma)
    {
        localPrefs.SetBregma(useBregma);

        foreach (TP_ProbeController pcontroller in allProbes)
            pcontroller.SetProbePosition();
    }

    public void SetInPlane(bool state)
    {
        localPrefs.SetInplane(state);
        inPlaneSlice.UpdateInPlane();
    }

    public bool GetBregma()
    {
        return localPrefs.GetBregma();
    }

    public void SetStereotaxic(bool state)
    {
        localPrefs.SetStereotaxic(state);
        foreach(TP_ProbeController pcontroller in allProbes)
            pcontroller.UpdateText();
    }

    public bool GetStereotaxic()
    {
        return localPrefs.GetStereotaxic();
    }
}
