using System;
using System.Collections.Generic;
using Services;
using Unity.AppUI.MVVM;
using UnityEngine;
using UnityEngine.UIElements;
using BrainAtlas;
using Models.Scene;
using Models;
using Unity.AppUI.Redux;
using TrajectoryPlanner;

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

            Debug.Log($"(AVM) Detected atlas loading state: {state.AtlasLoaded}");

            if (state.AtlasLoaded && _atlasTreeData == null)
            {
                Debug.Log($"(AVM) Loading atlas {_atlasName}");
                LoadAtlasData();
            }
        }

        private void LoadAtlasData()
        {
            var rootID = BrainAtlasManager.ActiveReferenceAtlas.Ontology.Acronym2ID("root");

            _atlasTreeData = RecursiveParse(rootID);
            Debug.Log($"(AVM) Found {_atlasTreeData.Count} nodes");
        }

        private List<TreeViewItemData<(string, string)>> RecursiveParse(int rootID)
        {
            var childrenIDs = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Children(rootID);

            if (childrenIDs.Count == 0)
                return null;

            List<TreeViewItemData<(string, string)>> childrenData = new();

            foreach (var childID in childrenIDs)
            {
                var childData = RecursiveParse(childID);
                var childName = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Name(childID);
                var childAcronym = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Acronym(childID);
                childrenData.Add(new TreeViewItemData<(string, string)>(childID, (childAcronym, childName), childData));
            }

            return childrenData;
        }

        private void OnShuttingDown()
        {
            _sceneStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }

        [ICommand]
        private void SelectArea(int areaID)
        {
            _storeService.Store.Dispatch(SceneActions.ROTATE_AREA_VISIBILITY, areaID);

            

            Debug.Log($"(AVM) Toggled visibility for area ID {areaID}");
        }
    }

}