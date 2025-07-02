using UnityEngine;
using TrajectoryPlanner;
using BrainAtlas;
using UnityEditor;

public class AtlasService
{
    TrajectoryPlannerManager _tpManager;
    
    public AtlasService()
    {
        Debug.Log("here1");
    }

    [RuntimeInitializeOnLoadMethod]
    public void Method()
    {
        Debug.Log("here3");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void GetTPManagerReference()
    {
        if (BrainAtlasManager.ActiveReferenceAtlas != null)
            LoadAtlas();
        else
            TrajectoryPlannerManager.Instance.StartupEvent_RefAtlasLoaded.AddListener(LoadAtlas);
    }

    public static void LoadAtlas()
    {
        var ontology = BrainAtlasManager.ActiveReferenceAtlas.Ontology;
        var root = ontology.SearchByAcronym("root");

        OntologyNode rootNode = ontology.ID2Node(root[0]);

        var allIDs = ontology.SearchByName("");

        Debug.Log(allIDs);
    }
}
