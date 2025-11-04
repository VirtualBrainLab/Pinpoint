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
            var rootId = BrainAtlasManager.ActiveReferenceAtlas.Ontology.Acronym2ID("root");

            // Get the PinpointAtlasManager to check which nodes are loaded
            var pinpointAtlasManager = GameObject.Find("main").GetComponent<PinpointAtlasManager>();
            var defaultNodeIds = new HashSet<int>();

            if (pinpointAtlasManager != null && pinpointAtlasManager.DefaultNodes != null)
            {
                foreach (var node in pinpointAtlasManager.DefaultNodes)
                {
                    defaultNodeIds.Add(node.ID);
                }
            }

            AtlasTreeData = RecursiveParse(rootId);

            // Initialize the brain area visibility state
            var initialVisibility = new Dictionary<int, AreaDisplayType>();

            void CollectNodeIds(List<TreeViewItemData<(string, string, Color, AreaDisplayType)>> items)
            {
                if (items == null) return;

                foreach (var item in items)
                {
                    // Only set initial visibility for nodes that are NOT already in the state
                    // This ensures we respect any existing visibility settings
                    var currentState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
                    if (currentState.BrainAreaVisibility == null || !currentState.BrainAreaVisibility.ContainsKey(item.id))
                    {
                        // Set to Opaque if in default nodes, Hidden otherwise
                        initialVisibility[item.id] = defaultNodeIds.Contains(item.id)
                            ? AreaDisplayType.Opaque
                            : AreaDisplayType.Hidden;
                    }

                    CollectNodeIds(item.children.ToList());
                }
            }

            CollectNodeIds(AtlasTreeData);

            // Update the state with initial visibility
            foreach (var kvp in initialVisibility)
            {
                _storeService.Store.Dispatch(SceneActions.ROTATE_AREA_VISIBILITY, kvp.Key);
                if (kvp.Value == AreaDisplayType.Opaque)
                {
                    // Already set to opaque by the first dispatch
                }
                else if (kvp.Value == AreaDisplayType.Hidden)
                {
                    // Need to rotate twice more to get to Hidden (Opaque -> Transparent -> Hidden)
                    _storeService.Store.Dispatch(SceneActions.ROTATE_AREA_VISIBILITY, kvp.Key);
                    _storeService.Store.Dispatch(SceneActions.ROTATE_AREA_VISIBILITY, kvp.Key);
                }
            }

            return;

            List<TreeViewItemData<(string, string, Color, AreaDisplayType)>> RecursiveParse(int nodeId)
            {
                var childrenIds = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Children(
                    nodeId
                );

                return childrenIds.Count == 0
                    ? null
                    : (
                        from childId in childrenIds
                        let childData = RecursiveParse(childId)
                        let childName = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Name(
                            childId
                        )
                        let childAcronym = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Acronym(
                            childId
                        )
                        let displayType = defaultNodeIds.Contains(childId) ? AreaDisplayType.Opaque : AreaDisplayType.Hidden
                        select new TreeViewItemData<(string, string, Color, AreaDisplayType)>(
                            childId,
                            (childAcronym, childName, BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Color(childId), displayType),
                            childData
                        )
                    ).ToList();
            }
        }

        #endregion
    }
}
