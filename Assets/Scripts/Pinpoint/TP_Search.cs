using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TrajectoryPlanner;
using UnityEngine.Serialization;
using System;

public class TP_Search : MonoBehaviour
{
    //[FormerlySerializedAs("tpmanager")] [SerializeField] TrajectoryPlannerManager _tpmanager;
    //[FormerlySerializedAs("modelControl")] [SerializeField] CCFModelControl _modelControl;

    //[FormerlySerializedAs("areaPanelsParentGO")] [SerializeField] GameObject _areaPanelsParentGo;
    //[FormerlySerializedAs("areaPanelPrefab")] [SerializeField] GameObject _areaPanelPrefab;
    //[FormerlySerializedAs("maxAreaPanels")] [SerializeField] int _maxAreaPanels = 1;

    //private List<GameObject> localAreaPanels;
    //public List<CCFTreeNode> activeBrainAreas { get; private set; }

    //private const int ACRONYM_FONT_SIZE = 24;
    //private const int FULL_FONT_SIZE = 14;

    void Awake()
    {
        throw new NotImplementedException();
        //localAreaPanels = new List<GameObject>();
        //activeBrainAreas = new List<CCFTreeNode>();

        //for (int i = 0; i < _maxAreaPanels; i++)
        //{
        //    GameObject areaPanel = Instantiate(_areaPanelPrefab, _areaPanelsParentGo.transform);
        //    areaPanel.GetComponentInChildren<Button>().onClick.AddListener(() =>
        //    {
        //        _tpmanager.SetProbeTipPositionToCCFNode(areaPanel.GetComponent<TP_SearchAreaPanel>().Node);
        //    });
        //    localAreaPanels.Add(areaPanel);
        //    areaPanel.SetActive(false);
        //}
    }

    ///// <summary>
    ///// Called when updating acronyms <-> area names
    ///// </summary>
    //public void RefreshSearchWindow()
    //{
    //    ChangeSearch(GetComponentInChildren<TMP_InputField>().text);
    //}

    //public void ChangeSearch(string searchString)
    //{
    //    // if empty, hide all panels
    //    if (searchString.Length==0)
    //    {
    //        foreach (GameObject panel in localAreaPanels)
    //            panel.SetActive(false);
    //        return;
    //    }
    //    // otherwise do the search

    //    // Find all areas in the CCF that match this search string
    //    List<int> matchingAreas = _modelControl.AreasMatchingAcronym(searchString);
    //    if (matchingAreas.Contains(997))
    //        matchingAreas.Remove(997);

    //    if (!Settings.UseAcronyms)
    //    {
    //        List<int> areasMatchingName = _modelControl.AreasMatchingName(searchString);
    //        matchingAreas = matchingAreas.Union(areasMatchingName).ToList();
    //    }

    //    // if the matching areas is larger than the number of max panels, we'll sort the list
    //    if (matchingAreas.Count > _maxAreaPanels)
    //    {
    //        Debug.Log("Searching for matched searches, bumping these");
    //        for (int i = 0; i < matchingAreas.Count; i++)
    //        {
    //            int id = matchingAreas[i];
    //            CCFTreeNode areaNode = _modelControl.tree.findNode(id);
    //            if (areaNode.ShortName.ToLower().Equals(searchString) || areaNode.Name.ToLower().Equals(searchString))
    //            {
    //                matchingAreas.RemoveAt(i);
    //                matchingAreas = matchingAreas.Prepend(id).ToList();
    //                break;
    //            }
    //        }
    //    }

    //    for (int i = 0; i < _maxAreaPanels; i++)
    //    {
    //        GameObject areaPanel = localAreaPanels[i];
    //        if (i < matchingAreas.Count)
    //        {
    //            CCFTreeNode areaNode = _modelControl.tree.findNode(matchingAreas[i]);
    //            if (Settings.UseAcronyms)
    //                areaPanel.GetComponentInChildren<TextMeshProUGUI>().text = areaNode.ShortName;
    //            else
    //                areaPanel.GetComponentInChildren<TextMeshProUGUI>().text = areaNode.Name;
    //            areaPanel.GetComponent<TP_SearchAreaPanel>().Node = areaNode;
    //            areaPanel.GetComponent<Image>().color = areaNode.Color;
    //            areaPanel.SetActive(true);
    //        }
    //        else
    //            areaPanel.SetActive(false);
    //    }
    //}

    //public void ClickArea(GameObject target)
    //{
    //    CCFTreeNode targetNode = target.GetComponent<TP_SearchAreaPanel>().Node;
    //    // Depending on whether the node is in the base set or not we will load it temporarily or just set it to have a different material
    //    SelectBrainArea(targetNode);
    //}

    //public void ClickArea(int annotationID)
    //{
    //    SelectBrainArea(_modelControl.GetNode(annotationID));
    //}


    //public void SelectBrainArea(CCFTreeNode targetNode)
    //{
    //    // if this is an active node, just make it transparent again
    //    if (activeBrainAreas.Contains(targetNode))
    //    {
    //        if (_modelControl.InDefaults(targetNode.ID))
    //            _modelControl.ChangeMaterial(targetNode, "default");
    //        else
    //            targetNode.SetNodeModelVisibility(false);
    //        activeBrainAreas.Remove(targetNode);
    //    }
    //    else
    //    {

    //        if (_modelControl.InDefaults(targetNode.ID))
    //            _modelControl.ChangeMaterial(targetNode, "lit");
    //        else
    //        {
    //            if (!targetNode.IsLoaded(true))
    //                LoadSearchNode(targetNode);
    //            else
    //            {
    //                targetNode.SetNodeModelVisibility(true);
    //                _modelControl.ChangeMaterial(targetNode, "lit");
    //            }
    //        }
    //        activeBrainAreas.Add(targetNode);
    //    }
    //}

    //public void ClearAllAreas()
    //{
    //    foreach (CCFTreeNode targetNode in activeBrainAreas)
    //    {
    //        if (_modelControl.InDefaults(targetNode.ID))
    //        {
    //            _modelControl.ChangeMaterial(targetNode, "default");
    //        }
    //        else
    //        {
    //            targetNode.SetNodeModelVisibility(false);
    //        }
    //    }
    //    activeBrainAreas = new List<CCFTreeNode>();
    //}

    //private async void LoadSearchNode(CCFTreeNode node)
    //{
    //    node.LoadNodeModel(true, false);
    //    await node.GetLoadedTask(true);
    //    node.GetNodeTransform().localPosition = Vector3.zero;
    //    node.GetNodeTransform().localRotation = Quaternion.identity;
    //    node.SetNodeModelVisibility(true);
    //    _tpmanager.WarpNode(node);
    //    _modelControl.ChangeMaterial(node, "lit");
    //}

    //public void ChangeWarp()
    //{
    //    foreach (CCFTreeNode node in activeBrainAreas)
    //        _tpmanager.WarpNode(node);
    //}

    ///// <summary>
    ///// Change the font size based on whether the "acronyms only" setting has been updated
    ///// </summary>
    //public void UpdateSearchFontSize(bool acronyms)
    //{
    //    foreach (GameObject searchPanel in localAreaPanels)
    //        searchPanel.GetComponent<TP_SearchAreaPanel>().SetFontSize(acronyms ? ACRONYM_FONT_SIZE : FULL_FONT_SIZE);
    //}
}
