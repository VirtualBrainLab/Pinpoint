using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TrajectoryPlanner;

public class TP_Search : MonoBehaviour
{
    [SerializeField] TrajectoryPlannerManager tpmanager;
    [SerializeField] CCFModelControl modelControl;

    [SerializeField] GameObject areaPanelsParentGO;
    [SerializeField] GameObject areaPanelPrefab;
    [SerializeField] int maxAreaPanels = 1;

    private List<GameObject> localAreaPanels;
    private List<CCFTreeNode> activeBrainAreas;

    // Start is called before the first frame update
    void Start()
    {
        localAreaPanels = new List<GameObject>();
        activeBrainAreas = new List<CCFTreeNode>();

        for (int i =0; i< maxAreaPanels; i++)
        {
            GameObject areaPanel = Instantiate(areaPanelPrefab, areaPanelsParentGO.transform);
            localAreaPanels.Add(areaPanel);
            areaPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Called when updating acronyms <-> area names
    /// </summary>
    public void RefreshSearchWindow()
    {
        ChangeSearch(GetComponentInChildren<TMP_InputField>().text);
    }

    public void ChangeSearch(string searchString)
    {

        // Find all areas in the CCF that match this search string
        List<int> areasMatchingAcronym = modelControl.AreasMatchingAcronym(searchString);
        List<int> areasMatchingName = modelControl.AreasMatchingName(searchString);

        List<int> matchingAreas = areasMatchingAcronym.Union(areasMatchingName).ToList<int>();
        

        for (int i = 0; i < maxAreaPanels; i++)
        {
            GameObject areaPanel = localAreaPanels[i];
            if (i < matchingAreas.Count)
            {
                CCFTreeNode areaNode = modelControl.tree.findNode(matchingAreas[i]);
                if (tpmanager.UseAcronyms())
                    areaPanel.GetComponentInChildren<TextMeshProUGUI>().text = areaNode.ShortName;
                else
                    areaPanel.GetComponentInChildren<TextMeshProUGUI>().text = areaNode.Name;
                areaPanel.GetComponent<TP_SearchAreaPanel>().SetNode(areaNode);
                areaPanel.GetComponent<Image>().color = areaNode.GetColor();
                areaPanel.SetActive(true);
            }
            else
                areaPanel.SetActive(false);
        }
    }

    private CCFTreeNode lastClickedNode;
    private float lastClickedTime;
    private const float DOUBLE_CLICK_TIME = 0.3f;

    public void ClickArea(GameObject target)
    {
        CCFTreeNode targetNode = target.GetComponent<TP_SearchAreaPanel>().GetNode();
        // Depending on whether the node is in the base set or not we will load it temporarily or just set it to have a different material

        // Check if this was a double click
        if ((Time.realtimeSinceStartup - lastClickedTime) < DOUBLE_CLICK_TIME && lastClickedNode == targetNode)
        {
            // double click -- snap the probe tip to this location
            tpmanager.SetProbeTipPositionToCCFNode(targetNode);
        }
        else
        {
            // if this is an active node, just make it transparent again
            if (activeBrainAreas.Contains(targetNode))
            {
                if (modelControl.InDefaults(targetNode.ID))
                    modelControl.ChangeMaterial(targetNode, "default");
                else
                    targetNode.SetNodeModelVisibility(false);
                activeBrainAreas.Remove(targetNode);
            }
            else
            {
                if (modelControl.InDefaults(targetNode.ID))
                    modelControl.ChangeMaterial(targetNode, "lit");
                else
                {
                    if (!targetNode.IsLoaded())
                        LoadSearchNode(targetNode);
                    else
                    {
                        targetNode.SetNodeModelVisibility(true);
                        modelControl.ChangeMaterial(targetNode, "lit");
                    }
                }
                activeBrainAreas.Add(targetNode);
            }
        }

        lastClickedTime = Time.realtimeSinceStartup;
        lastClickedNode = targetNode;
    }

    private async void LoadSearchNode(CCFTreeNode node)
    {
        await node.loadNodeModel(false);
        node.GetNodeTransform().localPosition = Vector3.zero;
        node.GetNodeTransform().localRotation = Quaternion.identity;
        node.SetNodeModelVisibility(true);
        modelControl.ChangeMaterial(node, "lit");
    }

    public void ClearAllAreas()
    {
        foreach (CCFTreeNode targetNode in activeBrainAreas)
        {
            if (modelControl.InDefaults(targetNode.ID))
            {
                modelControl.ChangeMaterial(targetNode, "default");
            }
            else
            {
                targetNode.SetNodeModelVisibility(false);
            }
        }
        activeBrainAreas = new List<CCFTreeNode>();
    }
}
