using System.Collections.Generic;
using System.Linq;
using BrainAtlas;
using Models;
using Models.Scene;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.UIElements;
using Utils.Types;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class AtlasViewModel
    {
        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;

        #region Properties

        [ObservableProperty]
        private string _atlasName;

        [ObservableProperty]
        private List<TreeViewItemData<(string, string, Color, AreaDisplayType)>> _atlasTreeData;

        [ObservableProperty]
        private string _searchText = string.Empty;

        #endregion

        public AtlasViewModel(StoreService storeService)
        {
            _storeService = storeService;

            _sceneStateSubscription = storeService.Store.Subscribe(
                state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                OnSceneStateChanged,
                new SubscribeOptions<SceneState> { fireImmediately = true }
            );

            App.shuttingDown += OnShuttingDown;
        }

        private void OnSceneStateChanged(SceneState state)
        {
            _atlasName = state.AtlasName;
        }

        private void OnShuttingDown()
        {
            _sceneStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }

        #region Commands

        [ICommand]
        private void SelectArea(int areaID)
        {
            // Dispatch the action to rotate the area visibility
            _storeService.Store.Dispatch(SceneActions.ROTATE_AREA_VISIBILITY, areaID);

            Debug.Log($"(AVM) Toggled visibility for area ID {areaID}");
        }

        [ICommand]
        private void LoadAtlasData()
        {
            if (BrainAtlasManager.Instance == null || BrainAtlasManager.ActiveReferenceAtlas == null)
                return;

            var rootId = BrainAtlasManager.ActiveReferenceAtlas.Ontology.Acronym2ID("root");

            var pinpointAtlasManager = GameObject.Find("main").GetComponent<PinpointAtlasManager>();
            var defaultNodeIds = new HashSet<int>();

            if (pinpointAtlasManager != null && pinpointAtlasManager.DefaultNodes != null)
            {
                foreach (var node in pinpointAtlasManager.DefaultNodes)
                {
                    defaultNodeIds.Add(node.ID);
                }
            }

            var visitedNodes = new HashSet<int>();
            var recursionDepth = 0;
            const int MAX_RECURSION_DEPTH = 1000;

            AtlasTreeData = RecursiveParse(rootId);

            var initialVisibility = new Dictionary<int, AreaDisplayType>();

            foreach (var nodeId in defaultNodeIds)
            {
                initialVisibility[nodeId] = AreaDisplayType.Transparent;
            }

            _storeService.Store.Dispatch(SceneActions.INITIALIZE_AREA_VISIBILITY, initialVisibility);

            return;

            List<TreeViewItemData<(string, string, Color, AreaDisplayType)>> RecursiveParse(int nodeId)
            {
                recursionDepth++;
                if (recursionDepth > MAX_RECURSION_DEPTH)
                {
                    Debug.LogError($"Exceeded maximum recursion depth at node {nodeId}");
                    recursionDepth--;
                    return new List<TreeViewItemData<(string, string, Color, AreaDisplayType)>>();
                }

                if (!visitedNodes.Add(nodeId))
                {
                    Debug.LogWarning($"Circular reference detected in atlas ontology at node ID {nodeId}");
                    recursionDepth--;
                    return new List<TreeViewItemData<(string, string, Color, AreaDisplayType)>>();
                }

                var childrenIds = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Children(nodeId);

                if (childrenIds.Count == 0)
                {
                    visitedNodes.Remove(nodeId);
                    recursionDepth--;
                    return new List<TreeViewItemData<(string, string, Color, AreaDisplayType)>>();
                }

                var result = new List<TreeViewItemData<(string, string, Color, AreaDisplayType)>>(childrenIds.Count);

                foreach (var childId in childrenIds)
                {
                    var childData = RecursiveParse(childId);
                    var childName = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Name(childId);
                    var childAcronym = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Acronym(childId);
                    var displayType = defaultNodeIds.Contains(childId) ? AreaDisplayType.Opaque : AreaDisplayType.Hidden;

                    result.Add(new TreeViewItemData<(string, string, Color, AreaDisplayType)>(
                        childId,
                        (childAcronym, childName, BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Color(childId), displayType),
                        childData.Count > 0 ? childData : null
                    ));
                }

                visitedNodes.Remove(nodeId);
                recursionDepth--;
                return result;
            }
        }

        #endregion
    }
}
