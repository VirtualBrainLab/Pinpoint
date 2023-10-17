using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TrajectoryPlanner;
using UnityEngine.Serialization;
using System;
using BrainAtlas;

public class TP_Search : MonoBehaviour
{
    [FormerlySerializedAs("tpmanager")] [SerializeField] TrajectoryPlannerManager _tpmanager;

    [FormerlySerializedAs("areaPanelsParentGO")][SerializeField] GameObject _areaPanelsParentGo;
    [FormerlySerializedAs("areaPanelPrefab")][SerializeField] GameObject _areaPanelPrefab;
    [FormerlySerializedAs("maxAreaPanels")][SerializeField] int _maxAreaPanels = 1;

    private List<GameObject> localAreaPanels;
    public List<int> VisibleSearchedAreas { get; private set; }

    private const int ACRONYM_FONT_SIZE = 24;
    private const int FULL_FONT_SIZE = 14;

    void Awake()
    {
        localAreaPanels = new();
        VisibleSearchedAreas = new();

        for (int i = 0; i < _maxAreaPanels; i++)
        {
            GameObject areaPanel = Instantiate(_areaPanelPrefab, _areaPanelsParentGo.transform);
            areaPanel.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                _tpmanager.SetProbeTipPosition2AreaID(areaPanel.GetComponent<TP_SearchAreaPanel>().ID);
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
        if (searchString.Length == 0)
        {
            foreach (GameObject panel in localAreaPanels)
                panel.SetActive(false);
            return;
        }
        
        // Find all areas in the CCF that match this search string
        List<int> matchingAreas = BrainAtlasManager.ActiveReferenceAtlas.Ontology.SearchByAcronym(searchString);
        if (matchingAreas.Contains(997))
            matchingAreas.Remove(997);

        // If acronyms are turned off, add any areas that match by full name as well
        if (!Settings.UseAcronyms)
        {
            List<int> areasMatchingName = BrainAtlasManager.ActiveReferenceAtlas.Ontology.SearchByName(searchString);
            matchingAreas = matchingAreas.Union(areasMatchingName).ToList();
        }

        // if the matching areas is larger than the number of max panels, we'll sort the list
        if (matchingAreas.Count > _maxAreaPanels)
        {
            Debug.Log("Searching for matched searches, bumping these");
            for (int i = 0; i < matchingAreas.Count; i++)
            {
                int id = matchingAreas[i];
                string acronym = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Acronym(id);
                string name = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Name(id);

                if (acronym.Equals(searchString) || name.Equals(searchString))
                {
                    // Move this area to the front
                    matchingAreas.RemoveAt(i);
                    matchingAreas = matchingAreas.Prepend(id).ToList();
                    break;
                }
            }
        }

        for (int i = 0; i < _maxAreaPanels; i++)
        {
            GameObject areaPanel = localAreaPanels[i];
            if (i < matchingAreas.Count)
            {
                int id = matchingAreas[i];
                string acronym = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Acronym(id);
                string name = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Name(id);

                if (Settings.UseAcronyms)
                    areaPanel.GetComponentInChildren<TextMeshProUGUI>().text = acronym;
                else
                    areaPanel.GetComponentInChildren<TextMeshProUGUI>().text = name;
                areaPanel.GetComponent<TP_SearchAreaPanel>().ID = id;
                areaPanel.GetComponent<Image>().color = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Color(id);
                areaPanel.SetActive(true);
            }
            else
                areaPanel.SetActive(false);
        }
    }

    public void ClickArea(GameObject target)
    {
        int targetAreaID = target.GetComponent<TP_SearchAreaPanel>().ID;
        // Depending on whether the node is in the base set or not we will load it temporarily or just set it to have a different material
        SelectBrainArea(targetAreaID);
    }

    public void ClickArea(int annotationID)
    {
        SelectBrainArea(annotationID);
    }


    public async void SelectBrainArea(int targetAreaID)
    {
        bool inDefaults = BrainAtlasManager.ActiveReferenceAtlas.DefaultAreas.Contains(targetAreaID);
        if (VisibleSearchedAreas.Contains(targetAreaID))
        {
            // if this is an active node, either make it transparent again (default node) or hide it (non-default)
            if (inDefaults)
                BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Node(targetAreaID).SetMaterial(BrainAtlasManager.BrainRegionMaterials["default"], OntologyNode.OntologyNodeSide.All);
            else
                BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Node(targetAreaID).SetVisibility(false, OntologyNode.OntologyNodeSide.All);
            VisibleSearchedAreas.Remove(targetAreaID);
        }
        else
        {
            // if this is an inactive node, make it opaque (for both default or non-default)
            if (inDefaults)
                BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Node(targetAreaID).SetMaterial(BrainAtlasManager.BrainRegionMaterials["opaque-lit"], OntologyNode.OntologyNodeSide.All);
            else
            {
                // load the node, then make it opaque
                OntologyNode node = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Node(targetAreaID);
                var loadTask = node.LoadMesh(OntologyNode.OntologyNodeSide.Left);
                await loadTask;

                node.SetVisibility(true, OntologyNode.OntologyNodeSide.Left);
                node.SetVisibility(true, OntologyNode.OntologyNodeSide.Right);
                node.SetMaterial(BrainAtlasManager.BrainRegionMaterials["opaque-lit"], OntologyNode.OntologyNodeSide.Left);
                node.SetMaterial(BrainAtlasManager.BrainRegionMaterials["opaque-lit"], OntologyNode.OntologyNodeSide.Right);
                node.ResetColor();
            }
            VisibleSearchedAreas.Add(targetAreaID);
        }
    }

    public void ClearAllAreas()
    {
        foreach (int targetAreaID in VisibleSearchedAreas)
        {
            if (BrainAtlasManager.ActiveReferenceAtlas.DefaultAreas.Contains(targetAreaID))
            {
                BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Node(targetAreaID).SetMaterial(BrainAtlasManager.BrainRegionMaterials["default"], OntologyNode.OntologyNodeSide.All);
            }
            else
            {
                OntologyNode node = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Node(targetAreaID);
                node.SetVisibility(false, OntologyNode.OntologyNodeSide.Left);
                node.SetVisibility(false, OntologyNode.OntologyNodeSide.Right);
            }
        }
        VisibleSearchedAreas.Clear();
    }

    /// <summary>
    /// Change the font size based on whether the "acronyms only" setting has been updated
    /// </summary>
    public void UpdateSearchFontSize(bool acronyms)
    {
        foreach (GameObject searchPanel in localAreaPanels)
            searchPanel.GetComponent<TP_SearchAreaPanel>().SetFontSize(acronyms ? ACRONYM_FONT_SIZE : FULL_FONT_SIZE);
    }
}
