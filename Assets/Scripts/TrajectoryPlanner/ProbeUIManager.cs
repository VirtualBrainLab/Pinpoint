using System.Collections.Generic;
using TrajectoryPlanner;
using UnityEngine;
using UnityEngine.UI;

public class ProbeUIManager : MonoBehaviour
{
    [SerializeField] private GameObject probePanelPrefab;
    private GameObject probePanelGO;
    private TP_ProbePanel probePanel;

    [SerializeField] private ProbeManager probeManager;
    private CCFModelControl modelControl;

    [SerializeField] private GameObject electrodeBase;
    [SerializeField] private int order;

    private CCFAnnotationDataset annotationDataset;

    private TrajectoryPlannerManager tpmanager;

    private Color defaultColor;
    private Color selectedColor;

    private bool probeMovedDirty = false;

    private float probePanelPxHeight;
    private float pxStep;

    private const int MINIMUM_AREA_PIXEL_HEIGHT = 7;

    private void Awake()
    {
        Debug.Log("Adding puimanager: " + order);

        // Add the probePanel
        Transform probePanelParentT = GameObject.Find("ProbePanelParent").transform;
        probePanelGO = Instantiate(probePanelPrefab, probePanelParentT);
        probePanel = probePanelGO.GetComponent<TP_ProbePanel>();
        probePanel.RegisterProbeController(probeManager);
        probePanel.RegisterProbeUIManager(this);

        probePanelPxHeight = probePanel.GetPanelHeight();
        pxStep = probePanelPxHeight / 10;

        GameObject main = GameObject.Find("main");
        modelControl = main.GetComponent<CCFModelControl>();

        // Get the annotation dataset
        tpmanager = main.GetComponent<TrajectoryPlannerManager>();
        annotationDataset = tpmanager.GetAnnotationDataset();

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
        }
    }

    public void UpdateColors()
    {
        defaultColor = probeManager.GetColor();
        defaultColor.a = 0.5f;

        selectedColor = defaultColor;
        selectedColor.a = 0.75f;

        ProbeSelected(tpmanager.GetActiveProbeManager() == probeManager);
    }

    public int GetOrder()
    {
        return order;
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
        (float mmStartPos, float mmRecordingSize) = ((DefaultProbeController)probeManager.GetProbeController()).GetRecordingRegionHeight();

        (Vector3 startCoordWorld, Vector3 endCoordWorld) = probeManager.GetProbeController().GetRecordingRegionWorld(electrodeBase.transform);
        Vector3 startApdvlr25 = annotationDataset.CoordinateSpace.World2Space(startCoordWorld);
        Vector3 endApdvlr25 = annotationDataset.CoordinateSpace.World2Space(endCoordWorld);


        List<int> mmTickPositions = new List<int>();
        List<int> tickIdxs = new List<int>();
        List<int> tickHeights = new List<int>(); // this will be calculated in the second step

        if (tpmanager.GetSetting_ShowRecRegionOnly())
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

        probePanel.SetTipData(startApdvlr25, endApdvlr25, mmRecordingSize, tpmanager.GetSetting_ShowRecRegionOnly());

        if (tpmanager.GetSetting_ShowRecRegionOnly())
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
        probePanel.UpdateText(centerHeights, names, tpmanager.ProbePanelTextFS(tpmanager.GetSetting_UseAcronyms()));
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
        //Color[] interpolated = new Color[(int)probePanelPxHeight];
        List<int> heights = new List<int>();
        List<int> pixelHeight = new List<int>();
        List<string> areaNames = new List<string>();

        int prevID = int.MinValue;
        for (int i = 0; i < probePanelPxHeight; i++)
        {
            float perc = i / (probePanelPxHeight-1);
            Vector3 interpolatedPosition = Vector3.Lerp(tipPosition, topPosition, perc);
            // Round to int
            int ID = annotationDataset.ValueAtIndex(Mathf.RoundToInt(interpolatedPosition.x), Mathf.RoundToInt(interpolatedPosition.y), Mathf.RoundToInt(interpolatedPosition.z));
            // convert to Beryl ID (if modelControl is set to do that)
            ID = modelControl.RemapID(ID);
            //interpolated[i] = modelControl.GetCCFAreaColor(ID);

            if (ID != prevID)
            {
                // We have arrived at a new area, get the name and height
                heights.Add(i);
                if (tpmanager.GetSetting_UseAcronyms())
                    areaNames.Add(modelControl.ID2Acronym(ID));
                else
                    areaNames.Add(modelControl.ID2AreaName(ID));

                prevID = ID;
            }
        }

        // Also compute the area heights
        // Now get the centerHeights -- this will be the position we'll show the area text at, so it should be bounded between 0 and the probePanelPxHeight
        // and in theory it should be halfway between each height and the next height, with the exception of the first and last areas
        List<int> centerHeights = new List<int>();
        if (heights.Count > 0)
        {
            if (heights.Count > 1)
            {
                for (int i = 0; i < (heights.Count - 1); i++)
                {
                    centerHeights.Add(Mathf.RoundToInt((heights[i] + heights[i + 1]) / 2f));
                    pixelHeight.Add(heights[i + 1] - heights[i]);
                }
                pixelHeight.Add(heights[heights.Count - 1] - heights[heights.Count - 2]);
            }
            centerHeights.Add(Mathf.RoundToInt((heights[heights.Count - 1] + probePanelPxHeight) / 2f));
        }

        // If there is only one value in the heights array, pixelHeight will be empty
        if (pixelHeight.Count > 0)
        {
            // Remove any areas where heights < MINIMUM_AREA_PIXEL_HEIGHT
            for (int i = heights.Count - 1; i >= 0; i--)
            {
                if (pixelHeight[i] < MINIMUM_AREA_PIXEL_HEIGHT)
                {
                    heights.RemoveAt(i);
                    centerHeights.RemoveAt(i);
                    areaNames.RemoveAt(i);
                }
            }
        }

        return (heights, centerHeights, areaNames);
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
