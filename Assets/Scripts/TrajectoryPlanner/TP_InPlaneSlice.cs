using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using TrajectoryPlanner;

public class TP_InPlaneSlice : MonoBehaviour
{
    // In plane slice handling
    [SerializeField] private TrajectoryPlannerManager _tpmanager;
    [SerializeField] private GameObject _inPlaneSliceUigo;
    [SerializeField] private CCFModelControl _modelControl;

    [SerializeField] private TextMeshProUGUI _areaText;
    [SerializeField] private TMP_Text _textX;
    [SerializeField] private TMP_Text _textY;

    [SerializeField] private Renderer _gpuSliceRenderer;

    private float probeWidth = 70; // probes are 70um wide
    private int zoomLevel = 0;
    private float zoomFactor = 1f;

    private RectTransform _rect;

    private Texture3D annotationDatasetGPUTexture;
    private TaskCompletionSource<bool> gpuTextureLoadedSource;
    private Task gpuTextureLoadedTask;

    private float inPlaneScale;
    private Vector3 recordingRegionCenterPosition;
    Vector3 upWorldU;
    Vector3 forwardWorldU;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        gpuTextureLoadedSource = new TaskCompletionSource<bool>();
        gpuTextureLoadedTask = gpuTextureLoadedSource.Task;

        ResetRendererParameters();
    }
    // Start is called before the first frame update
    private async void Start()
    {
        await VolumeDatasetManager.Texture3DLoaded();

        annotationDatasetGPUTexture = VolumeDatasetManager.AnnotationDatasetTexture3D;

        _gpuSliceRenderer.sharedMaterial.SetTexture("_Volume", annotationDatasetGPUTexture);
        _gpuSliceRenderer.sharedMaterial.SetVector("_VolumeSize", new Vector4(528, 320, 456, 0));
        gpuTextureLoadedSource.SetResult(true);
    }

    private void ResetRendererParameters()
    {
        _gpuSliceRenderer.sharedMaterial.SetFloat("_FourShankProbe", 0f);
        _gpuSliceRenderer.sharedMaterial.SetVector("_TipPosition", Vector4.zero);
        _gpuSliceRenderer.sharedMaterial.SetVector("_ForwardDirection", Vector4.zero);
        _gpuSliceRenderer.sharedMaterial.SetVector("_UpDirection", Vector4.zero);
        _gpuSliceRenderer.sharedMaterial.SetFloat("_RecordingRegionSize", 0f);
        _gpuSliceRenderer.sharedMaterial.SetFloat("_Scale", 1f);
        _gpuSliceRenderer.sharedMaterial.SetFloat("_ShankWidth", probeWidth);
    }

    public async Task<Texture3D> GetAnnotationDatasetGPUTexture()
    {
        await gpuTextureLoadedTask;

        return annotationDatasetGPUTexture;
    }

    public Task GetGPUTextureTask()
    {
        return gpuTextureLoadedTask;
    }

    // *** INPLANE SLICE CODE *** //
    public void UpdateInPlaneVisibility()
    {
        _inPlaneSliceUigo.SetActive(Settings.ShowInPlaneSlice);
    }

    public void UpdateInPlaneSlice()
    {
        if (!Settings.ShowInPlaneSlice) return;

        if (ProbeManager.ActiveProbeManager == null)
        {
            ResetRendererParameters();
            return;
        }

        ProbeInsertion insertion = ProbeManager.ActiveProbeManager.ProbeController.Insertion;

        // Get the start/end coordinates of the probe recording region and convert them into *un-transformed* coordinates
        (Vector3 startCoordWorldU, Vector3 endCoordWorldU) = ProbeManager.ActiveProbeManager.RecRegionCoordWorldU;

        Vector3 startApdvlr25 = VolumeDatasetManager.AnnotationDataset.CoordinateSpace.World2Space(startCoordWorldU);
        Vector3 endApdvlr25 = VolumeDatasetManager.AnnotationDataset.CoordinateSpace.World2Space(endCoordWorldU);
        //(Vector3 startCoordWorld, Vector3 endCoordWorld) = ProbeManager.ActiveProbeManager.ProbeController.GetRecordingRegionWorld();
        (_, upWorldU, forwardWorldU) = ProbeManager.ActiveProbeManager.ProbeController.GetTipWorldU();

#if UNITY_EDITOR
        // debug statements
        Debug.DrawRay(startCoordWorldU, upWorldU, Color.green);
        Debug.DrawRay(endCoordWorldU, forwardWorldU, Color.red);
#endif

        // Calculate the size

        float recordingSizemmU = Vector3.Distance(startCoordWorldU, endCoordWorldU);

        // This could be improved by moving this check into a property attached to the probe type in some way
        bool fourShank = false;
        bool twoShank = false;

        // This needs to be improved by making it possible to attach shanks to the shader in some way, instead of relying on this per-shank check to render them properly
        float shankSpacing = 0f;
        float centerOffset = 0f;

        switch (ProbeManager.ActiveProbeManager.ProbeType)
        {
            case ProbeProperties.ProbeType.Neuropixels24:
                shankSpacing = 0.25f;
                centerOffset = 1.5f;
                fourShank = true;
                break;

            case ProbeProperties.ProbeType.Neuropixels24x2:
                shankSpacing = 0.25f;
                centerOffset = 1.5f;
                fourShank = true;
                break;

            case ProbeProperties.ProbeType.UCLA128K:
                shankSpacing = -0.2f;
                centerOffset = 1.5f;
                fourShank = true;
                break;

            case ProbeProperties.ProbeType.UCLA256F:
                shankSpacing = -0.5f;
                centerOffset = 0.5f;
                twoShank = true;
                break;
        }
        _gpuSliceRenderer.sharedMaterial.SetFloat("_ShankSpacing", shankSpacing);

        Debug.Log((startApdvlr25, endApdvlr25));

        recordingRegionCenterPosition = VolumeDatasetManager.AnnotationDataset.CoordinateSpace.World2Space(startCoordWorldU + 
            upWorldU * recordingSizemmU / 2 + 
            forwardWorldU * shankSpacing * centerOffset);

        _gpuSliceRenderer.sharedMaterial.SetFloat("_FourShankProbe", fourShank ? 1f : 0f);
        _gpuSliceRenderer.sharedMaterial.SetFloat("_TwoShankProbe", twoShank ? 1f : 0f);

        inPlaneScale = recordingSizemmU * 1.5f * 1000f / 25f * zoomFactor;


        _gpuSliceRenderer.sharedMaterial.SetVector("_RecordingRegionCenterPosition", recordingRegionCenterPosition);
        _gpuSliceRenderer.sharedMaterial.SetVector("_ForwardDirection", forwardWorldU);
        _gpuSliceRenderer.sharedMaterial.SetVector("_UpDirection", upWorldU);
        _gpuSliceRenderer.sharedMaterial.SetFloat("_RecordingRegionSize", recordingSizemmU * 1000f / 25f);
        _gpuSliceRenderer.sharedMaterial.SetFloat("_Scale", inPlaneScale);
        float roundedMmRecSize = Mathf.Round(recordingSizemmU * 1.5f * zoomFactor * 100) / 100;

        string formatted = $"< {roundedMmRecSize} mm >";
        _textX.text = formatted;
        _textY.text = formatted;
    }

    public void InPlaneSliceHover(Vector2 pointerData)
    {
        if (ProbeManager.ActiveProbeManager == null)
            return;

        Vector3 inPlanePosition = CalculateInPlanePosition(pointerData);

        int annotation = VolumeDatasetManager.AnnotationDataset.ValueAtIndex(Mathf.RoundToInt(inPlanePosition.x), Mathf.RoundToInt(inPlanePosition.y), Mathf.RoundToInt(inPlanePosition.z));
        annotation = _modelControl.RemapID(annotation);

        if (Input.GetMouseButtonDown(0))
        {
            if (annotation > 0)
                _tpmanager.TargetSearchArea(annotation);
        }

        if (Settings.UseAcronyms)
            _areaText.text = _modelControl.ID2Acronym(annotation);
        else
            _areaText.text = _modelControl.ID2AreaName(annotation);
    }

    private Vector3 CalculateInPlanePosition(Vector2 pointerData)
    {
        Vector2 inPlanePosNorm = GetLocalRectPosNormalized(pointerData) * inPlaneScale / 2;
        // Take the tip transform and go out according to the in plane percentage 
        Vector3 inPlanePosition = recordingRegionCenterPosition + (VolumeDatasetManager.AnnotationDataset.CoordinateSpace.World2SpaceAxisChange(forwardWorldU) * -inPlanePosNorm.x + VolumeDatasetManager.AnnotationDataset.CoordinateSpace.World2SpaceAxisChange(upWorldU) * inPlanePosNorm.y);
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
