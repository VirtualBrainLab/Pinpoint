using System.Collections.Generic;
using TrajectoryPlanner;
using UnityEngine;
using UnityEngine.UI;

public class ProbeUIManager : MonoBehaviour
{
    [SerializeField] private GameObject _probePanelPrefab;
    private GameObject _probePanelGO;
    private TP_ProbePanel _probePanel;

    [SerializeField] private ProbeManager _probeManager;
    private CCFModelControl _modelControl;

    [SerializeField] private GameObject _electrodeBase;
    [SerializeField] private int _order;

    private CCFAnnotationDataset _annotationDataset;

    private TrajectoryPlannerManager _tpmanager;

    private Color _defaultColor;
    private Color _selectedColor;

    private bool _probeMovedDirty = false;

    private float _probePanelPxHeight;
    private float _pxStep;

    private const int MINIMUM_AREA_PIXEL_HEIGHT = 7;

    private void Awake()
    {
        Debug.Log("Adding puimanager: " + _order);

        // Add the probePanel
        Transform probePanelParentT = GameObject.Find("ProbePanelParent").transform;
        _probePanelGO = Instantiate(_probePanelPrefab, probePanelParentT);
        _probePanel = _probePanelGO.GetComponent<TP_ProbePanel>();
        _probePanel.RegisterProbeController(_probeManager);
        _probePanel.RegisterProbeUIManager(this);

        _probePanelPxHeight = _probePanel.GetPanelHeight();
        _pxStep = _probePanelPxHeight / 10;

        GameObject main = GameObject.Find("main");
        _modelControl = main.GetComponent<CCFModelControl>();

        // Get the annotation dataset
        _tpmanager = main.GetComponent<TrajectoryPlannerManager>();
        _annotationDataset = _tpmanager.GetAnnotationDataset();

        // Set color properly
        UpdateColors();

        // Set probe to be un-selected
        ProbeSelected(false);
    }

    private void Update()
    {
        if (_probeMovedDirty)
        {
            ProbedMovedHelper();
            _probeMovedDirty = false;
        }
    }

    public void UpdateColors()
    {
        _defaultColor = _probeManager.GetColor();
        _defaultColor.a = 0.5f;

        _selectedColor = _defaultColor;
        _selectedColor.a = 0.75f;

        ProbeSelected(_tpmanager.GetActiveProbeManager() == _probeManager);
    }

    public int GetOrder()
    {
        return _order;
    }

    public void Destroy()
    {
        Destroy(_probePanelGO);
    }

    public void ProbeMoved()
    {
        _probeMovedDirty = true;
    }

    public void UpdateName(string newName)
    {
        _probePanelGO.name = newName;
    }

    public TP_ProbePanel GetProbePanel()
    {
        return _probePanel;
    }

    public void SetProbePanelVisibility(bool state)
    {
        _probePanelGO.SetActive(state);
    }

    private void ProbedMovedHelper()
    {
        // Get the height of the recording region, either we'll show it next to the regions, or we'll use it to restrict the display
        (float mmStartPos, float mmRecordingSize) = ((DefaultProbeController)_probeManager.GetProbeController()).GetRecordingRegionHeight();

        (Vector3 startCoordWorld, Vector3 endCoordWorld) = _probeManager.GetProbeController().GetRecordingRegionWorld(_electrodeBase.transform);
        Vector3 startApdvlr25 = _annotationDataset.CoordinateSpace.World2Space(startCoordWorld);
        Vector3 endApdvlr25 = _annotationDataset.CoordinateSpace.World2Space(endCoordWorld);


        List<int> mmTickPositions = new List<int>();
        List<int> tickIdxs = new List<int>();
        List<int> tickHeights = new List<int>(); // this will be calculated in the second step

        if (_tpmanager.GetSetting_ShowRecRegionOnly())
        {
            // If we are only showing regions from the recording region, we need to offset the tip and end to be just the recording region
            // we also want to save the mm tick positions

            float mmEndPos = mmStartPos + mmRecordingSize;
            List<int> mmPos = new List<int>();
            for (int i = Mathf.Max(1,Mathf.CeilToInt(mmStartPos)); i <= Mathf.Min(9,Mathf.FloorToInt(mmEndPos)); i++)
                mmPos.Add(i); // this is the list of values we are going to have to assign a position to

            int idx = 0;
            for (int y = 0; y < _probePanelPxHeight; y++)
            {
                if (idx >= mmPos.Count)
                    break;

                float um = mmStartPos + (y / _probePanelPxHeight) * (mmEndPos - mmStartPos);
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

        _probePanel.SetTipData(startApdvlr25, endApdvlr25, mmRecordingSize, _tpmanager.GetSetting_ShowRecRegionOnly());

        if (_tpmanager.GetSetting_ShowRecRegionOnly())
        {
            for (int y = 0; y < _probePanelPxHeight; y++)
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
            for (int y = 0; y < _probePanelPxHeight; y++)
            {
                bool depthLine = y > 0 && y < _probePanelPxHeight && y % (int)_pxStep == 0;

                if (depthLine)
                {
                    tickHeights.Add(y);
                    tickIdxs.Add(9 - y / (int)_pxStep);
                }
            }
        }

        _probePanel.UpdateTicks(tickHeights, tickIdxs);
        _probePanel.UpdateText(centerHeights, names, _tpmanager.ProbePanelTextFS(_tpmanager.GetSetting_UseAcronyms()));
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
        for (int i = 0; i < _probePanelPxHeight; i++)
        {
            float perc = i / (_probePanelPxHeight-1);
            Vector3 interpolatedPosition = Vector3.Lerp(tipPosition, topPosition, perc);
            // Round to int
            int ID = _annotationDataset.ValueAtIndex(Mathf.RoundToInt(interpolatedPosition.x), Mathf.RoundToInt(interpolatedPosition.y), Mathf.RoundToInt(interpolatedPosition.z));
            // convert to Beryl ID (if modelControl is set to do that)
            ID = _modelControl.RemapID(ID);
            //interpolated[i] = modelControl.GetCCFAreaColor(ID);

            if (ID != prevID)
            {
                // We have arrived at a new area, get the name and height
                heights.Add(i);
                if (_tpmanager.GetSetting_UseAcronyms())
                    areaNames.Add(_modelControl.ID2Acronym(ID));
                else
                    areaNames.Add(_modelControl.ID2AreaName(ID));

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
            centerHeights.Add(Mathf.RoundToInt((heights[heights.Count - 1] + _probePanelPxHeight) / 2f));
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
            _probePanelGO.GetComponent<Image>().color = _selectedColor;
        else
            _probePanelGO.GetComponent<Image>().color = _defaultColor;
    }

    public void ResizeProbePanel(int newPxHeight)
    {
        _probePanel.ResizeProbePanel(newPxHeight);

        _probePanelPxHeight = _probePanel.GetPanelHeight();
        _pxStep = _probePanelPxHeight / 10;
    }
}
