using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Models;
using Models.Scene;
using Services;
using UI.ViewModels;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.UIElements;
using Utils.Types;

namespace UI.Views
{
    public class AtlasView
    {
        private readonly TreeView _atlasTree;
        private readonly AtlasViewModel _atlasViewModel;
        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;
        private readonly Unity.AppUI.UI.SearchBar _searchBar;

        // Cache view models by area ID
        private readonly Dictionary<int, AtlasTreeItemViewModel> _viewModelCache = new();

        // Store the full unfiltered tree data
        private List<TreeViewItemData<(string, string, Color, AreaDisplayType)>> _fullTreeData;

        public AtlasView(AtlasViewModel atlasViewModel, StoreService storeService)
        {
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>("atlas-view");

            _atlasViewModel = atlasViewModel;
            _storeService = storeService;

            _atlasTree = root.Q<TreeView>("atlas-tree");
            _searchBar = root.Q<Unity.AppUI.UI.SearchBar>();

            // Initialize full tree data before setting root items
            _fullTreeData = atlasViewModel.AtlasTreeData;

            _atlasTree.SetRootItems(_fullTreeData);
            _atlasTree.bindItem = BindItem;
            _atlasTree.selectionType = SelectionType.Multiple;
            _atlasTree.Rebuild();

            // Bind search bar to trigger filtering as user types
            if (_searchBar != null)
            {
                _searchBar.RegisterValueChangedCallback(evt =>
                {
                    FilterTreeView(evt.newValue);
                });
            }

            _atlasViewModel.PropertyChanged += OnPropertyChanged;

            // Subscribe to scene state changes to update view models
            _sceneStateSubscription = storeService.Store.Subscribe(
                state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                OnSceneStateChanged,
                new SubscribeOptions<SceneState>()
            );

            // Callback invoked when the user double-clicks an item
            _atlasTree.itemsChosen += OnItemsChosen;

            // Callback invoked when the user changes the selection inside the TreeView
            _atlasTree.selectedIndicesChanged += OnSelectedIndicesChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnSceneStateChanged(SceneState state)
        {
            // Update cached view models when visibility changes
            if (state.BrainAreaVisibility != null)
            {
                foreach (var kvp in state.BrainAreaVisibility)
                {
                    if (_viewModelCache.TryGetValue(kvp.Key, out var viewModel))
                    {
                        viewModel.UpdateDisplayType(kvp.Value);
                    }
                }
            }
        }

        private void FilterTreeView(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                // Show full tree when search is empty
                _atlasTree.SetRootItems(_fullTreeData ?? _atlasViewModel.AtlasTreeData);
                _atlasTree.Rebuild();
                // Collapse all items when search is cleared
                _atlasTree.CollapseAll();
                return;
            }

            var searchLower = searchText.ToLowerInvariant();
            var filteredData = FilterTreeData(_fullTreeData ?? _atlasViewModel.AtlasTreeData, searchLower);

            _atlasTree.SetRootItems(filteredData ?? new List<TreeViewItemData<(string, string, Color, AreaDisplayType)>>());
            _atlasTree.Rebuild();
            
            // Expand all items to show search results
            _atlasTree.ExpandAll();
        }

        private List<TreeViewItemData<(string, string, Color, AreaDisplayType)>> FilterTreeData(
            List<TreeViewItemData<(string, string, Color, AreaDisplayType)>> items,
            string searchLower)
        {
            if (items == null)
                return null;

            var filtered = new List<TreeViewItemData<(string, string, Color, AreaDisplayType)>>();

            foreach (var item in items)
            {
                var (acronym, name, color, displayType) = item.data;
                var matchesCurrent = acronym.ToLowerInvariant().Contains(searchLower) ||
                                     name.ToLowerInvariant().Contains(searchLower);

                var filteredChildren = FilterTreeData(item.children?.ToList(), searchLower);
                var hasMatchingChildren = filteredChildren != null && filteredChildren.Count > 0;

                // Include this item if it matches or has matching descendants
                if (matchesCurrent || hasMatchingChildren)
                {
                    filtered.Add(new TreeViewItemData<(string, string, Color, AreaDisplayType)>(
                        item.id,
                        item.data,
                        filteredChildren
                    ));
                }
            }

            return filtered.Count > 0 ? filtered : null;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_atlasViewModel.AtlasTreeData):
                    _viewModelCache.Clear();
                    _fullTreeData = _atlasViewModel.AtlasTreeData;
                    _atlasTree.SetRootItems(_atlasViewModel.AtlasTreeData);
                    _atlasTree.Rebuild();
                    break;
            }
        }

        private void OnShuttingDown()
        {
            _atlasViewModel.PropertyChanged -= OnPropertyChanged;
            _sceneStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }

        private void OnItemsChosen(IEnumerable<object> selectedItems)
        {
            Debug.Log("Items chosen: " + string.Join(", ", selectedItems));

            // Get the selected indices to correlate with IDs and data
            var selectedIndices = _atlasTree.selectedIndices;

            foreach (var index in selectedIndices)
            {
                var id = _atlasTree.GetIdForIndex(index);
                var data = _atlasTree.GetItemDataForIndex<(string, string, Color, AreaDisplayType)>(
                    index
                );

                Debug.Log($"Chosen item - ID: {id}, Data: ({data.Item1}, {data.Item2})");

                // Use the ID for your command execution
                _atlasViewModel.SelectAreaCommand.Execute(id);
            }
        }

        private void OnSelectedIndicesChanged(IEnumerable<int> selectedIndices)
        {
            var log = "IDs selected: ";
            foreach (var index in selectedIndices)
            {
                log += $"{_atlasTree.GetIdForIndex(index)}, ";
            }
            Debug.Log(log.TrimEnd(',', ' '));
        }

        private void BindItem(VisualElement e, int i)
        {
            var id = _atlasTree.GetIdForIndex(i);
            var itemData = _atlasTree.GetItemDataForIndex<(string, string, Color, AreaDisplayType)>(i);

            // Check if we already have a view model for this area ID
            if (!_viewModelCache.TryGetValue(id, out var viewModel))
            {
                // Create new view model and cache it
                viewModel = new AtlasTreeItemViewModel(itemData);
                _viewModelCache[id] = viewModel;
            }

            e.dataSource = viewModel;
        }
    }
}
