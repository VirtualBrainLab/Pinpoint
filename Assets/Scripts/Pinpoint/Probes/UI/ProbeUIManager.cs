using BrainAtlas;
using Pinpoint.CoordinateSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utils.Types;
#if APP_UI
using Models;
using Models.Scene;
using Models.Settings;
using Services;
using UI;
using UI.ViewModels;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
#endif

public class ProbeUIManager : MonoBehaviour
{
    [FormerlySerializedAs("probePanelPrefab")][SerializeField] private GameObject _probePanelPrefab;
    private GameObject probePanelGO;
    private TP_ProbePanel probePanel;

    [FormerlySerializedAs("probeManager")][SerializeField] private ProbeManager _probeManager;

    [FormerlySerializedAs("electrodeBase")][SerializeField] private GameObject _electrodeBase;
    [FormerlySerializedAs("order")][SerializeField] private int _order;

    private bool _selected;

    private bool probeMovedDirty = false;

    private float probePanelPxHeight;

    private const int MINIMUM_AREA_PIXEL_HEIGHT = 7;

    public string MaxArea { get; private set; }

    private void Awake()
    {
        Debug.Log("Adding puimanager: " + _order);

        MaxArea = "";

        // Add the probePanel
        Transform probePanelParentT = GameObject.Find("ProbePanelGO").transform;
        probePanelGO = Instantiate(_probePanelPrefab, probePanelParentT);
        probePanel = probePanelGO.GetComponent<TP_ProbePanel>();
        probePanel.name = $"{_probeManager.name}_panel_{GetOrder()}";
        probePanel.RegisterProbeManager(_probeManager);

        probePanelPxHeight = probePanel.GetPanelHeight();

        GameObject main = GameObject.Find("main");

        ProbeSelected(false);

        _probeManager.UIUpdateEvent.AddListener(UpdateUI);

#if APP_UI
        var storeService = PinpointApp.Services.GetRequiredService<StoreService>();
        var channelMapViewModel = PinpointApp.Services.GetRequiredService<ChannelMapViewModel>();

        probePanel.OnTextDataChanged = (names, percentages) =>
  {
      channelMapViewModel.UpdateTextItems(names, percentages);
  };
#endif
    }

    private async void Start()
    {
        var cmapTask = _probeManager.GetChannelMap();
        await cmapTask;
        probePanel.SetChannelMap(cmapTask.Result.Texture);
    }

    private void Update()
    {
        if (probeMovedDirty)
        {
            ProbedMovedHelper();
            probeMovedDirty = false;
        }
    }

    public Transform ShankTipT()
    {
        return _electrodeBase.transform;
    }

    public void UpdateColors()
    {
        UpdateUIManagerColor();
    }

    public int GetOrder()
    {
        return _order;
    }

    public void Cleanup()
    {
        Destroy(probePanelGO);
    }

    public void UpdateUI()
    {
        probeMovedDirty = true;
    }

    public void UpdateName(string newName)
    {
        probePanelGO.name = newName;
    }

    public TP_ProbePanel GetProbePanel()
    {
        return probePanel;
    }

    public void SetProbePanelVisibility(bool state)
    {
        probePanelGO.SetActive(state);
    }

    private async void ProbedMovedHelper()
    {
        await BrainAtlasManager.ActiveReferenceAtlas.AnnotationsTask;
        await _probeManager.ChannelMapTask;

        var channelCoords = _probeManager.GetChannelRangemm();

        Vector3 startCoordWorldT = _electrodeBase.transform.position + -_electrodeBase.transform.forward * channelCoords.startPosmm;
        Vector3 endCoordWorldT = _electrodeBase.transform.position + -_electrodeBase.transform.forward * channelCoords.endPosmm;

        Vector3 startCoordWorldU = BrainAtlasManager.WorldT2WorldU(startCoordWorldT, true);
        Vector3 endCoordWorldU = BrainAtlasManager.WorldT2WorldU(endCoordWorldT, true);

        Vector3 startAPMLDV = BrainAtlasManager.ActiveReferenceAtlas.World2AtlasIdx(startCoordWorldU);
        Vector3 endAPMLDV = BrainAtlasManager.ActiveReferenceAtlas.World2AtlasIdx(endCoordWorldU);

        (List<int> boundaryHeights, List<float> centerPercentages, List<string> names) = InterpolateAnnotationIDs(startAPMLDV, endAPMLDV);

        probePanel.SetTipData(startAPMLDV, endAPMLDV, 0, 1, channelCoords.recordingSizemm);

#if APP_UI
        probePanel.UpdateText(new List<int>(), names, Settings.UseAcronyms ? ProbeProperties.FONT_SIZE_ACRONYM : ProbeProperties.FONT_SIZE_AREA);
#else
        List<int> centerHeights = centerPercentages.Select(perc => Mathf.RoundToInt(perc * probePanelPxHeight)).ToList();
     probePanel.UpdateText(centerHeights, names, Settings.UseAcronyms ? ProbeProperties.FONT_SIZE_ACRONYM : ProbeProperties.FONT_SIZE_AREA);
#endif
    }

    private (List<int>, List<float>, List<string>) InterpolateAnnotationIDs(Vector3 tipIdxWorldT, Vector3 topIdxWorldT)
    {
        List<int> areaPositionPixels = new();
        List<int> areaHeightPixels = new();
        List<int> areaIDs = new();
        List<string> areaNames = new();
        List<float> centerPercentages = new();

        int prevID = int.MinValue;
        for (int i = 0; i < probePanelPxHeight; i++)
        {
            float perc = i / (probePanelPxHeight - 1);
            Vector3 interpolatedIdxWorldT = Vector3.Lerp(tipIdxWorldT, topIdxWorldT, perc);
            int ID = BrainAtlasManager.ActiveReferenceAtlas.GetAnnotationIdx(interpolatedIdxWorldT);
            ID = BrainAtlasManager.ActiveReferenceAtlas.Ontology.RemapID_NoLayers(ID);

            if (ID != prevID)
            {
                areaPositionPixels.Add(i);
                areaIDs.Add(ID);
                if (Settings.UseAcronyms)
                    areaNames.Add(BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Acronym(ID));
                else
                    areaNames.Add(BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Name(ID));

                int curIdx = areaPositionPixels.Count - 1;
                if (curIdx >= 1)
                {
                    int centerPixel = Mathf.RoundToInt((areaPositionPixels[curIdx - 1] + areaPositionPixels[curIdx]) / 2f);
                    centerPercentages.Add(centerPixel / (float)probePanelPxHeight);
                    areaHeightPixels.Add(areaPositionPixels[curIdx] - areaPositionPixels[curIdx - 1]);
                }

                prevID = ID;
            }
        }

        if (areaPositionPixels.Count > 0)
        {
            int lastCenterPixel = Mathf.RoundToInt((areaPositionPixels[areaPositionPixels.Count - 1] + probePanelPxHeight) / 2f);
            centerPercentages.Add(lastCenterPixel / (float)probePanelPxHeight);
            areaHeightPixels.Add(Mathf.RoundToInt(probePanelPxHeight - areaPositionPixels[areaPositionPixels.Count - 1]));
        }

        int maxAreaID = 0;
        int maxPixelHeight = 0;
        if (areaHeightPixels.Count > 0)
        {
            for (int i = areaPositionPixels.Count - 1; i >= 0; i--)
            {
                if (areaHeightPixels[i] > maxPixelHeight && areaIDs[i] > 0)
                {
                    maxPixelHeight = areaHeightPixels[i];
                    maxAreaID = areaIDs[i];
                }
                if (areaHeightPixels[i] < MINIMUM_AREA_PIXEL_HEIGHT)
                {
                    areaPositionPixels.RemoveAt(i);
                    centerPercentages.RemoveAt(i);
                    areaIDs.RemoveAt(i);
                }
            }
        }

        MaxArea = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Acronym(maxAreaID);

        areaNames = Settings.UseAcronyms ?
            areaIDs.ConvertAll(x => BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Acronym(x)) :
              areaIDs.ConvertAll(x => BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Name(x));

        return (areaPositionPixels, centerPercentages, areaNames);
    }

    public void ProbeSelected(bool selected)
    {
        _selected = selected;
        UpdateUIManagerColor();
    }

    private void UpdateUIManagerColor()
    {
#if APP_UI
        var storeService = PinpointApp.Services.GetRequiredService<StoreService>();
        var sceneState = storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
        var probeState = sceneState.Probes.FirstOrDefault(p => p.Name == _probeManager.name);

        if (probeState != null)
        {
            Color defaultColor = probeState.ColorValue;
            defaultColor.a = 0.5f;

            Color selectedColor = probeState.ColorValue;
            selectedColor.a = 0.75f;

            var imageComponent = probePanelGO.GetComponent<Image>();
            if (imageComponent != null)
            {
                imageComponent.color = _selected ? selectedColor : defaultColor;
            }
        }
#else
        Color defaultColor = _probeManager.Color;
        defaultColor.a = 0.5f;

   Color selectedColor = _probeManager.Color;
        selectedColor.a = 0.75f;

        var imageComponent = probePanelGO.GetComponent<Image>();
        if (imageComponent != null)
        {
            imageComponent.color = _selected ? selectedColor : defaultColor;
        }
#endif
    }

    public void ResizeProbePanel(int newPxHeight)
    {
        probePanel.ResizeProbePanel(newPxHeight);

        probePanelPxHeight = probePanel.GetPanelHeight();

        probePanel.ResizeProbePanel(newPxHeight);
    }
}
