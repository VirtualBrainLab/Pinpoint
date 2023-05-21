using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class CCFModelControl : MonoBehaviour
{
    #region Static fields
    public static CCFModelControl Instance { get; private set; }
    #endregion

    public CCFTree tree;

    //private int lowQualityDepth = 5;
    //private int[] lowQualityDefaults = new int[] { 315, 343, 698, 1089, 512 };
    private int highQualityDepth = 6;
    private int[] highQualityDefaults = new int[] {184, 500, 453, 1057, 677, 247, 669, 31, 972, 44, 714, 95, 254, 22, 541, 922, 698, 895, 1089, 703, 623, 343, 512};
    [SerializeField] private bool loadBerylDepth;

    private int[] defaultNodes;
    private int defaultDepth;

    private static Dictionary<int, Color> ccfAreaColors;
    private Dictionary<int, Color> ccfAreaColorsMinDepth;
    private Dictionary<int, string> ccfAreaAcronyms;
    private Dictionary<int, string> ccfAreaNames;

    // beryl remapping
    private Dictionary<int, int> berylRemap;
    private Dictionary<int, int> cosmosRemap;
    private bool useBerylRemap;
    private bool useCosmosRemap;

    // node lists (for convenience)
    private List<CCFTreeNode> berylNodes;
    private List<CCFTreeNode> cosmosNodes;

    [SerializeField] private bool overrideNetwork;

    [SerializeField] List<Material> brainRegionMaterials;
    [SerializeField] List<string> brainRegionMaterialNames;

    [SerializeField] private float modelScale;

    private Material defaultBrainRegionMaterial;

    private List<CCFTreeNode> defaultLoadedNodes;
    private TaskCompletionSource<bool> defaultLoadedTaskSource;
    private Task defaultLoadedTask;
    private List<Task<bool>> defaultLoadedNodesTasks;

    private int[] missing = { 738, 995 };

    private bool started;

    private void Awake()
    {
        if (Instance != null)
            throw new Exception("Duplicate CCFModelControl class");
        Instance = this;

        // Initialize
        ccfAreaColors = new Dictionary<int, Color>();
        ccfAreaColorsMinDepth = new Dictionary<int, Color>();
        ccfAreaAcronyms = new Dictionary<int, string>();
        ccfAreaNames = new Dictionary<int, string>();
        berylRemap = new Dictionary<int, int>();
        cosmosRemap = new Dictionary<int, int>();
        berylNodes = new List<CCFTreeNode>();
        cosmosNodes = new List<CCFTreeNode>();

        defaultBrainRegionMaterial = brainRegionMaterials[brainRegionMaterialNames.IndexOf("default")];

        defaultNodes = highQualityDefaults;
        //defaultNodes = lowQualityDefaults;
        //if (QualitySettings.GetQualityLevel() >= 5) { defaultNodes = highQualityDefaults; }
        defaultDepth = highQualityDepth;

        defaultLoadedNodes = new List<CCFTreeNode>();

        useBerylRemap = false;

        // Setup async tasks
        defaultLoadedTaskSource = new TaskCompletionSource<bool>();
        defaultLoadedTask = defaultLoadedTaskSource.Task;
        defaultLoadedNodesTasks = new List<Task<bool>>();
    }

    private void Update()
    {
        if (started && (defaultLoadedNodesTasks.Count > 0))
        {
            for (int i = defaultLoadedNodesTasks.Count-1; i >= 0; i--)
            {
                if (defaultLoadedNodesTasks[i].IsCompleted)
                    defaultLoadedNodesTasks.RemoveAt(i);
            }
            if (defaultLoadedNodesTasks.Count == 0)
                defaultLoadedTaskSource.SetResult(true);
        }
    }

    public Task GetDefaultLoadedTask()
    {
        return defaultLoadedTask;
    }

    public void LateStart(bool loadDefaults)
    {
        Debug.Log("Ontology start called");
        if (!overrideNetwork) return;

        LoadCSVData(loadDefaults);
        started = true;
    }

    public void SetBeryl(bool state)
    {
        useBerylRemap = state;
    }

    public bool InDefaults(int ID)
    {
        return defaultNodes.Contains(ID);
    }

    /// <summary>
    /// Load the ontology CSV file (Resources/AllenCCF/ontology_structure_minimal)
    /// Also loads the current default set of areas, set by defaultNodes
    /// </summary>
    private async void LoadCSVData(bool loadDefaults)
    {
        Debug.Log("(CCFMC) Starting remote load of ontology structure file");

        Task<string> ontologyTask = AddressablesRemoteLoader.LoadAllenCCFOntology();
        await ontologyTask;

        Debug.Log("(CCFMC) Ontology structure file loaded");

        List<Dictionary<string, object>> data = CSVReader.ParseText(ontologyTask.Result);

        for (var i = 0; i < data.Count; i++)
        {
            // get the values in the CSV file and add to the tree
            int id = (int)data[i]["id"];
            int atlas_id = (int)data[i]["atlas_id"];
            int depth = (int)data[i]["depth"];
            int parent = (int)data[i]["parent_structure_id"];
            int beryl = (int)data[i]["beryl_id"];
            int cosmos = (int)data[i]["cosmos_id"];
            string name = (string)data[i]["name"];
            string shortName = (string)data[i]["acronym"];
            string hexColorString = data[i]["color_hex_code"].ToString();
            Color color = TP_Utils.Hex2Color(hexColorString);

            if (name.Equals("root"))
            {
                tree = new CCFTree(id, atlas_id, "root", modelScale, color, defaultBrainRegionMaterial);
            }
            else
            {
                // not the root, so add this node 
                CCFTreeNode node = tree.addNode(parent, id, atlas_id, depth, name, shortName, color);

                // check if this node is cosmos or beryl
                if (!missing.Contains(id))
                    if (id == cosmos)
                        cosmosNodes.Add(node);
                    else if (id == beryl)
                        berylNodes.Add(node);

                // If this node should be visible by default, load it now
                if (loadDefaults)
                {
                    if (loadBerylDepth)
                    {
                        if (id == beryl && !missing.Contains(id))
                        {
                            node.LoadNodeModel(true, true);
                            defaultLoadedNodesTasks.Add(node.GetLoadedTask(true));
                            defaultLoadedNodesTasks.Add(node.GetLoadedTask(false));
                            defaultLoadedNodes.Add(node);
                        }
                    }
                    else if (defaultNodes.Contains(id))
                    {
                        // Note: it's fine not to await this asynchronous call, we don't need to use the node model for anything in this function
                        node.LoadNodeModel(true, false);
                        defaultLoadedNodesTasks.Add(node.GetLoadedTask(true));
                        defaultLoadedNodes.Add(node);
                    }
                }

                // Keep track of the colors of areas in the dictionary, these are used to color e.g. neurons in different areas with different colors
                if (!ccfAreaColors.ContainsKey(id))
                    ccfAreaColors.Add(id, color);
                // Now pop up the tree until we find the parent at the defaultDepth
                // then add that color to the min depth list
                CCFTreeNode minDepthParent = node;
                while (minDepthParent.Depth > defaultDepth)
                    minDepthParent = minDepthParent.Parent();
                if (!ccfAreaColorsMinDepth.ContainsKey(id))
                    ccfAreaColorsMinDepth.Add(id, ccfAreaColors[minDepthParent.ID]);

                // Keep track of the acronyms as well
                if (!ccfAreaAcronyms.ContainsKey(id))
                    ccfAreaAcronyms.Add(id, shortName);

                if (!ccfAreaNames.ContainsKey(id))
                    ccfAreaNames.Add(id, name);

                if (!berylRemap.ContainsKey(id))
                    berylRemap.Add(id, beryl);

                if (!cosmosRemap.ContainsKey(id))
                    cosmosRemap.Add(id, cosmos);
            }
        }
    }

    public List<CCFTreeNode> GetDefaultLoadedNodes()
    {
        return defaultLoadedNodes;
    }

    /// <summary>
    /// Convenience function to load all beryl nodes and set visible (if they aren't already loaded)
    /// </summary>
    /// <returns>Beryl nodes</returns>
    public async Task<List<CCFTreeNode>> LoadBerylNodes(bool full)
    {
        List<Task> taskHandles = new List<Task>();
        foreach (CCFTreeNode node in berylNodes)
            if (!node.IsLoaded(full))
            {
                node.LoadNodeModel(full, !full);
                taskHandles.Add(node.GetLoadedTask(full));
            }
        await Task.WhenAll(taskHandles);

        return berylNodes;
    }

    /// <summary>
    /// Convenience function to load all cosmos nodes and set visible (if they aren't already loaded)
    /// </summary>
    /// <returns>Cosmos nodes</returns>
    public async Task<List<CCFTreeNode>> LoadCosmosNodes(bool full)
    {
        List<Task> taskHandles = new List<Task>();
        foreach (CCFTreeNode node in cosmosNodes)
            if (!node.IsLoaded(full))
            {
                node.LoadNodeModel(full, !full);
                taskHandles.Add(node.GetLoadedTask(full));
            }
        await Task.WhenAll(taskHandles);

        return cosmosNodes;
    }

    public CCFTreeNode GetNode(int ID)
    {
        return tree.findNode(ID);
    }

    public static Color GetCCFAreaColor(int ID)
    {
        ID = RemapID(ID);
        if (ccfAreaColors.ContainsKey(ID))
            return ccfAreaColors[ID];
        else
            return Color.black;
    }

    public Color GetCCFAreaColorMinDepth(int ID)
    {
        ID = RemapID(ID);
        if (ccfAreaColorsMinDepth.ContainsKey(ID))
            return ccfAreaColorsMinDepth[ID];
        else
            return Color.black;
    }

    public static string ID2Acronym(int ID)
    {
        ID = RemapID(ID);
        if (Instance.ccfAreaAcronyms.ContainsKey(ID))
            return Instance.ccfAreaAcronyms[ID];
        else
            return "-";
    }

    public int Acronym2ID(string acronym)
    {
        if (ccfAreaAcronyms.ContainsValue(acronym))
            foreach (KeyValuePair<int, string> pair in ccfAreaAcronyms)
                if (pair.Value == acronym)
                    return pair.Key;
        return -1;
    }

    public bool IsAcronym(string acronym)
    {
        return ccfAreaAcronyms.ContainsValue(acronym);
    }

    public static string ID2AreaName(int ID)
    {
        ID = RemapID(ID);
        if (Instance.ccfAreaNames.ContainsKey(ID))
            return Instance.ccfAreaNames[ID];
        else
            return "-";
    }

    public List<int> AreasMatchingAcronym(string match)
    {
        match = match.ToLower();
        List<int> ret = new List<int>();
        foreach (KeyValuePair<int, string> kvp in ccfAreaAcronyms)
            if (kvp.Value.ToLower().Contains(match))
            {
                int currentID = RemapID(kvp.Key);
                if (!ret.Contains(currentID))
                    ret.Add(currentID);
            }
        return ret;
    }

    public List<int> AreasMatchingName(string match)
    {
        match = match.ToLower();
        List<int> ret = new List<int>();
        foreach (KeyValuePair<int, string> kvp in ccfAreaNames)
            if (kvp.Value.ToLower().Contains(match))
            {
                int currentID = RemapID(kvp.Key);
                if (!ret.Contains(currentID))
                    ret.Add(currentID);
            }
        return ret;
    }

    public static int RemapID(int ID)
    {
        // cosmos remapping strictly supercedes beryl remapping
        if (Instance.useCosmosRemap)
            return GetCosmosID(ID);
        else if (Instance.useBerylRemap)
            return GetBerylID(ID);
        else
            return ID;
    }

    public void ChangeMaterial(int ID, string materialName)
    {
        CCFTreeNode node = tree.findNode(ID);
        if (node != null)
            ChangeMaterial(node, materialName);
    }
    public void ChangeMaterialOneSided(int ID, string materialName, bool leftSide)
    {
        CCFTreeNode node = tree.findNode(ID);
        if (node != null)
            ChangeMaterialOneSided(node, materialName, leftSide);
    }

    public void ChangeMaterial(CCFTreeNode node, string materialName)
    {
        if (brainRegionMaterialNames.Contains(materialName))
            node.SetMaterial(brainRegionMaterials[brainRegionMaterialNames.IndexOf(materialName)]);
        else
            Debug.LogWarning("Material name [" + materialName + "] missing from material options");
    }
    public void ChangeMaterialOneSided(CCFTreeNode node, string materialName, bool leftSide)
    {
        if (brainRegionMaterialNames.Contains(materialName))
            node.SetMaterialOneSided(brainRegionMaterials[brainRegionMaterialNames.IndexOf(materialName)], leftSide);
        else
            Debug.LogWarning("Material name [" + materialName + "] missing from material options");
    }

    public static int GetBerylID(int ID)
    {
        if (Instance.berylRemap.ContainsKey(ID))
            return Instance.berylRemap[ID];
        else
            return -1;
    }
    public static int GetCosmosID(int ID)
    {
        if (Instance.cosmosRemap.ContainsKey(ID))
            return Instance.cosmosRemap[ID];
        else
            return -1;
    }
}
