using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TP_SliceRenderer : MonoBehaviour
{
    [SerializeField] private GameObject sagittalSliceGO;
    [SerializeField] private GameObject coronalSliceGO;
    [SerializeField] private TP_TrajectoryPlannerManager tpmanager;
    [SerializeField] private CCFModelControl modelControl;
    [SerializeField] private TP_PlayerPrefs localPrefs;
    [SerializeField] private TP_InPlaneSlice inPlaneSlice;
    [SerializeField] private Utils util;
    [SerializeField] private TMP_Dropdown dropdownMenu;
    [SerializeField] private AssetReference iblCoverageTextureAssetRef;

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
        Debug.Log("(SliceRenderer) Waiting for inplane slice to complete");
        await inPlaneSlice.GetGPUTextureTask();

        Debug.Log("(SliceRenderer) Loading 3D texture");
        ToggleSliceVisibility(localPrefs.GetSlice3D());

        if (dropdownMenu.value == 1)
            SetActiveTextureAnnotation();
        else if (dropdownMenu.value == 2)
            SetActiveTextureIBLCoverage();
        loaded = true;
    }


    private Texture3D iblCoverageTexture;
    private bool coverageLoaded;
    private bool coverageLoading;

    public async void LoadCoverageTexture()
    {
        coverageLoading = true;

        if (iblCoverageTextureAssetRef == null)
        {
            VolumetricDataset coverageDataset = tpmanager.GetIBLCoverageDataset();
            if (coverageDataset == null)
            {
                await tpmanager.LoadIBLCoverageDataset();
                coverageDataset = tpmanager.GetIBLCoverageDataset();
            }
            AnnotationDataset annotationDataset = tpmanager.GetAnnotationDataset();

            Color[] colors = new Color[] { Color.grey, Color.yellow, Color.green };

            iblCoverageTexture = new Texture3D(528, 320, 456, TextureFormat.RGB24, false);
            iblCoverageTexture.filterMode = FilterMode.Point;
            iblCoverageTexture.wrapMode = TextureWrapMode.Clamp;

            Debug.Log("Converting annotation dataset to texture format");
            for (int ap = 0; ap < 528; ap++)
            {
                for (int dv = 0; dv < 320; dv++)
                    for (int ml = 0; ml < 456; ml++)
                        if (annotationDataset.ValueAtIndex(ap, dv, ml) > 0)
                            iblCoverageTexture.SetPixel(ap, dv, ml, colors[coverageDataset.ValueAtIndex(ap, dv, ml)]);
                        else
                            iblCoverageTexture.SetPixel(ap, dv, ml, Color.black);
            }
            iblCoverageTexture.Apply();

            //if (Application.isEditor)
            //    AssetDatabase.CreateAsset(iblCoverageTexture, "Assets/AddressableAssets/Textures/IBLCoverageTexture3D.asset");
        }
        else
        {
            AsyncOperationHandle<Texture3D> dataLoader = iblCoverageTextureAssetRef.LoadAssetAsync<Texture3D>();
            await dataLoader.Task;
            iblCoverageTexture = dataLoader.Result;
        }

        coverageLoaded = true;

        SetActiveTextureIBLCoverage();
    }

    private void SetActiveTextureAnnotation()
    {
        saggitalSliceMaterial.SetTexture("_Volume", inPlaneSlice.GetAnnotationDatasetGPUTexture());
        coronalSliceMaterial.SetTexture("_Volume", inPlaneSlice.GetAnnotationDatasetGPUTexture());
    }

    private void SetActiveTextureIBLCoverage()
    {
        if (!coverageLoaded && !coverageLoading)
        {
            LoadCoverageTexture();
            return;
        }
        saggitalSliceMaterial.SetTexture("_Volume", iblCoverageTexture);
        coronalSliceMaterial.SetTexture("_Volume", iblCoverageTexture);
    }

    private void Update()
    {
        if (localPrefs.GetSlice3D()>0 && loaded)
        {
            // Check if the camera moved such that we have to flip the slice quads
            UpdateCameraPosition();

            if (tpmanager.MovedThisFrame())
            {
                UpdateSlicePosition();
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

        //Vector3 tipPositionAPDVLR = util.WorldSpace2apdvlr(tipPosition + tpmanager.GetCenterOffset());

        apWorldmm = tipPosition.z + 6.6f;
        coronalSliceGO.transform.position = new Vector3(0f, 0f, tipPosition.z);
        coronalSliceMaterial.SetFloat("_SlicePosition", apWorldmm / 13.2f);

        mlWorldmm = -(tipPosition.x - 5.7f);
        sagittalSliceGO.transform.position = new Vector3(tipPosition.x, 0f, 0f);
        saggitalSliceMaterial.SetFloat("_SlicePosition", mlWorldmm / 11.4f);

        UpdateNodeModelSlicing();
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

    public void ToggleSliceVisibility(int sliceType)
    {
        localPrefs.SetSlice3D(sliceType);

        if (sliceType==0)
        {
            // make slices invisible
            sagittalSliceGO.SetActive(false);
            coronalSliceGO.SetActive(false);
            ClearNodeModelSlicing();
        }
        else
        {
            // Standard sagittal/coronal slices
            localPrefs.SetSlice3D(sliceType);
            sagittalSliceGO.SetActive(true);
            coronalSliceGO.SetActive(true);

            if (sliceType == 1)
                SetActiveTextureAnnotation();
            else if (sliceType == 2)
                SetActiveTextureIBLCoverage();

            UpdateSlicePosition();
            UpdateCameraPosition();
        }
    }
}
