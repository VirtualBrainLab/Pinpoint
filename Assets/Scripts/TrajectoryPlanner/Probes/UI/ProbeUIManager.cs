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
    private CCFModelControl modelControl;

    [FormerlySerializedAs("electrodeBase")] [SerializeField] private GameObject _electrodeBase;
    [FormerlySerializedAs("order")] [SerializeField] private int _order;

    private Color defaultColor;
    private Color selectedColor;

    private bool probeMovedDirty = false;

    private float probePanelPxHeight;
    private float pxStep;

    private const int MINIMUM_AREA_PIXEL_HEIGHT = 7;

    /// <summary>
    /// Area that this probe goes through covering the most pixels
    /// </summary>
    public string MaxArea;

    private void Awake()
    {
        Debug.Log("Adding puimanager: " + _order);

        // initialize vars
        MaxArea = "";

        // Add the probePanel
        Transform probePanelParentT = GameObject.Find("ProbePanelParent").transform;
        probePanelGO = Instantiate(_probePanelPrefab, probePanelParentT);
        probePanel = probePanelGO.GetComponent<TP_ProbePanel>();
        probePanel.RegisterProbeController(_probeManager);
        probePanel.RegisterProbeUIManager(this);

        probePanelPxHeight = probePanel.GetPanelHeight();
        pxStep = probePanelPxHeight / 10;

        GameObject main = GameObject.Find("main");
        modelControl = main.GetComponent<CCFModelControl>();

        // Set color properly
        UpdateColors();

        // Set probe to be un-selected
        ProbeSelected(false);
    }

    private void Update()
    {
        if (probeMovedDirty)
        {
            ProbedMovedHelper();
            probeMovedDirty = false;
            _probeManager.ProbeUIUpdateEvent.Invoke();
        }
    }

    private void UpdateColors()
    {
        defaultColor = _probeManager.GetColor();
        defaultColor.a = 0.5f;

        selectedColor = defaultColor;
        selectedColor.a = 0.75f;
    }

    public int GetOrder()
    {
        return _order;
    }

    public void Destroy()
    {
        Destroy(probePanelGO);
    }

    public void ProbeMoved()
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

    private void ProbedMovedHelper()
    {
        // Get the height of the recording region, either we'll show it next to the regions, or we'll use it to restrict the display
        (float mmStartPos, float mmRecordingSize) = ((DefaultProbeController)_probeManager.GetProbeController()).GetRecordingRegionHeight();

        (Vector3 startCoordWorld, Vector3 endCoordWorld) = _probeManager.GetProbeController().GetRecordingRegionWorld(_electrodeBase.transform);
        Vector3 startApdvlr25 = VolumeDatasetManager.AnnotationDataset.CoordinateSpace.World2Space(startCoordWorld);
        Vector3 endApdvlr25 = VolumeDatasetManager.AnnotationDataset.CoordinateSpace.World2Space(endCoordWorld);


        List<int> mmTickPositions = new List<int>();
        List<int> tickIdxs = new List<int>();
        List<int> tickHeights = new List<int>(); // this will be calculated in the second step

        if (Settings.RecordingRegionOnly)
        {
            // If we are only showing regions from the recording region, we need to offset the tip and end to be just the recording region
            // we also want to save the mm tick positions

            float mmEndPos = mmStartPos + mmRecordingSize;
            List<int> mmPos = new List<int>();
            for (int i = Mathf.Max(1,Mathf.CeilToInt(mmStartPos)); i <= Mathf.Min(9,Mathf.FloorToInt(mmEndPos)); i++)
                mmPos.Add(i); // this is the list of values we are going to have to assign a position to

            int idx = 0;
            for (int y = 0; y < probePanelPxHeight; y++)
            {
                if (idx >= mmPos.Count)
                    break;

                float um = mmStartPos + (y / probePanelPxHeight) * (mmEndPos - mmStartPos);
                if (um >= mmPos[idx])
                {
                    mmTickPositions.Add(y);
                    // We also need to keep track of *what* tick we are at with this position
                    // index 0 = 1000, 1 = 2000, ... 8 = 9000
                    tickIdxs.Add(9 - mmPos[idx]);

                    idx++;
                }
            }
        }
        else
        {
            // apparently we don't save tick positions if we aren't in the reocrding region? This doesn't seem right
        }

        // Interpolate from the tip to the top, putting this data into the probe panel texture
        (List<int> boundaryHeights, List<int> centerHeights, List<string> names) = InterpolateAnnotationIDs(startApdvlr25, endApdvlr25);

        probePanel.SetTipData(startApdvlr25, endApdvlr25, mmRecordingSize, Settings.RecordingRegionOnly);

        if (Settings.RecordingRegionOnly)
        {
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
        }
        else
        {
            // set all the other pixels
            for (int y = 0; y < probePanelPxHeight; y++)
            {
                bool depthLine = y > 0 && y < probePanelPxHeight && y % (int)pxStep == 0;

                if (depthLine)
                {
                    tickHeights.Add(y);
                    tickIdxs.Add(9 - y / (int)pxStep);
                }
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
            float perc = i / (probePanelPxHeight-1);
            Vector3 interpolatedPosition = Vector3.Lerp(tipPosition, topPosition, perc);
            // Round to int
            int ID = VolumeDatasetManager.AnnotationDataset.ValueAtIndex(Mathf.RoundToInt(interpolatedPosition.x), Mathf.RoundToInt(interpolatedPosition.y), Mathf.RoundToInt(interpolatedPosition.z));
            // convert to Beryl ID (if modelControl is set to do that)
            ID = modelControl.RemapID(ID);
            //interpolated[i] = modelControl.GetCCFAreaColor(ID);

            if (ID != prevID)
            {
                // We have arrived at a new area, get the name and height
                areaPositionPixels.Add(i);
                areaIDs.Add(ID);
                //if (Settings.UseAcronyms)
                //    areaNames.Add(modelControl.ID2Acronym(ID));
                //else
                //    areaNames.Add(modelControl.ID2AreaName(ID));
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

        MaxArea = modelControl.ID2Acronym(maxAreaID);

        areaNames = Settings.UseAcronyms ?
            areaIDs.ConvertAll(x => modelControl.ID2Acronym(x)) :
            areaIDs.ConvertAll(x => modelControl.ID2AreaName(x));

        return (areaPositionPixels, centerHeightsPixels, areaNames);
    }

    public void ProbeSelected(bool selected)
    {
        if (selected)
            probePanelGO.GetComponent<Image>().color = selectedColor;
        else
            probePanelGO.GetComponent<Image>().color = defaultColor;
    }

    public void ResizeProbePanel(int newPxHeight)
    {
        probePanel.ResizeProbePanel(newPxHeight);

        probePanelPxHeight = probePanel.GetPanelHeight();
        pxStep = probePanelPxHeight / 10;
    }
}
