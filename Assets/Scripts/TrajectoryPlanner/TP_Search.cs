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

    void Awake()
    {
        localAreaPanels = new List<GameObject>();
        activeBrainAreas = new List<CCFTreeNode>();

        for (int i =0; i< maxAreaPanels; i++)
        {
            GameObject areaPanel = Instantiate(areaPanelPrefab, areaPanelsParentGO.transform);
            areaPanel.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                tpmanager.SetProbeTipPositionToCCFNode(areaPanel.GetComponent<TP_SearchAreaPanel>().GetNode());
            });
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
        // if empty, hide all panels
        if (searchString.Length==0)
        {
            foreach (GameObject panel in localAreaPanels)
                panel.SetActive(false);
            return;
        }
        // otherwise do the search

        // Find all areas in the CCF that match this search string
        List<int> matchingAreas = modelControl.AreasMatchingAcronym(searchString);
        if (matchingAreas.Contains(997))
            matchingAreas.Remove(997);

        if (!tpmanager.GetSetting_UseAcronyms())
        {
            List<int> areasMatchingName = modelControl.AreasMatchingName(searchString);
            matchingAreas = matchingAreas.Union(areasMatchingName).ToList();
        }

        // if the matching areas is larger than the number of max panels, we'll sort the list
        if (matchingAreas.Count > maxAreaPanels)
        {
            Debug.Log("Searching for matched searches, bumping these");
            for (int i = 0; i < matchingAreas.Count; i++)
            {
                int id = matchingAreas[i];
                CCFTreeNode areaNode = modelControl.tree.findNode(id);
                if (areaNode.ShortName.ToLower().Equals(searchString) || areaNode.Name.ToLower().Equals(searchString))
                {
                    matchingAreas.RemoveAt(i);
                    matchingAreas = matchingAreas.Prepend(id).ToList();
                    break;
                }
            }
        }

        for (int i = 0; i < maxAreaPanels; i++)
        {
            GameObject areaPanel = localAreaPanels[i];
            if (i < matchingAreas.Count)
            {
                CCFTreeNode areaNode = modelControl.tree.findNode(matchingAreas[i]);
                if (tpmanager.GetSetting_UseAcronyms())
                    areaPanel.GetComponentInChildren<TextMeshProUGUI>().text = areaNode.ShortName;
                else
                    areaPanel.GetComponentInChildren<TextMeshProUGUI>().text = areaNode.Name;
                areaPanel.GetComponent<TP_SearchAreaPanel>().SetNode(areaNode);
                areaPanel.GetComponent<Image>().color = areaNode.Color;
                areaPanel.SetActive(true);
            }
            else
                areaPanel.SetActive(false);
        }
    }

    public void ClickArea(GameObject target)
    {
        CCFTreeNode targetNode = target.GetComponent<TP_SearchAreaPanel>().GetNode();
        // Depending on whether the node is in the base set or not we will load it temporarily or just set it to have a different material
        SelectBrainArea(targetNode);
    }

    public void ClickArea(int annotationID)
    {
        SelectBrainArea(modelControl.GetNode(annotationID));
    }


    public void SelectBrainArea(CCFTreeNode targetNode)
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
                if (!targetNode.IsLoaded(true))
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

    private async void LoadSearchNode(CCFTreeNode node)
    {
        node.LoadNodeModel(true, false);
        await node.GetLoadedTask(true);
        node.GetNodeTransform().localPosition = Vector3.zero;
        node.GetNodeTransform().localRotation = Quaternion.identity;
        node.SetNodeModelVisibility(true);
        tpmanager.WarpNode(node);
        modelControl.ChangeMaterial(node, "lit");
    }

    public void ChangeWarp()
    {
        foreach (CCFTreeNode node in activeBrainAreas)
            tpmanager.WarpNode(node);
    }
}
