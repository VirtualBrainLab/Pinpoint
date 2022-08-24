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

    [SerializeField] private GameObject gpuInPlaneSliceGO;
    private Renderer gpuSliceRenderer;

    private AnnotationDataset annotationDataset;

    private float probeWidth = 70; // probes are 70um wide
    private float zoomFactor = 1f;

    private RectTransform rect;

    private Texture3D annotationDatasetGPUTexture;
    private TaskCompletionSource<bool> gpuTextureLoadedSource;
    private Task gpuTextureLoadedTask;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        gpuTextureLoadedSource = new TaskCompletionSource<bool>();
        gpuTextureLoadedTask = gpuTextureLoadedSource.Task;

        gpuSliceRenderer = gpuInPlaneSliceGO.GetComponent<Renderer>();

        gpuSliceRenderer.material.SetFloat("_FourShankProbe", 0f);
        gpuSliceRenderer.material.SetVector("_TipPosition", Vector4.zero);
        gpuSliceRenderer.material.SetVector("_ForwardDirection", Vector4.zero);
        gpuSliceRenderer.material.SetVector("_UpDirection", Vector4.zero);
        gpuSliceRenderer.material.SetFloat("_RecordingRegionSize", 0f);
        gpuSliceRenderer.material.SetFloat("_Scale", 1f);
        gpuSliceRenderer.material.SetFloat("_ShankWidth", probeWidth);
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

    public async Task<Texture3D> GetAnnotationDatasetGPUTexture()
    {
        await gpuTextureLoadedTask;

        return annotationDatasetGPUTexture;
    }

    public void StartAnnotationDataset()
    {
        annotationDataset = tpmanager.GetAnnotationDataset();

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

    private float inPlaneScale;
    private Vector3 centerOffset;
    private Vector3 recordingRegionCenterPosition;
    private Transform tipTransform;

    public void UpdateInPlaneSlice()
    {
        if (!localPrefs.GetInplane()) return;

        ProbeManager activeProbeController = tpmanager.GetActiveProbeController();

        // Calculate the size
        float[] heightPerc = activeProbeController.GetRecordingRegionHeight();
        float mmStartPos = heightPerc[0] * (10 - heightPerc[1]);
        float mmRecordingSize = heightPerc[1];

        // Take the active probe, find the position and rotation, and interpolate across the annotation dataset to render a 400x400 image of the brain at that slice
        tipTransform = activeProbeController.GetTipTransform();


        Vector3 tipPosition = tipTransform.position + tipTransform.up * (0.2f + mmStartPos);
        //tipPositionAPDVLR = Utils.WorldSpace2apdvlr(tipPosition + tpmanager.GetCenterOffset());
        bool fourShank = activeProbeController.GetProbeType() == 4;

        recordingRegionCenterPosition = fourShank ? 
            Utils.WorldSpace2apdvlr25(tipPosition + tipTransform.up * mmRecordingSize / 2 + tipTransform.forward * 0.375f) :
            Utils.WorldSpace2apdvlr25(tipPosition + tipTransform.up * mmRecordingSize / 2); ;

        gpuSliceRenderer.material.SetFloat("_FourShankProbe", fourShank ? 1f : 0f);

        inPlaneScale = mmRecordingSize * 1.5f * 1000f / 25f * zoomFactor;

        gpuSliceRenderer.material.SetVector("_RecordingRegionCenterPosition", recordingRegionCenterPosition);
        gpuSliceRenderer.material.SetVector("_ForwardDirection", tipTransform.forward);
        gpuSliceRenderer.material.SetVector("_UpDirection", tipTransform.up);
        gpuSliceRenderer.material.SetFloat("_RecordingRegionSize", mmRecordingSize * 1000f / 25f);
        gpuSliceRenderer.material.SetFloat("_Scale", inPlaneScale);
        GameObject.Find("SliceTextX").GetComponent<TextMeshProUGUI>().text = "<- " + mmRecordingSize * 1.5f + "mm ->";
        GameObject.Find("SliceTextY").GetComponent<TextMeshProUGUI>().text = "<- " + mmRecordingSize * 1.5f + "mm ->";
    }

    public void InPlaneSliceHover(Vector2 pointerData)
    {
        Vector3 inPlanePosition = CalculateInPlanePosition(pointerData);

        int annotation = annotationDataset.ValueAtIndex(Mathf.RoundToInt(inPlanePosition.x), Mathf.RoundToInt(inPlanePosition.y), Mathf.RoundToInt(inPlanePosition.z));
        annotation = modelControl.GetCurrentID(annotation);

        if (Input.GetMouseButtonDown(0))
        {
            if (annotation > 0)
                tpmanager.SelectBrainArea(annotation);
        }

        if (tpmanager.GetSetting_UseAcronyms())
            areaText.text = modelControl.ID2Acronym(annotation);
        else
            areaText.text = modelControl.GetCCFAreaName(annotation);
    }

    private Vector3 CalculateInPlanePosition(Vector2 pointerData)
    {
        Vector2 inPlanePosNorm = GetLocalRectPosNormalized(pointerData);

        // Take the tip transform and go out according to the in plane percentage 
        Vector3 inPlanePosition = recordingRegionCenterPosition + (RotateWorld2APDVLR(tipTransform.forward) * -inPlanePosNorm.x + RotateWorld2APDVLR(tipTransform.up) * inPlanePosNorm.y) * inPlaneScale;

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

    private Vector3 RotateWorld2APDVLR(Vector3 world)
    {
        return new Vector3(world.z, -world.y, -world.x);
    }

    public void ZoomIn()
    {
        zoomFactor = zoomFactor * 0.75f;
        UpdateInPlaneSlice();
    }

    public void ZoomOut()
    {
        zoomFactor = zoomFactor * 1.5f;
        UpdateInPlaneSlice();
    }

    public void ResetZoom()
    {
        zoomFactor = 1f;
        UpdateInPlaneSlice();
    }

    public void SetZoomFactor(float newZoomFactor)
    {
        zoomFactor = newZoomFactor;
        UpdateInPlaneSlice();
    }
}
