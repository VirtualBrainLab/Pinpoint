using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using TrajectoryPlanner;
using BrainAtlas;

public class TP_InPlaneSlice : MonoBehaviour
{
    // In plane slice handling
    [SerializeField] private TrajectoryPlannerManager _tpmanager;
    [SerializeField] private GameObject _inPlaneSliceUigo;

    [SerializeField] private TextMeshProUGUI _areaText;
    [SerializeField] private TMP_Text _textX;
    [SerializeField] private TMP_Text _textY;

    [SerializeField] private Renderer _gpuSliceRenderer;

    private float probeWidth = 70; // probes are 70um wide
    private int zoomLevel = 0;
    private float zoomFactor = 1f;

    private RectTransform _rect;

    private float inPlaneScale;
    private Vector3 recRegionCenterIdx;
    Vector3 rightWorldU;
    Vector3 upWorldU;
    Vector3 forwardWorldU;

    public Texture3D texture;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        ResetRendererParameters();
    }

    public void Startup(Texture3D annotationTexture)
    {
        _gpuSliceRenderer.sharedMaterial.SetTexture("_Volume", annotationTexture);
        Vector4 shape = new Vector4(annotationTexture.width, annotationTexture.height, annotationTexture.depth, 0f);
        _gpuSliceRenderer.sharedMaterial.SetVector("_VolumeSize", shape);
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

        (_, rightWorldU, upWorldU, forwardWorldU) = ProbeManager.ActiveProbeManager.ProbeController.GetTipWorldU();

#if UNITY_EDITOR
        // debug statements
        Debug.DrawRay(startCoordWorldU, upWorldU, Color.green);
        Debug.DrawRay(endCoordWorldU, rightWorldU, Color.red);
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
                shankSpacing = -0.25f;
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

        // the slice's "up" direction is the probe's "backward"
        recRegionCenterIdx = BrainAtlasManager.ActiveReferenceAtlas.World2AtlasIdx(startCoordWorldU +
            -forwardWorldU * recordingSizemmU / 2 +
            -rightWorldU * shankSpacing * centerOffset);

        _gpuSliceRenderer.sharedMaterial.SetFloat("_FourShankProbe", fourShank ? 1f : 0f);
        _gpuSliceRenderer.sharedMaterial.SetFloat("_TwoShankProbe", twoShank ? 1f : 0f);

        inPlaneScale = recordingSizemmU * 1.5f * 1000f / 25f * zoomFactor;


        _gpuSliceRenderer.sharedMaterial.SetVector("_RecordingRegionCenterPosition", recRegionCenterIdx);
        _gpuSliceRenderer.sharedMaterial.SetVector("_RightDirection", rightWorldU);
        // the slice's "up" direction is the probe's "backward"
        _gpuSliceRenderer.sharedMaterial.SetVector("_UpDirection", -forwardWorldU);
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

        int annotation = BrainAtlasManager.ActiveReferenceAtlas.GetAnnotationIdx(inPlanePosition);
        annotation = BrainAtlasManager.ActiveReferenceAtlas.Ontology.RemapID_NoLayers(annotation);

        if (Input.GetMouseButtonDown(0))
        {
            if (annotation > 0)
                _tpmanager.TargetSearchArea(annotation);
        }

        if (Settings.UseAcronyms)
            _areaText.text = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Acronym(annotation);
        else
            _areaText.text = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Name(annotation);
    }

    private Vector3 CalculateInPlanePosition(Vector2 pointerData)
    {
        Vector2 inPlanePosNorm = GetLocalRectPosNormalized(pointerData) * inPlaneScale / 2;
        // Take the tip transform and go out according to the in plane percentage 

        Vector3 inPlanePosition = recRegionCenterIdx + (BrainAtlasManager.ActiveReferenceAtlas.World2Atlas_Vector(forwardWorldU) * -inPlanePosNorm.x + BrainAtlasManager.ActiveReferenceAtlas.World2Atlas_Vector(upWorldU) * inPlanePosNorm.y);
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
