using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using TrajectoryPlanner;
using BrainAtlas;
using Utils.Types;
using Models;
using Models.Scene;
using Models.Settings;
using Services;
using UI;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

public class TP_InPlaneSlice : MonoBehaviour
{
    // In plane slice handling
    [SerializeField] private TrajectoryPlannerManager _tpmanager;
    [SerializeField] private GameObject _inPlaneSliceUigo;

    [SerializeField] private TextMeshProUGUI _areaText;
    [SerializeField] private TMP_Text _textX;
    [SerializeField] private TMP_Text _textY;

    [SerializeField] private Renderer _gpuSliceRenderer;
    [SerializeField] private RenderTexture _inPlaneRenderTexture;
    [SerializeField] private Material _inPlaneSliceMaterial;

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

#if APP_UI
    private StoreService _storeService;
    private IDisposableSubscription _probeWorldStateSubscription;
    private ProbeWorldState _cachedProbeWorldState;
    private IDisposableSubscription _activeProbeStateSubscription;
    private ProbeState _cachedActiveProbeState;
    private IDisposableSubscription _settingsStateSubscription;
#endif

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        ResetRendererParameters();
    }

    public void Startup(Texture3D annotationTexture)
    {
        _inPlaneSliceMaterial.SetTexture("_Volume", annotationTexture);
        Vector4 shape = new Vector4(annotationTexture.width, annotationTexture.height, annotationTexture.depth, 0f);
        _inPlaneSliceMaterial.SetVector("_VolumeSize", shape);
    }

    private void Start()
    {
        _storeService = PinpointApp.Services.GetRequiredService<StoreService>();

        _probeWorldStateSubscription = _storeService.Store.Subscribe(
            state =>
            {
                var sceneState = state.Get<SceneState>(SliceNames.SCENE_SLICE);
                var probeWorldSlice = state.Get<ProbeWorldStateSlice>(SliceNames.PROBE_WORLD_SLICE);
                return probeWorldSlice.GetProbeWorldState(sceneState.ActiveProbeName);
            },
            probeWorldState =>
            {
                _cachedProbeWorldState = probeWorldState;
                UpdateInPlaneSlice();
            },
            new SubscribeOptions<ProbeWorldState> { fireImmediately = true }
        );

        _activeProbeStateSubscription = _storeService.Store.Subscribe(
            state =>
            {
                var sceneState = state.Get<SceneState>(SliceNames.SCENE_SLICE);
                return sceneState.Probes.Find(p => p.Name == sceneState.ActiveProbeName);
            },
            probeState =>
            {
                _cachedActiveProbeState = probeState;
                UpdateInPlaneSlice();
            },
            new SubscribeOptions<ProbeState> { fireImmediately = true }
        );

        _settingsStateSubscription = _storeService.Store.Subscribe(
            state => state.Get<SettingsState>(SliceNames.SETTINGS_SLICE).inPlaneZoom,
            UpdateZoom,
            new SubscribeOptions<int> { fireImmediately = true }
        );
    }

    private void OnDestroy()
    {
        _probeWorldStateSubscription?.Dispose();
        _activeProbeStateSubscription?.Dispose();
        _settingsStateSubscription?.Dispose();
    }

    private void ResetRendererParameters()
    {
        _inPlaneSliceMaterial.SetFloat("_FourShankProbe", 0f);
        _inPlaneSliceMaterial.SetVector("_TipPosition", Vector4.zero);
        _inPlaneSliceMaterial.SetVector("_ForwardDirection", Vector4.zero);
        _inPlaneSliceMaterial.SetVector("_UpDirection", Vector4.zero);
        _inPlaneSliceMaterial.SetFloat("_RecordingRegionSize", 0f);
        _inPlaneSliceMaterial.SetFloat("_Scale", 1f);
        _inPlaneSliceMaterial.SetFloat("_ShankWidth", probeWidth);
    }

    // *** INPLANE SLICE CODE *** //
    public void UpdateInPlaneVisibility()
    {
        _inPlaneSliceUigo.SetActive(Settings.ShowInPlaneSlice);
    }

    public void UpdateInPlaneSlice()
    {
        Debug.Log("here");
#if APP_UI
        if (_cachedProbeWorldState == null || string.IsNullOrEmpty(_cachedProbeWorldState.Name) || _cachedActiveProbeState == null)
        {
            ResetRendererParameters();
            return;
        }

        if (BrainAtlasManager.Instance == null || BrainAtlasManager.ActiveReferenceAtlas == null)
        {
            ResetRendererParameters();
            return;
        }

        Vector3 startCoordWorldU = _cachedProbeWorldState.RecRegionBaseCoordWorldU;
        Vector3 endCoordWorldU = _cachedProbeWorldState.RecRegionTopCoordWorldU;

        rightWorldU = _cachedProbeWorldState.TipRightWorldU;
        upWorldU = _cachedProbeWorldState.TipUpWorldU;
        forwardWorldU = _cachedProbeWorldState.TipForwardWorldU;

        ProbeType activeProbeType = _cachedActiveProbeState.ProbeType;
#else
      if (ProbeManager.ActiveProbeManager == null)
        {
   ResetRendererParameters();
    return;
        }

    if (BrainAtlasManager.Instance == null || BrainAtlasManager.ActiveReferenceAtlas == null)
 {
            ResetRendererParameters();
       return;
        }

      (Vector3 startCoordWorldU, Vector3 endCoordWorldU) = ProbeManager.ActiveProbeManager.RecRegionCoordWorldU;

        (_, rightWorldU, upWorldU, forwardWorldU) = ProbeManager.ActiveProbeManager.ProbeController.GetTipWorldU();

        ProbeType activeProbeType = ProbeManager.ActiveProbeManager.ProbeType;
#endif

#if UNITY_EDITOR
        Debug.DrawRay(startCoordWorldU, upWorldU, Color.green);
        Debug.DrawRay(endCoordWorldU, rightWorldU, Color.red);
#endif

        // Calculate the size
        float recordingSizemmU = Vector3.Distance(startCoordWorldU, endCoordWorldU);

        bool fourShank = false;
        bool twoShank = false;

        float shankSpacing = 0f;
        float centerOffset = 0f;

        switch (activeProbeType)
        {
            case ProbeType.Neuropixels24:
                shankSpacing = -0.25f;
                centerOffset = 1.5f;
                fourShank = true;
                break;

            case ProbeType.Neuropixels24x2:
                shankSpacing = 0.25f;
                centerOffset = 1.5f;
                fourShank = true;
                break;

            case ProbeType.UCLA128K:
                shankSpacing = -0.2f;
                centerOffset = 1.5f;
                fourShank = true;
                break;

            case ProbeType.UCLA256F:
                shankSpacing = -0.5f;
                centerOffset = 0.5f;
                twoShank = true;
                break;
        }
        _inPlaneSliceMaterial.SetFloat("_ShankSpacing", shankSpacing);

        // the slice's "up" direction is the probe's "backward"
        recRegionCenterIdx = BrainAtlasManager.ActiveReferenceAtlas.World2AtlasIdx(startCoordWorldU +
            -forwardWorldU * recordingSizemmU / 2 +
            rightWorldU * shankSpacing * centerOffset);

        _inPlaneSliceMaterial.SetFloat("_FourShankProbe", fourShank ? 1f : 0f);
        _inPlaneSliceMaterial.SetFloat("_TwoShankProbe", twoShank ? 1f : 0f);

        Vector3 resolution = BrainAtlasManager.ActiveReferenceAtlas.Resolution;

        inPlaneScale = recordingSizemmU * 1.5f * 1000f / resolution.x * zoomFactor;

        _inPlaneSliceMaterial.SetVector("_RecordingRegionCenterPosition", recRegionCenterIdx);
        _inPlaneSliceMaterial.SetVector("_RightDirection", rightWorldU);
        // the slice's "up" direction is the probe's "backward"
        _inPlaneSliceMaterial.SetVector("_UpDirection", -forwardWorldU);
        _inPlaneSliceMaterial.SetFloat("_RecordingRegionSize", recordingSizemmU * 1000f / resolution.x);
        _inPlaneSliceMaterial.SetFloat("_Scale", inPlaneScale);
        float roundedMmRecSize = Mathf.Round(recordingSizemmU * 1.5f * zoomFactor * 100) / 100;

        string formatted = $"< {roundedMmRecSize} mm >";
        _textX.text = formatted;
        _textY.text = formatted;

        Graphics.Blit(null, _inPlaneRenderTexture, _inPlaneSliceMaterial);
    }

    public void InPlaneSliceHover(Vector2 pointerData)
    {
#if APP_UI
        if (_cachedProbeWorldState == null || string.IsNullOrEmpty(_cachedProbeWorldState.Name))
            return;
#else
 if (ProbeManager.ActiveProbeManager == null)
   return;
#endif

        if (BrainAtlasManager.Instance == null || BrainAtlasManager.ActiveReferenceAtlas == null)
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
        if (BrainAtlasManager.Instance == null || BrainAtlasManager.ActiveReferenceAtlas == null)
            return Vector3.zero;

        Vector2 inPlanePosNorm = GetLocalRectPosNormalized(pointerData) * inPlaneScale / 2;
        // Take the tip transform and go out according to the in plane percentage 

        // We get the center index, then add the x position * the left vector, then add the y position * the up vector
        // remember that for the probe, up = backward, and left = left
        Vector3 inPlanePosition = recRegionCenterIdx +
 (BrainAtlasManager.ActiveReferenceAtlas.World2Atlas_Vector(-rightWorldU) * inPlanePosNorm.x +
          BrainAtlasManager.ActiveReferenceAtlas.World2Atlas_Vector(-forwardWorldU) * inPlanePosNorm.y);
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

    public void UpdateZoom(int zoomLevel)
    {
        this.zoomLevel = zoomLevel;
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
