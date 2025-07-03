using BrainAtlas;
using UnityEngine;

namespace Services
{
    public class AtlasService
    {
        // TrajectoryPlannerManager _tpManager;
    
        public AtlasService()
        {
        }

        public void LoadActiveReferenceAtlas()
        {
            var ontology = BrainAtlasManager.ActiveReferenceAtlas.Ontology;
            var root = ontology.SearchByAcronym("root");

            OntologyNode rootNode = ontology.ID2Node(root[0]);

            var allIDs = ontology.SearchByName("");

            // foreach (var id in allIDs)
            // {
            //     Debug.Log(id);
            // }
        }
    }
}
