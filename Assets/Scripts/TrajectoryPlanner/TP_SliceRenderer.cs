using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TrajectoryPlanner;

public class TP_SliceRenderer : MonoBehaviour
{
    [SerializeField] private GameObject _sagittalSliceGO;
    [SerializeField] private GameObject _coronalSliceGO;
    [SerializeField] private TrajectoryPlannerManager _tpmanager;
    [SerializeField] private CCFModelControl _modelControl;
    [SerializeField] private PlayerPrefs _localPrefs;
    [SerializeField] private TP_InPlaneSlice _inPlaneSlice;
    [SerializeField] private Utils _util;
    [SerializeField] private TMP_Dropdown _dropdownMenu;
    [SerializeField] private AssetReference _iblCoverageTextureAssetRef;

    private bool _loaded;

    private bool _camXLeft;
    private bool _camYBack;

    private Material _saggitalSliceMaterial;
    private Material _coronalSliceMaterial;

    private void Awake()
    {
        _saggitalSliceMaterial = _sagittalSliceGO.GetComponent<Renderer>().material;
        _coronalSliceMaterial = _coronalSliceGO.GetComponent<Renderer>().material;

        _loaded = false;
    }

    // Start is called before the first frame update
    private async void Start()
    {
        Debug.Log("(SliceRenderer) Waiting for inplane slice to complete");
        await _inPlaneSlice.GetGPUTextureTask();
        Debug.Log("(SliceRenderer) Waiting for node models to load");
        await _modelControl.GetDefaultLoadedTask();

        Debug.Log("(SliceRenderer) Loading 3D texture");
        ToggleSliceVisibility(_localPrefs.GetSlice3D());

        if (_dropdownMenu.value == 1)
            SetActiveTextureAnnotation();
        //else if (dropdownMenu.value == 2)
        //    SetActiveTextureIBLCoverage();
        _loaded = true;
    }


    //private Texture3D iblCoverageTexture;
    //private bool coverageLoaded;
    //private bool coverageLoading;

    //public async void LoadCoverageTexture()
    //{
    //    coverageLoading = true;

    //    if (iblCoverageTextureAssetRef == null)
    //    {
    //        VolumetricDataset coverageDataset = tpmanager.GetIBLCoverageDataset();
    //        if (coverageDataset == null)
    //        {
    //            await tpmanager.LoadIBLCoverageDataset();
    //            coverageDataset = tpmanager.GetIBLCoverageDataset();
    //        }
    //        AnnotationDataset annotationDataset = tpmanager.GetAnnotationDataset();

    //        Color[] colors = new Color[] { Color.grey, Color.yellow, Color.green };

    //        iblCoverageTexture = new Texture3D(528, 320, 456, TextureFormat.RGB24, false);
    //        iblCoverageTexture.filterMode = FilterMode.Point;
    //        iblCoverageTexture.wrapMode = TextureWrapMode.Clamp;

    //        Debug.Log("Converting annotation dataset to texture format");
    //        for (int ap = 0; ap < 528; ap++)
    //        {
    //            for (int dv = 0; dv < 320; dv++)
    //                for (int ml = 0; ml < 456; ml++)
    //                    if (annotationDataset.ValueAtIndex(ap, dv, ml) > 0)
    //                        iblCoverageTexture.SetPixel(ap, dv, ml, colors[coverageDataset.ValueAtIndex(ap, dv, ml)]);
    //                    else
    //                        iblCoverageTexture.SetPixel(ap, dv, ml, Color.black);
    //        }
    //        iblCoverageTexture.Apply();

    //        //if (Application.isEditor)
    //        //    AssetDatabase.CreateAsset(iblCoverageTexture, "Assets/AddressableAssets/Textures/IBLCoverageTexture3D.asset");
    //    }
    //    else
    //    {
    //        AsyncOperationHandle<Texture3D> dataLoader = iblCoverageTextureAssetRef.LoadAssetAsync<Texture3D>();
    //        await dataLoader.Task;
    //        iblCoverageTexture = dataLoader.Result;
    //    }
    //    coverageLoaded = true;

    //    SetActiveTextureIBLCoverage();
    //}

    private async void SetActiveTextureAnnotation()
    {
        Task<Texture3D> textureTask = _inPlaneSlice.GetAnnotationDatasetGPUTexture();
        await textureTask;

        _saggitalSliceMaterial.SetTexture("_Volume", textureTask.Result);
        _coronalSliceMaterial.SetTexture("_Volume", textureTask.Result);
    }

    //private void SetActiveTextureIBLCoverage()
    //{
    //    if (!coverageLoaded && !coverageLoading)
    //    {
    //        LoadCoverageTexture();
    //        return;
    //    }
    //    saggitalSliceMaterial.SetTexture("_Volume", iblCoverageTexture);
    //    coronalSliceMaterial.SetTexture("_Volume", iblCoverageTexture);
    //}

    private void Update()
    {
        if (_localPrefs.GetSlice3D()>0 && _loaded)
        {
            // Check if the camera moved such that we have to flip the slice quads
            UpdateCameraPosition();
        }
    }

    private float _apWorldmm;
    private float _mlWorldmm;
    
    /// <summary>
    /// Shift the position of the sagittal and coronal slices to match the tip of the active probe
    /// </summary>
    public void UpdateSlicePosition()
    {
        if (_localPrefs.GetSlice3D() > 0)
        {
            ProbeManager activeProbeManager = _tpmanager.GetActiveProbeManager();
            if (activeProbeManager == null) return;

            // the actual tip
            Vector3 probeTipWorld = activeProbeManager.GetProbeController().ProbeTipT.position;
            // position the slices along the real tip in world space
            _coronalSliceGO.transform.position = new Vector3(0f, 0f, probeTipWorld.z);
            _sagittalSliceGO.transform.position = new Vector3(probeTipWorld.x, 0f, 0f);

            // for CCF coordinates
            (Vector3 tipCoordWorld, _, _) = activeProbeManager.GetProbeController().GetTipWorldU();

            _apWorldmm = tipCoordWorld.z + 6.6f;
            _coronalSliceMaterial.SetFloat("_SlicePosition", _apWorldmm / 13.2f);

            _mlWorldmm = -(tipCoordWorld.x - 5.7f);
            _saggitalSliceMaterial.SetFloat("_SlicePosition", _mlWorldmm / 11.4f);

            UpdateNodeModelSlicing();
        }
    }

    private void UpdateCameraPosition()
    {
        Vector3 camPosition = Camera.main.transform.position;
        bool changed = false;
        if (_camXLeft && camPosition.x < 0)
        {
            _camXLeft = false;
            changed = true;
        }
        else if (!_camXLeft && camPosition.x > 0)
        {
            _camXLeft = true;
            changed = true;
        }
        else if (_camYBack && camPosition.z < 0)
        {
            _camYBack = false;
            changed = true;
        }
        else if (!_camYBack && camPosition.z > 0)
        {
            _camYBack = true;
            changed = true;
        }
        if (changed)
            UpdateNodeModelSlicing();
    }

    private void UpdateNodeModelSlicing()
    {
        // Update the renderers on the node objects
        foreach (CCFTreeNode node in _modelControl.GetDefaultLoadedNodes())
        {
            if (_camYBack)
                // clip from apPosition forward
                node.SetShaderProperty("_APClip", new Vector2(0f, _apWorldmm));
            else
                node.SetShaderProperty("_APClip", new Vector2(_apWorldmm, 13.2f));

            if (_camXLeft)
                // clip from mlPosition forward
                node.SetShaderProperty("_MLClip", new Vector2(_mlWorldmm, 11.4f));
            else
                node.SetShaderProperty("_MLClip", new Vector2(0f, _mlWorldmm));
        }
    }

    private void ClearNodeModelSlicing()
    {
        // Update the renderers on the node objects
        foreach (CCFTreeNode node in _modelControl.GetDefaultLoadedNodes())
        {
            node.SetShaderProperty("_APClip", new Vector2(0f, 13.2f));
            node.SetShaderProperty("_MLClip", new Vector2(0f, 11.4f));
        }
    }

    public void ToggleSliceVisibility(int sliceType)
    {
        Debug.Log(sliceType);
        _localPrefs.SetSlice3D(sliceType);

        if (sliceType==0)
        {
            // make slices invisible
            _sagittalSliceGO.SetActive(false);
            _coronalSliceGO.SetActive(false);
            ClearNodeModelSlicing();
        }
        else
        {
            // Standard sagittal/coronal slices
            _sagittalSliceGO.SetActive(true);
            _coronalSliceGO.SetActive(true);

            if (sliceType == 1)
                SetActiveTextureAnnotation();
            //else if (sliceType == 2)
            //    SetActiveTextureIBLCoverage();

            UpdateSlicePosition();
            UpdateCameraPosition();
        }
    }
}
