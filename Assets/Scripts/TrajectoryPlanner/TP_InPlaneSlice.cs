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

public class TP_InPlaneSlice : MonoBehaviour
{
    // In plane slice handling
    [SerializeField] private TrajectoryPlannerManager _tpmanager;
    [SerializeField] private GameObject _inPlaneSliceUIGO;
    [SerializeField] private CCFModelControl _modelControl;
    [SerializeField] private PlayerPrefs _localPrefs;

    [SerializeField] private TextMeshProUGUI _areaText;
    [SerializeField] private TMP_Text _textX;
    [SerializeField] private TMP_Text _textY;

    [SerializeField] private GameObject _gpuInPlaneSliceGO;
    private Renderer _gpuSliceRenderer;

    private CCFAnnotationDataset _annotationDataset;

    private float _probeWidth = 70; // probes are 70um wide
    private int _zoomLevel = 0;
    private float _zoomFactor = 1f;

    private RectTransform _rect;

    private Texture3D _annotationDatasetGPUTexture;
    private TaskCompletionSource<bool> _gpuTextureLoadedSource;
    private Task _gpuTextureLoadedTask;

    private float _inPlaneScale;
    private Vector3 _recordingRegionCenterPosition;
    private Vector3 _upWorld;
    private Vector3 _forwardWorld;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        _gpuTextureLoadedSource = new TaskCompletionSource<bool>();
        _gpuTextureLoadedTask = _gpuTextureLoadedSource.Task;

        _gpuSliceRenderer = _gpuInPlaneSliceGO.GetComponent<Renderer>();

        ResetRendererParameters();
    }
    // Start is called before the first frame update
    private async void Start()
    {

        Task<Texture3D> textureTask = AddressablesRemoteLoader.LoadAnnotationTexture();
        await textureTask;

        _annotationDatasetGPUTexture = textureTask.Result;
        _gpuSliceRenderer.material.SetTexture("_Volume", _annotationDatasetGPUTexture);
        _gpuSliceRenderer.material.SetVector("_VolumeSize", new Vector4(528, 320, 456, 0));
        _gpuTextureLoadedSource.SetResult(true);

        Debug.Log("(InPlaneSlice) Annotation dataset texture loaded");
    }

    private void ResetRendererParameters()
    {
        _gpuSliceRenderer.material.SetFloat("_FourShankProbe", 0f);
        _gpuSliceRenderer.material.SetVector("_TipPosition", Vector4.zero);
        _gpuSliceRenderer.material.SetVector("_ForwardDirection", Vector4.zero);
        _gpuSliceRenderer.material.SetVector("_UpDirection", Vector4.zero);
        _gpuSliceRenderer.material.SetFloat("_RecordingRegionSize", 0f);
        _gpuSliceRenderer.material.SetFloat("_Scale", 1f);
        _gpuSliceRenderer.material.SetFloat("_ShankWidth", _probeWidth);
    }

    public async Task<Texture3D> GetAnnotationDatasetGPUTexture()
    {
        await _gpuTextureLoadedTask;

        return _annotationDatasetGPUTexture;
    }

    public void StartAnnotationDataset()
    {
        _annotationDataset = _tpmanager.GetAnnotationDataset();
        Debug.Log("(in-plane slice) Annotation data set");
    }

    public Task GetGPUTextureTask()
    {
        return _gpuTextureLoadedTask;
    }

    // *** INPLANE SLICE CODE *** //
    public void UpdateInPlaneVisibility()
    {
        _inPlaneSliceUIGO.SetActive(_localPrefs.GetInplane());
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
        (_, _upWorld, _forwardWorld) = activeProbeManager.GetProbeController().GetTipWorldU();

#if UNITY_EDITOR
        // debug statements
        Debug.DrawRay(startCoordWorld, _upWorld, Color.green);
        Debug.DrawRay(startCoordWorld, _forwardWorld, Color.red);
#endif

        // Calculate the size
        float mmRecordingSize = Vector3.Distance(startCoordWorld, endCoordWorld);

        int type = activeProbeManager.ProbeType;
        bool fourShank = type == 4 || type == 8;

        _recordingRegionCenterPosition = fourShank ? 
            _annotationDataset.CoordinateSpace.World2Space(startCoordWorld + _upWorld * mmRecordingSize / 2 + _forwardWorld * 0.375f) :
            _annotationDataset.CoordinateSpace.World2Space(startCoordWorld + _upWorld * mmRecordingSize / 2);

        _gpuSliceRenderer.material.SetFloat("_FourShankProbe", fourShank ? 1f : 0f);

        _inPlaneScale = mmRecordingSize * 1.5f * 1000f / 25f * _zoomFactor;

        _gpuSliceRenderer.material.SetVector("_RecordingRegionCenterPosition", _recordingRegionCenterPosition);
        _gpuSliceRenderer.material.SetVector("_ForwardDirection", _forwardWorld);
        _gpuSliceRenderer.material.SetVector("_UpDirection", _upWorld);
        _gpuSliceRenderer.material.SetFloat("_RecordingRegionSize", mmRecordingSize * 1000f / 25f);
        _gpuSliceRenderer.material.SetFloat("_Scale", _inPlaneScale);
        float roundedMmRecSize = Mathf.Round(mmRecordingSize * 1.5f * 100) / 100;
        string formatted = string.Format("<- {0} mm ->", roundedMmRecSize);
        _textX.text = formatted;
        _textY.text = formatted;
    }

    public void InPlaneSliceHover(Vector2 pointerData)
    {
        Vector3 inPlanePosition = CalculateInPlanePosition(pointerData);

        int annotation = _annotationDataset.ValueAtIndex(Mathf.RoundToInt(inPlanePosition.x), Mathf.RoundToInt(inPlanePosition.y), Mathf.RoundToInt(inPlanePosition.z));
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
        Vector2 inPlanePosNorm = GetLocalRectPosNormalized(pointerData) * _inPlaneScale / 2;
        // Take the tip transform and go out according to the in plane percentage 
        Vector3 inPlanePosition = _recordingRegionCenterPosition + (_annotationDataset.CoordinateSpace.World2SpaceAxisChange(_forwardWorld) * -inPlanePosNorm.x + _annotationDataset.CoordinateSpace.World2SpaceAxisChange(_upWorld) * inPlanePosNorm.y);
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
        _zoomLevel += 1;
        _zoomFactor = Mathf.Pow(0.75f, _zoomLevel);
        UpdateInPlaneSlice();
    }

    public void ZoomOut()
    {
        _zoomLevel -= 1;
        _zoomFactor = Mathf.Pow(0.75f, _zoomLevel);
        UpdateInPlaneSlice();
    }

    public void ResetZoom()
    {
        _zoomLevel = 0;
        _zoomFactor = 1f;
        UpdateInPlaneSlice();
    }

    public void SetZoomFactor(float newZoomFactor)
    {
        _zoomFactor = newZoomFactor;
        UpdateInPlaneSlice();
    }
}
