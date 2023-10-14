using BrainAtlas;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ProbeUIManager : MonoBehaviour
{
    [FormerlySerializedAs("probePanelPrefab")] [SerializeField] private GameObject _probePanelPrefab;
    private GameObject probePanelGO;
    private TP_ProbePanel probePanel;

    [FormerlySerializedAs("probeManager")] [SerializeField] private ProbeManager _probeManager;

    [FormerlySerializedAs("electrodeBase")] [SerializeField] private GameObject _electrodeBase;
    [FormerlySerializedAs("order")] [SerializeField] private int _order;

    private Color defaultColor;
    private Color selectedColor;
    private bool _selected;

    private bool probeMovedDirty = false;

    private float probePanelPxHeight;

    private const int MINIMUM_AREA_PIXEL_HEIGHT = 7;

    /// <summary>
    /// Area that this probe goes through covering the most pixels
    /// </summary>
    public string MaxArea { get; private set; }

    private void Awake()
    {
        Debug.Log("Adding puimanager: " + _order);

        // initialize vars
        MaxArea = "";

        // Add the probePanel
        Transform probePanelParentT = GameObject.Find("ProbePanelParent").transform;
        probePanelGO = Instantiate(_probePanelPrefab, probePanelParentT);
        probePanel = probePanelGO.GetComponent<TP_ProbePanel>();
        probePanel.name = $"{_probeManager.name}_panel_{GetOrder()}";
        UpdateChannelMap();
        probePanel.RegisterProbeManager(_probeManager);

        probePanelPxHeight = probePanel.GetPanelHeight();

        GameObject main = GameObject.Find("main");

        // Set color properly
        UpdateColors();

        // Set probe to be un-selected
        ProbeSelected(false);

        _probeManager.UIUpdateEvent.AddListener(UpdateUI);
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
        defaultColor = _probeManager.Color;
        defaultColor.a = 0.5f;

        selectedColor = defaultColor;
        selectedColor.a = 0.75f;

        UpdateUIManagerColor();
    }

    public void UpdateChannelMap()
    {
        probePanel.SetChannelMap(_probeManager.ChannelMap.GetChannelMapTexture(_probeManager.SelectionLayerName));
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
        // Make sure the annotations have been loaded
        await BrainAtlasManager.ActiveReferenceAtlas.AnnotationsTask;

        // Get the height of the recording region, either we'll show it next to the regions, or we'll use it to restrict the display
        var channelCoords = _probeManager.GetChannelRangemm();
        ProbeInsertion insertion = _probeManager.ProbeController.Insertion;

        Vector3 startCoordWorldT = _electrodeBase.transform.position + _electrodeBase.transform.up * channelCoords.startPosmm;
        Vector3 endCoordWorldT = _electrodeBase.transform.position + _electrodeBase.transform.up * channelCoords.endPosmm;
        Vector3 startCoordWorldU = insertion.CoordinateSpace.Space2World(insertion.CoordinateTransform.T2U(insertion.CoordinateTransform.U2T_Vector(insertion.CoordinateSpace.World2Space(startCoordWorldT))));
        Vector3 endCoordWorldU = insertion.CoordinateSpace.Space2World(insertion.CoordinateTransform.T2U(insertion.CoordinateTransform.U2T_Vector(insertion.CoordinateSpace.World2Space(endCoordWorldT))));

        Vector3 startApdvlr25 = insertion.CoordinateSpace.World2Space(startCoordWorldU);
        Vector3 endApdvlr25 = insertion.CoordinateSpace.World2Space(endCoordWorldU);

        List<int> mmTickPositions = new List<int>();
        List<int> tickIdxs = new List<int>();
        List<int> tickHeights = new List<int>(); // this will be calculated in the second step

        // If we are only showing regions from the recording region, we need to offset the tip and end to be just the recording region
        // we also want to save the mm tick positions

        List<int> mmPos = new List<int>();
        for (int i = Mathf.Max(1, Mathf.CeilToInt(channelCoords.startPosmm)); i <= Mathf.Min(9, Mathf.FloorToInt(channelCoords.endPosmm)); i++)
            mmPos.Add(i); // this is the list of values we are going to have to assign a position to

        int idx = 0;
        for (int y = 0; y < probePanelPxHeight; y++)
        {
            if (idx >= mmPos.Count)
                break;

            float um = channelCoords.startPosmm + (y / probePanelPxHeight) * channelCoords.recordingSizemm;
            if (um >= mmPos[idx])
            {
                mmTickPositions.Add(y);
                // We also need to keep track of *what* tick we are at with this position
                // index 0 = 1000, 1 = 2000, ... 8 = 9000
                tickIdxs.Add(9 - mmPos[idx]);

                idx++;
            }
        }

        // Interpolate from the tip to the top, putting this data into the probe panel texture
        (List<int> boundaryHeights, List<int> centerHeights, List<string> names) = InterpolateAnnotationIDs(startApdvlr25, endApdvlr25);

        // Get the percentage height along the probe

        // Update probePanel data
        probePanel.SetTipData(startApdvlr25, endApdvlr25, channelCoords.startPosmm / channelCoords.fullHeight, channelCoords.endPosmm / channelCoords.fullHeight, channelCoords.recordingSizemm);

        for (int y = 0; y < probePanelPxHeight; y++)
        {
            // If the mm tick position matches with the position we're at, then add a depth line
            bool depthLine = mmTickPositions.Contains(y);

            // We also want to check if we're at at height line, in which case we'll add a little tick on the right side
            bool heightLine = boundaryHeights.Contains(y);

            if (depthLine)
            {
                tickHeights.Add(y);
            }
        }

        probePanel.UpdateTicks(tickHeights, tickIdxs);
        probePanel.UpdateText(centerHeights, names, Settings.UseAcronyms ? ProbeProperties.FONT_SIZE_ACRONYM : ProbeProperties.FONT_SIZE_AREA);
    }

    /// <summary>
    /// Compute the annotation acronyms/names along a vector from tipPosition to topPosition, saving the pixel positions and center points of each area
    /// 
    /// This function ignores areas where the area height is less than X pixels (defined above)
    /// </summary>
    /// <param name="tipPosition"></param>
    /// <param name="topPosition"></param>
    /// <returns></returns>
    private (List<int>, List<int>, List<string>)  InterpolateAnnotationIDs(Vector3 tipPosition, Vector3 topPosition)
    {
        // pixel height at which changes happen
        List<int> areaPositionPixels = new();
        // pixel count for each area
        List<int> areaHeightPixels = new();
        // area IDs
        List<int> areaIDs = new();
        // string name
        List<string> areaNames = new();
        // center position of each area
        List<int> centerHeightsPixels = new();

        int prevID = int.MinValue;
        for (int i = 0; i < probePanelPxHeight; i++)
        {
            float perc = i / (probePanelPxHeight - 1);
            Vector3 interpolatedPosition = Vector3.Lerp(tipPosition, topPosition, perc);
            // Round to int

            int ID = BrainAtlasManager.ActiveReferenceAtlas.GetAnnotationIdx(interpolatedPosition);
            // convert to Beryl ID (if modelControl is set to do that)
            ID = BrainAtlasManager.ActiveReferenceAtlas.Ontology.RemapID_NoLayers(ID);

            if (ID != prevID)
            {
                // We have arrived at a new area, get the name and height
                areaPositionPixels.Add(i);
                areaIDs.Add(ID);
                if (Settings.UseAcronyms)
                    areaNames.Add(BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Acronym(ID));
                else
                    areaNames.Add(BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Name(ID));
                // Now compute the center height for the *previous* area, and the pixel height
                int curIdx = areaPositionPixels.Count - 1;
                if (curIdx >= 1)
                {
                    centerHeightsPixels.Add(Mathf.RoundToInt((areaPositionPixels[curIdx - 1] + areaPositionPixels[curIdx]) / 2f));
                    areaHeightPixels.Add(areaPositionPixels[curIdx] - areaPositionPixels[curIdx - 1]);
                }

                prevID = ID;
            }
        }

        // The top region (last) will be missing it's center height and area height, so compute those now
        if (areaPositionPixels.Count > 0)
        {
            centerHeightsPixels.Add(Mathf.RoundToInt((areaPositionPixels[areaPositionPixels.Count - 1] + probePanelPxHeight) / 2f));
            areaHeightPixels.Add(Mathf.RoundToInt(probePanelPxHeight - areaPositionPixels[areaPositionPixels.Count - 1]));
        }

        // If there is only one value in the heights array, pixelHeight will be empty
        // Also find the area with the maximum pixel height
        int maxAreaID = 0;
        int maxPixelHeight = 0;
        if (areaHeightPixels.Count > 0)
        {
            // Remove any areas where heights < MINIMUM_AREA_PIXEL_HEIGHT
            for (int i = areaPositionPixels.Count - 1; i >= 0; i--)
            {
                // Get the max area, ignoring "-"
                // This is safe to do even though we remove areas afterward, because we are going backwards through the list
                if (areaHeightPixels[i] > maxPixelHeight && areaIDs[i] > 0)
                {
                    maxPixelHeight = areaHeightPixels[i];
                    maxAreaID = areaIDs[i];
                }
                // Remove areas that are too small
                if (areaHeightPixels[i] < MINIMUM_AREA_PIXEL_HEIGHT)
                {
                    areaPositionPixels.RemoveAt(i);
                    centerHeightsPixels.RemoveAt(i);
                    areaIDs.RemoveAt(i);
                }
            }
        }

        MaxArea = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Acronym(maxAreaID);

        areaNames = Settings.UseAcronyms ?
            areaIDs.ConvertAll(x => BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Acronym(x)) :
            areaIDs.ConvertAll(x => BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Name(x));

        return (areaPositionPixels, centerHeightsPixels, areaNames);
    }

    public void ProbeSelected(bool selected)
    {
        _selected = selected;
        UpdateUIManagerColor();
    }

    private void UpdateUIManagerColor()
    {
        if (_selected)
            probePanelGO.GetComponent<Image>().color = selectedColor;
        else
            probePanelGO.GetComponent<Image>().color = defaultColor;
    }

    public void ResizeProbePanel(int newPxHeight)
    {
        probePanel.ResizeProbePanel(newPxHeight);

        probePanelPxHeight = probePanel.GetPanelHeight();

        probePanel.ResizeProbePanel(newPxHeight);
    }
}
