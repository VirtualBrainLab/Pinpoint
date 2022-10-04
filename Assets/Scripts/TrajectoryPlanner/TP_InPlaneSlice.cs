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
    [SerializeField] private TrajectoryPlannerManager tpmanager;
    [SerializeField] private GameObject inPlaneSliceUIGO;
    [SerializeField] private CCFModelControl modelControl;
    [SerializeField] private PlayerPrefs localPrefs;

    [SerializeField] private TextMeshProUGUI areaText;
    [SerializeField] private TMP_Text textX;
    [SerializeField] private TMP_Text textY;

    [SerializeField] private GameObject gpuInPlaneSliceGO;
    private Renderer gpuSliceRenderer;

    private CCFAnnotationDataset annotationDataset;

    private float probeWidth = 70; // probes are 70um wide
    private int zoomLevel = 0;
    private float zoomFactor = 1f;

    private RectTransform rect;

    private Texture3D annotationDatasetGPUTexture;
    private TaskCompletionSource<bool> gpuTextureLoadedSource;
    private Task gpuTextureLoadedTask;

    private float inPlaneScale;
    private Vector3 recordingRegionCenterPosition;
    Vector3 upWorld;
    Vector3 forwardWorld;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        gpuTextureLoadedSource = new TaskCompletionSource<bool>();
        gpuTextureLoadedTask = gpuTextureLoadedSource.Task;

        gpuSliceRenderer = gpuInPlaneSliceGO.GetComponent<Renderer>();

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
        annotationDataset = tpmanager.GetAnnotationDataset();
        Debug.Log("(in-plane slice) Annotation data set");
    }

    public Task GetGPUTextureTask()
    {
        return gpuTextureLoadedTask;
    }

    // *** INPLANE SLICE CODE *** //
    public void UpdateInPlaneVisibility()
    {
        inPlaneSliceUIGO.SetActive(localPrefs.GetInplane());
    }

    public void UpdateInPlaneSlice()
    {
        if (!localPrefs.GetInplane()) return;

        ProbeManager activeProbeController = tpmanager.GetActiveProbeController();

        if (activeProbeController == null)
        {
            ResetRendererParameters();
            return;
        }

        (Vector3 startCoordWorld, Vector3 endCoordWorld) = activeProbeController.GetProbeController().GetRecordingRegionWorld();
        upWorld = (endCoordWorld - startCoordWorld).normalized;
        forwardWorld = Quaternion.Euler(-90f, 0f, 0f) * upWorld;

        // Calculate the size
        float mmRecordingSize = Vector3.Distance(startCoordWorld, endCoordWorld);

        int type = activeProbeController.ProbeType;
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
        textX.text = "<- " + mmRecordingSize * 1.5f + "mm ->";
        textY.text = "<- " + mmRecordingSize * 1.5f + "mm ->";
    }

    public void InPlaneSliceHover(Vector2 pointerData)
    {
        Vector3 inPlanePosition = CalculateInPlanePosition(pointerData);

        int annotation = annotationDataset.ValueAtIndex(Mathf.RoundToInt(inPlanePosition.x), Mathf.RoundToInt(inPlanePosition.y), Mathf.RoundToInt(inPlanePosition.z));
        annotation = modelControl.RemapID(annotation);

        if (Input.GetMouseButtonDown(0))
        {
            if (annotation > 0)
                tpmanager.TargetSearchArea(annotation);
        }

        if (tpmanager.GetSetting_UseAcronyms())
            areaText.text = modelControl.ID2Acronym(annotation);
        else
            areaText.text = modelControl.ID2AreaName(annotation);
    }

    private Vector3 CalculateInPlanePosition(Vector2 pointerData)
    {
        Vector2 inPlanePosNorm = GetLocalRectPosNormalized(pointerData) * inPlaneScale / 2;

        // Take the tip transform and go out according to the in plane percentage 
        Vector3 inPlanePosition = recordingRegionCenterPosition + (annotationDataset.CoordinateSpace.World2SpaceRot(forwardWorld) * -inPlanePosNorm.x + annotationDataset.CoordinateSpace.World2SpaceRot(upWorld) * inPlanePosNorm.y);

        return inPlanePosition;
    }

    // Return the position within the local UI rectangle scaled to [-1, 1] on each axis
    private Vector2 GetLocalRectPosNormalized(Vector2 pointerData)
    {
        Vector2 inPlanePosNorm;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pointerData, Camera.main, out inPlanePosNorm);

        inPlanePosNorm += new Vector2(rect.rect.width, rect.rect.height / 2);
        inPlanePosNorm.x = inPlanePosNorm.x / rect.rect.width * 2 - 1;
        inPlanePosNorm.y = inPlanePosNorm.y / rect.rect.height * 2 - 1;
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
