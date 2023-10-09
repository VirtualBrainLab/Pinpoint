using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manager class to handle UI functionality related to the probe channel map and area panels
/// </summary>
public class ProbePanelManager : MonoBehaviour
{
    #region Constants
    private const int MAX_VISIBLE_PROBE_PANELS = 16;
    public float DEFAULT_PROBE_PANEL_HEIGHT {get; private set; }
    #endregion

    #region Public vars / Properties
    [SerializeField] GameObject _showAllProbePanelsGO;

    public int VisibleProbePanels { get; private set;}
    #endregion

    #region Unity
    private void Awake()
    {
        DEFAULT_PROBE_PANEL_HEIGHT = 1440f;
    }

    #endregion

    /// <summary>
    /// Count the number of visible probe panels and store this in VisibleProbePanels
    /// 
    /// If the number is larger than the maximum visible count, disable the ability to view all panels
    /// </summary>
    public void CountProbePanels()
    {
        VisibleProbePanels = 0;
        if (Settings.ShowAllProbePanels)
            foreach (ProbeManager probeManager in ProbeManager.Instances)
                VisibleProbePanels += probeManager.GetProbeUIManagers().Count;
        else
            VisibleProbePanels = ProbeManager.ActiveProbeManager != null ? 1 : 0;

        if (VisibleProbePanels > MAX_VISIBLE_PROBE_PANELS)
        {
            // Disable the option for users to show all probe panels
            Settings.ShowAllProbePanels = false;
            _showAllProbePanelsGO.GetComponent<Toggle>().interactable = false;
            _showAllProbePanelsGO.GetComponent<Toggle>().SetIsOnWithoutNotify(false);
        }
        else
            _showAllProbePanelsGO.GetComponent<Toggle>().interactable = true;
    }

    public void SetPanelHeight(float newHeight)
    {
        DEFAULT_PROBE_PANEL_HEIGHT = newHeight;
        RecalculateProbePanels();
    }

    /// <summary>
    /// Re-calculate the size of probe panels to check whether we need to half the vertical height to accommodate more than 8 panels
    /// </summary>
    public void RecalculateProbePanels()
    {
        CountProbePanels();

        // Set number of columns based on whether we need 8 probes or more
        GetComponent<GridLayoutGroup>().constraintCount = (VisibleProbePanels > 8) ? 8 : 4;

        if (VisibleProbePanels > 4)
        {
            // Increase the layout to have two rows, by shrinking all the ProbePanel objects to be 500 pixels tall
            GridLayoutGroup probePanelParent = GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>();
            Vector2 cellSize = probePanelParent.cellSize;
            cellSize.y = DEFAULT_PROBE_PANEL_HEIGHT/2;
            probePanelParent.cellSize = cellSize;

            // now resize all existing probeUIs to be 720 tall
            foreach (ProbeManager probeManager in ProbeManager.Instances)
            {
                probeManager.ResizeProbePanel(Mathf.RoundToInt(DEFAULT_PROBE_PANEL_HEIGHT/2));
            }
        }
        else if (VisibleProbePanels <= 4)
        {
            Debug.Log("Resizing panels to be 1440");
            // now resize all existing probeUIs to be 1400 tall
            GridLayoutGroup probePanelParent = GameObject.Find("ProbePanelParent").GetComponent<GridLayoutGroup>();
            Vector2 cellSize = probePanelParent.cellSize;
            cellSize.y = DEFAULT_PROBE_PANEL_HEIGHT;
            probePanelParent.cellSize = cellSize;

            foreach (ProbeManager probeManager in ProbeManager.Instances)
            {
                probeManager.ResizeProbePanel(Mathf.RoundToInt(DEFAULT_PROBE_PANEL_HEIGHT));
            }
        }

        // Finally, re-order panels if needed to put 2.4 probes first followed by 1.0 / 2.0
        ReOrderProbePanels();
    }

    /// <summary>
    /// Re-order probe panels to make sure that 4-shank probes are grouped together in the top row first, followed by single shanks
    /// </summary>
    private void ReOrderProbePanels()
    {
        Debug.Log("Re-ordering probe panels");
        Dictionary<float, ProbeUIManager> sorted = new Dictionary<float, ProbeUIManager>();

        int probeIndex = 0;
        // first, sort probes so that np2.4 probes go first
        List<ProbeManager> np24Probes = new List<ProbeManager>();
        List<ProbeManager> otherProbes = new List<ProbeManager>();
        foreach (ProbeManager pcontroller in ProbeManager.Instances)
            if (pcontroller.ProbeType == ProbeProperties.ProbeType.Neuropixels24)
                np24Probes.Add(pcontroller);
            else
                otherProbes.Add(pcontroller);
        // now sort by order within each puimanager
        foreach (ProbeManager pcontroller in np24Probes)
        {
            List<ProbeUIManager> puimanagers = pcontroller.GetProbeUIManagers();
            foreach (ProbeUIManager puimanager in pcontroller.GetProbeUIManagers())
                sorted.Add(probeIndex + puimanager.GetOrder() / 10f, puimanager);
            probeIndex++;
        }
        foreach (ProbeManager pcontroller in otherProbes)
        {
            List<ProbeUIManager> puimanagers = pcontroller.GetProbeUIManagers();
            foreach (ProbeUIManager puimanager in pcontroller.GetProbeUIManagers())
                sorted.Add(probeIndex + puimanager.GetOrder() / 10f, puimanager);
            probeIndex++;
        }

        // now sort the list according to the keys
        float[] keys = new float[sorted.Count];
        sorted.Keys.CopyTo(keys, 0);
        Array.Sort(keys);

        // and finally, now put the probe panel game objects in order
        for (int i = 0; i < keys.Length; i++)
        {
            GameObject probePanel = sorted[keys[i]].GetProbePanel().gameObject;
            probePanel.transform.SetAsLastSibling();
        }
    }

}
