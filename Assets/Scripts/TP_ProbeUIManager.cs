using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class TP_ProbeUIManager : MonoBehaviour
{
    [SerializeField] private GameObject probePanelPrefab;
    private GameObject probePanelGO;
    private TP_ProbePanel probePanel;

    [SerializeField] private TP_ProbeController probeController;
    private CCFModelControl modelControl;

    [SerializeField] private GameObject probeTip;
    [SerializeField] private int order;
    private GameObject probeTipOffset;
    private GameObject probeEndOffset;

    private AnnotationDataset annotationDataset;

    private TP_TrajectoryPlannerManager tpmanager;

    private Color defaultColor;
    private Color selectedColor;

    private bool probeMovedDirty = false;

    private float probePanelPxHeight;
    private float pxStep;

    private Utils utils;

    private void Awake()
    {
        Debug.Log("Adding puimanager: " + order);

        // Add the probePanel
        Transform probePanelParentT = GameObject.Find("ProbePanelParent").transform;
        probePanelGO = Instantiate(probePanelPrefab, probePanelParentT);
        probePanel = probePanelGO.GetComponent<TP_ProbePanel>();
        probePanel.RegisterProbeController(probeController);

        probePanelPxHeight = probePanel.GetPanelHeight();
        pxStep = probePanelPxHeight / 10;

        GameObject main = GameObject.Find("main");
        modelControl = main.GetComponent<CCFModelControl>();
        utils = main.GetComponent<Utils>();

        // Get the annotation dataset
        tpmanager = main.GetComponent<TP_TrajectoryPlannerManager>();
        annotationDataset = tpmanager.GetAnnotationDataset();

        // Set color properly
        defaultColor = probeController.GetColor();
        defaultColor.a = 0.5f;

        selectedColor = defaultColor;
        selectedColor.a = 0.75f;

        // Set probe to be un-selected
        ProbeSelected(false);

        probeTipOffset = new GameObject("TipOffset");
        probeTipOffset.transform.position = probeTip.transform.position + probeTip.transform.up * 0.2f;
        probeTipOffset.transform.parent = probeTip.transform;
        probeEndOffset = new GameObject("EndOffset");
        probeEndOffset.transform.position = probeTip.transform.position + probeTip.transform.up * 10.2f;
        probeEndOffset.transform.parent = probeTip.transform;
    }

    private void Update()
    {
        if (probeMovedDirty)
        {
            ProbedMovedHelper();
            probeMovedDirty = false;
        }
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

    private void ProbedMovedHelper()
    {
        // Get the height of the recording region, either we'll show it next to the regions, or we'll use it to restrict the display
        float[] heightPerc = probeController.GetRecordingRegionHeight();
        //Debug.Log(heightPerc[0] + " " + heightPerc[1]);

        // (1) Get the position of the probe tip and interpolate through the annotation dataset to the top of the probe
        Vector3 tip_apdvlr;
        Vector3 top_apdvlr;
        List<int> mmTickPositions = new List<int>();
        List<int> tickIdxs = new List<int>();
        List<int> tickHeights = new List<int>(); // this will be calculated in the second step

        if (tpmanager.RecordingRegionOnly())
        {
            // If we are only showing regions from the recording region, we need to offset the tip and end to be just the recording region
            // we also want to save the mm tick positions
            float mmStartPos = heightPerc[0] * (10 - heightPerc[1]);
            float mmRecordingSize = heightPerc[1];
            float mmEndPos = mmStartPos + mmRecordingSize;
            // shift the starting tipPos up by the mmStartPos
            Vector3 tipPos = probeTipOffset.transform.position + probeTipOffset.transform.up * (0.2f + mmStartPos);
            // shift the tipPos again to get the endPos
            Vector3 endPos = tipPos + probeTipOffset.transform.up * mmRecordingSize;
            //GameObject.Find("recording_bot").transform.position = tipPos;
            //GameObject.Find("recording_top").transform.position = endPos;
            tip_apdvlr = utils.WorldSpace2apdvlr(tipPos + tpmanager.GetCenterOffset());
            top_apdvlr = utils.WorldSpace2apdvlr(endPos + tpmanager.GetCenterOffset());
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
            tip_apdvlr = utils.WorldSpace2apdvlr(probeTipOffset.transform.position + probeTipOffset.transform.up * 0.2f + tpmanager.GetCenterOffset());
            top_apdvlr = utils.WorldSpace2apdvlr(probeEndOffset.transform.position + tpmanager.GetCenterOffset());
            //GameObject.Find("recording_bot").transform.position = probeTipOffset.transform.position;
            //GameObject.Find("recording_top").transform.position = probeEndOffset.transform.position;
        }

        // Interpolate from the tip to the top, putting this data into the probe panel texture
        (List<int> boundaryHeights, List<int> centerHeights, List<string> names) = InterpolateAnnotationIDs(tip_apdvlr, top_apdvlr);

        probePanel.SetTipData(tip_apdvlr, top_apdvlr, heightPerc[1], tpmanager.RecordingRegionOnly());

        if (tpmanager.RecordingRegionOnly())
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

                //for (int x = 0; x < 25; x++)
                //{
                //    // First check for the height line, draw that in black
                //    // then check the depth line
                //    // then finally do the color
                //    if (heightLine && x > 15)
                //        probePanel.SetPixel(x, y, Color.black);
                //    else if (depthLine)
                //        probePanel.SetPixel(x, y, Color.gray);
                //    else
                //        probePanel.SetPixel(x, y, interpolatedColors[y]);
                //}
            }
        }
        else
        {
            // save the first five pixels for the recording array height
            int bottomRecord = Mathf.RoundToInt(heightPerc[0] * (probePanelPxHeight - heightPerc[1]/10 * probePanelPxHeight));
            int topRecord = Mathf.RoundToInt(bottomRecord + heightPerc[1]/10 * probePanelPxHeight);
            // set all the other pixels
            for (int y = 0; y < probePanelPxHeight; y++)
            {
                bool depthLine = y > 0 && y < probePanelPxHeight && y % (int)pxStep == 0;

                if (depthLine)
                {
                    tickHeights.Add(y);
                    tickIdxs.Add(9 - y / (int)pxStep);
                }

                //for (int x = 0; x < 5; x++)
                //    if (y >= bottomRecord && y <= topRecord)
                //        probePanel.SetPixel(x, y, Color.red);
                //    else
                //        probePanel.SetPixel(x, y, Color.black);

                //for (int x = 5; x < 25; x++)
                //    // if we are at a 1000 um depth line color it gray
                //    if (depthLine)
                //    {
                //        probePanel.SetPixel(x, y, Color.gray);
                //    }
                //    else
                //    // otherwise give it a real color
                //        probePanel.SetPixel(x, y, interpolatedColors[y]);
            }
        }
        //probePanel.ApplyTex();

        probePanel.UpdateTicks(tickHeights, tickIdxs);
        probePanel.UpdateText(centerHeights, names, tpmanager.ProbePanelTextFS(tpmanager.UseAcronyms()));
    }

    private (List<int>, List<int>, List<string>)  InterpolateAnnotationIDs(Vector3 tipPosition, Vector3 topPosition)
    {
        //Color[] interpolated = new Color[(int)probePanelPxHeight];
        List<int> heights = new List<int>();
        List<string> areaNames = new List<string>();

        int prevID = int.MinValue;
        for (int i = 0; i < probePanelPxHeight; i++)
        {
            float perc = i / (probePanelPxHeight-1);
            Vector3 interpolatedPosition = Vector3.Lerp(tipPosition, topPosition, perc);
            // Round to int
            int ID = annotationDataset.ValueAtIndex(Mathf.RoundToInt(interpolatedPosition.x), Mathf.RoundToInt(interpolatedPosition.y), Mathf.RoundToInt(interpolatedPosition.z));
            // convert to Beryl ID (if modelControl is set to do that)
            ID = modelControl.GetCurrentID(ID);
            //interpolated[i] = modelControl.GetCCFAreaColor(ID);

            if (ID != prevID)
            {
                // We have arrived at a new area, get the name and height
                heights.Add(i);
                if (tpmanager.UseAcronyms())
                    areaNames.Add(modelControl.GetCCFAreaAcronym(ID));
                else
                    areaNames.Add(modelControl.GetCCFAreaName(ID));

                prevID = ID;
            }
        }

        // Now get the centerHeights -- this will be the position we'll show the area text at, so it should be bounded between 0 and the probePanelPxHeight
        // and in theory it should be halfway between each height and the next height, with the exception of the first and last areas
        List<int> centerHeights = new List<int>();
        if (heights.Count > 0)
        {
            for (int i = 0; i < (heights.Count - 1); i++)
            {
                centerHeights.Add(Mathf.RoundToInt((heights[i] + heights[i + 1]) / 2f));
            }
            centerHeights.Add(Mathf.RoundToInt((heights[heights.Count - 1] + probePanelPxHeight) / 2f));
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
