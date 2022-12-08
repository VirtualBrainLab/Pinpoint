using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using TrajectoryPlanner;
using UnityEngine.Serialization;

public class TP_InPlaneSlice : MonoBehaviour
{
    // In plane slice handling
    [FormerlySerializedAs("tpmanager")] [SerializeField] private TrajectoryPlannerManager _tpmanager;
    [FormerlySerializedAs("inPlaneSliceUIGO")] [SerializeField] private GameObject _inPlaneSliceUigo;
    [FormerlySerializedAs("modelControl")] [SerializeField] private CCFModelControl _modelControl;
    [FormerlySerializedAs("localPrefs")] [SerializeField] private PlayerPrefs _localPrefs;

    [FormerlySerializedAs("areaText")] [SerializeField] private TextMeshProUGUI _areaText;
    [FormerlySerializedAs("textX")] [SerializeField] private TMP_Text _textX;
    [FormerlySerializedAs("textY")] [SerializeField] private TMP_Text _textY;

    [FormerlySerializedAs("gpuInPlaneSliceGO")] [SerializeField] private GameObject _gpuInPlaneSliceGo;
    private Renderer gpuSliceRenderer;

    private CCFAnnotationDataset annotationDataset;

    private float probeWidth = 70; // probes are 70um wide
    private int zoomLevel = 0;
    private float zoomFactor = 1f;

    private RectTransform _rect;

    private Texture3D annotationDatasetGPUTexture;
    private TaskCompletionSource<bool> gpuTextureLoadedSource;
    private Task gpuTextureLoadedTask;

    private float inPlaneScale;
    private Vector3 recordingRegionCenterPosition;
    Vector3 upWorld;
    Vector3 forwardWorld;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        gpuTextureLoadedSource = new TaskCompletionSource<bool>();
        gpuTextureLoadedTask = gpuTextureLoadedSource.Task;

        gpuSliceRenderer = _gpuInPlaneSliceGo.GetComponent<Renderer>();

        ResetRendererParameters();
    }
    // Start is called before the first frame update
    private async void Start()
    {

        Task<Texture3D> textureTask = AddressablesRemoteLoader.LoadAnnotationTexture();
        await textureTask;

        annotationDatasetGPUTexture = textureTask.Result;
        gpuSliceRenderer.material.SetTexture("_Volume", annotationDatasetGPUTexture);
        gpuSliceRenderer.material.SetVector("_VolumeSize", new Vector4(528, 320, 456, 0));
        gpuTextureLoadedSource.SetResult(true);

        Debug.Log("(InPlaneSlice) Annotation dataset texture loaded");
    }

    private void ResetRendererParameters()
    {
        gpuSliceRenderer.material.SetFloat("_FourShankProbe", 0f);
        gpuSliceRenderer.material.SetVector("_TipPosition", Vector4.zero);
        gpuSliceRenderer.material.SetVector("_ForwardDirection", Vector4.zero);
        gpuSliceRenderer.material.SetVector("_UpDirection", Vector4.zero);
        gpuSliceRenderer.material.SetFloat("_RecordingRegionSize", 0f);
        gpuSliceRenderer.material.SetFloat("_Scale", 1f);
        gpuSliceRenderer.material.SetFloat("_ShankWidth", probeWidth);
    }

    public async Task<Texture3D> GetAnnotationDatasetGPUTexture()
    {
        await gpuTextureLoadedTask;

        return annotationDatasetGPUTexture;
    }

    public void StartAnnotationDataset()
    {
        annotationDataset = _tpmanager.GetAnnotationDataset();
        Debug.Log("(in-plane slice) Annotation data set");
    }

    public Task GetGPUTextureTask()
    {
        return gpuTextureLoadedTask;
    }

    // *** INPLANE SLICE CODE *** //
    public void UpdateInPlaneVisibility()
    {
        _inPlaneSliceUigo.SetActive(_localPrefs.GetInplane());
    }

    public void UpdateInPlaneSlice()
    {
        if (!_localPrefs.GetInplane()) return;

        ProbeManager activeProbeManager = _tpmanager.GetActiveProbeManager();

        if (activeProbeManager == null)
        {
            ResetRendererParameters();
            return;
        }

        (Vector3 startCoordWorld, Vector3 endCoordWorld) = activeProbeManager.GetProbeController().GetRecordingRegionWorld();
        (_, upWorld, forwardWorld) = activeProbeManager.GetProbeController().GetTipWorldU();

#if UNITY_EDITOR
        // debug statements
        Debug.DrawRay(startCoordWorld, upWorld, Color.green);
        Debug.DrawRay(startCoordWorld, forwardWorld, Color.red);
#endif

        // Calculate the size
        float mmRecordingSize = Vector3.Distance(startCoordWorld, endCoordWorld);

        int type = activeProbeManager.ProbeType;
        bool fourShank = type == 4 || type == 8;

        recordingRegionCenterPosition = fourShank ? 
            annotationDataset.CoordinateSpace.World2Space(startCoordWorld + upWorld * mmRecordingSize / 2 + forwardWorld * 0.375f) :
            annotationDataset.CoordinateSpace.World2Space(startCoordWorld + upWorld * mmRecordingSize / 2);

        gpuSliceRenderer.material.SetFloat("_FourShankProbe", fourShank ? 1f : 0f);

        inPlaneScale = mmRecordingSize * 1.5f * 1000f / 25f * zoomFactor;

        gpuSliceRenderer.material.SetVector("_RecordingRegionCenterPosition", recordingRegionCenterPosition);
        gpuSliceRenderer.material.SetVector("_ForwardDirection", forwardWorld);
        gpuSliceRenderer.material.SetVector("_UpDirection", upWorld);
        gpuSliceRenderer.material.SetFloat("_RecordingRegionSize", mmRecordingSize * 1000f / 25f);
        gpuSliceRenderer.material.SetFloat("_Scale", inPlaneScale);
        float roundedMmRecSize = Mathf.Round(mmRecordingSize * 1.5f * zoomFactor * 100) / 100;
        string formatted = string.Format("<- {0} mm ->", roundedMmRecSize);
        _textX.text = formatted;
        _textY.text = formatted;
    }

    public void InPlaneSliceHover(Vector2 pointerData)
    {
        Vector3 inPlanePosition = CalculateInPlanePosition(pointerData);

        int annotation = annotationDataset.ValueAtIndex(Mathf.RoundToInt(inPlanePosition.x), Mathf.RoundToInt(inPlanePosition.y), Mathf.RoundToInt(inPlanePosition.z));
        annotation = _modelControl.RemapID(annotation);

        if (Input.GetMouseButtonDown(0))
        {
            if (annotation > 0)
                _tpmanager.TargetSearchArea(annotation);
        }

        if (_tpmanager.GetSetting_UseAcronyms())
            _areaText.text = _modelControl.ID2Acronym(annotation);
        else
            _areaText.text = _modelControl.ID2AreaName(annotation);
    }

    private Vector3 CalculateInPlanePosition(Vector2 pointerData)
    {
        Vector2 inPlanePosNorm = GetLocalRectPosNormalized(pointerData) * inPlaneScale / 2;
        // Take the tip transform and go out according to the in plane percentage 
        Vector3 inPlanePosition = recordingRegionCenterPosition + (annotationDataset.CoordinateSpace.World2SpaceAxisChange(forwardWorld) * -inPlanePosNorm.x + annotationDataset.CoordinateSpace.World2SpaceAxisChange(upWorld) * inPlanePosNorm.y);
        return inPlanePosition;
    }

    // Return the position within the local UI rectangle scaled to [-1, 1] on each axis
    private Vector2 GetLocalRectPosNormalized(Vector2 pointerData)
    {
        Vector2 inPlanePosNorm;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, pointerData, Camera.main, out inPlanePosNorm);

        inPlanePosNorm += new Vector2(_rect.rect.width, _rect.rect.height / 2);
        inPlanePosNorm.x = inPlanePosNorm.x / _rect.rect.width * 2 - 1;
        inPlanePosNorm.y = inPlanePosNorm.y / _rect.rect.height * 2 - 1;
        return inPlanePosNorm;
    }

    public void ZoomIn()
    {
        zoomLevel += 1;
        zoomFactor = Mathf.Pow(0.75f, zoomLevel);
        UpdateInPlaneSlice();
    }

    public void ZoomOut()
    {
        zoomLevel -= 1;
        zoomFactor = Mathf.Pow(0.75f, zoomLevel);
        UpdateInPlaneSlice();
    }

    public void ResetZoom()
    {
        zoomLevel = 0;
        zoomFactor = 1f;
        UpdateInPlaneSlice();
    }

    public void SetZoomFactor(float newZoomFactor)
    {
        zoomFactor = newZoomFactor;
        UpdateInPlaneSlice();
    }
}
