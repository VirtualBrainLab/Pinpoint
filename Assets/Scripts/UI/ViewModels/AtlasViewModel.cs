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
        private List<TreeViewItemData<(string, string)>> _atlasTreeData;

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
            _storeService.Store.Dispatch(SceneActions.ROTATE_AREA_VISIBILITY, areaID);

            Debug.Log($"(AVM) Toggled visibility for area ID {areaID}");
        }

        [ICommand]
        private void LoadAtlasData()
        {
            var rootId = BrainAtlasManager.ActiveReferenceAtlas.Ontology.Acronym2ID("root");

            AtlasTreeData = RecursiveParse(rootId);
            return;

            List<TreeViewItemData<(string, string)>> RecursiveParse(int nodeId)
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
                        select new TreeViewItemData<(string, string)>(
                            childId,
                            (childAcronym, childName),
                            childData
                        )
                    ).ToList();
            }
        }

        #endregion
    }
}
