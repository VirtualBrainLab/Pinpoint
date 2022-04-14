using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_SliceRenderer : MonoBehaviour
{
    [SerializeField] private GameObject sagittalSliceGO;
    [SerializeField] private GameObject coronalSliceGO;
    [SerializeField] private TP_TrajectoryPlannerManager tpmanager;
    [SerializeField] private CCFModelControl modelControl;
    [SerializeField] private TP_PlayerPrefs localPrefs;
    [SerializeField] private TP_InPlaneSlice inPlaneSlice;
    [SerializeField] private Utils util;

    private int[] baseSize = { 528, 320, 456 };

    private bool loaded;

    private bool camXLeft;
    private bool camYBack;

    private Material saggitalSliceMaterial;
    private Material coronalSliceMaterial;

    private void Awake()
    {
        saggitalSliceMaterial = sagittalSliceGO.GetComponent<Renderer>().material;
        coronalSliceMaterial = coronalSliceGO.GetComponent<Renderer>().material;

        loaded = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        AsyncStart();
    }

    public async void AsyncStart()
    {
        Debug.Log("Waiting for inplane slice to complete");
        await inPlaneSlice.GetGPUTextureTask();

        ToggleSliceVisibility(localPrefs.GetSlice3D());

        saggitalSliceMaterial.SetTexture("_Volume", inPlaneSlice.GetAnnotationDatasetGPUTexture());
        coronalSliceMaterial.SetTexture("_Volume", inPlaneSlice.GetAnnotationDatasetGPUTexture());

        Debug.Log("Loading 3d texture");
        loaded = true;
    }

    private void Update()
    {
        if (localPrefs.GetSlice3D() && loaded)
        {
            // Check if the camera moved such that we have to flip the slice quads
            //UpdateCameraPosition();

            if (tpmanager.MovedThisFrame())
            {
                UpdateSlicePosition();
                UpdateCameraPosition();
            }
        }
    }

    private float apWorldmm;
    private float mlWorldmm;
    
    /// <summary>
    /// Shift the position of the sagittal and coronal slices to match the tip of the active probe
    /// </summary>
    private void UpdateSlicePosition()
    {
        TP_ProbeController activeProbeController = tpmanager.GetActiveProbeController();
        if (activeProbeController == null) return;
        Transform activeProbeTipT = activeProbeController.GetTipTransform();
        Vector3 tipPosition = activeProbeTipT.position + activeProbeTipT.up * 0.2f; // add 200 um to get to the start of the recording region

        Vector3 tipPositionAPDVLR = util.WorldSpace2apdvlr(tipPosition + tpmanager.GetCenterOffset());

        apWorldmm = tipPosition.z + 6.6f;
        coronalSliceGO.transform.position = new Vector3(0f, 0f, tipPosition.z);
        coronalSliceMaterial.SetFloat("_SlicePosition", apWorldmm / 13.2f);

        mlWorldmm = -(tipPosition.x - 5.7f);
        sagittalSliceGO.transform.position = new Vector3(tipPosition.x, 0f, 0f);
        saggitalSliceMaterial.SetFloat("_SlicePosition", mlWorldmm / 11.4f);
    }

    private void UpdateCameraPosition()
    {
        Vector3 camPosition = Camera.main.transform.position;
        bool changed = false;
        if (camXLeft && camPosition.x < 0)
        {
            camXLeft = false;
            changed = true;
        }
        else if (!camXLeft && camPosition.x > 0)
        {
            camXLeft = true;
            changed = true;
        }
        else if (camYBack && camPosition.z < 0)
        {
            camYBack = false;
            changed = true;
        }
        else if (!camYBack && camPosition.z > 0)
        {
            camYBack = true;
            changed = true;
        }
        if (changed)
            UpdateNodeModelSlicing();
    }

        private void UpdateNodeModelSlicing()
    {
        // Update the renderers on the node objects
        foreach (CCFTreeNode node in modelControl.GetDefaultLoadedNodes())
        {
            if (camYBack)
                // clip from apPosition forward
                node.SetShaderProperty("_APClip", new Vector2(0f, apWorldmm));
            else
                node.SetShaderProperty("_APClip", new Vector2(apWorldmm, 13.2f));

            if (camXLeft)
                // clip from mlPosition forward
                node.SetShaderProperty("_MLClip", new Vector2(mlWorldmm, 11.4f));
            else
                node.SetShaderProperty("_MLClip", new Vector2(0f, mlWorldmm));
        }
    }

    private void ClearNodeModelSlicing()
    {
        // Update the renderers on the node objects
        foreach (CCFTreeNode node in modelControl.GetDefaultLoadedNodes())
        {
            node.SetShaderProperty("_APClip", new Vector2(0f, 13.2f));
            node.SetShaderProperty("_MLClip", new Vector2(0f, 11.4f));
        }
    }

    public void ToggleSliceVisibility(bool visible)
    {
        localPrefs.SetSlice3D(visible);
        sagittalSliceGO.SetActive(visible);
        coronalSliceGO.SetActive(visible);
        if (visible)
        {
            // Force everything to render
            UpdateSlicePosition();
            UpdateCameraPosition();
        }
        else
        {
            ClearNodeModelSlicing();
        }
    }
}
