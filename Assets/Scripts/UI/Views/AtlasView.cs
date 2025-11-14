using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Models;
using Models.Scene;
using Services;
using TrajectoryPlanner;
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

        private readonly Dictionary<int, AtlasTreeItemViewModel> _viewModelCache = new();

        private List<TreeViewItemData<(string, string, Color, AreaDisplayType)>> _fullTreeData;

        public AtlasView(AtlasViewModel atlasViewModel, StoreService storeService)
        {
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>("atlas-view");

            _atlasViewModel = atlasViewModel;
            _storeService = storeService;

            _atlasTree = root.Q<TreeView>("atlas-tree");
            _searchBar = root.Q<Unity.AppUI.UI.SearchBar>();

            _fullTreeData = atlasViewModel.AtlasTreeData;

            _atlasTree.SetRootItems(_fullTreeData);
            _atlasTree.bindItem = BindItem;
            _atlasTree.selectionType = SelectionType.Multiple;
            _atlasTree.Rebuild();

            // Disable WASD and arrow key navigation to prevent conflicts with probe 3D controls.
            DisableTreeViewKeyboardNavigation(_atlasTree);

            if (_searchBar != null)
            {
                _searchBar.RegisterValueChangedCallback(evt =>
                {
                    FilterTreeView(evt.newValue);
                });
            }

            _atlasViewModel.PropertyChanged += OnPropertyChanged;

            _sceneStateSubscription = storeService.Store.Subscribe(
                state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                OnSceneStateChanged,
                new SubscribeOptions<SceneState>()
            );

            _atlasTree.itemsChosen += OnItemsChosen;

            _atlasTree.selectedIndicesChanged += OnSelectedIndicesChanged;

            var trajectoryPlannerManager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
            if (trajectoryPlannerManager != null)
            {
                trajectoryPlannerManager.StartupEvent_Complete.AddListener(OnStartupComplete);
            }

            App.shuttingDown += OnShuttingDown;
        }

        private void OnStartupComplete()
        {
            ExpandToInitialNodes();
        }

        private void OnSceneStateChanged(SceneState state)
        {
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
                _atlasTree.SetRootItems(_fullTreeData ?? _atlasViewModel.AtlasTreeData);
                _atlasTree.Rebuild();
                _atlasTree.CollapseAll();
                return;
            }

            var searchLower = searchText.ToLowerInvariant();
            var filteredData = FilterTreeData(_fullTreeData ?? _atlasViewModel.AtlasTreeData, searchLower);

            _atlasTree.SetRootItems(filteredData ?? new List<TreeViewItemData<(string, string, Color, AreaDisplayType)>>());
            _atlasTree.Rebuild();

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
            var selectedIndices = _atlasTree.selectedIndices;

            foreach (var index in selectedIndices)
            {
                var id = _atlasTree.GetIdForIndex(index);
                _atlasViewModel.SelectAreaCommand.Execute(id);
            }
        }

        private void OnSelectedIndicesChanged(IEnumerable<int> selectedIndices)
        {
        }

        private void BindItem(VisualElement e, int i)
        {
            var id = _atlasTree.GetIdForIndex(i);
            var itemData = _atlasTree.GetItemDataForIndex<(string, string, Color, AreaDisplayType)>(i);

            if (!_viewModelCache.TryGetValue(id, out var viewModel))
            {
                viewModel = new AtlasTreeItemViewModel(itemData);
                _viewModelCache[id] = viewModel;
            }

            e.dataSource = viewModel;
        }

        private void ExpandToInitialNodes()
        {
            var state = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);

            if (state.BrainAreaVisibility != null && _fullTreeData != null)
            {
                var loadedNodeIds = state.BrainAreaVisibility
                    .Where(kvp => kvp.Value != AreaDisplayType.Hidden)
                    .Select(kvp => kvp.Key)
                    .ToList();
                if (loadedNodeIds.Count > 0)
                {
                    _atlasTree.Rebuild();
                    ExpandToNodes(loadedNodeIds);
                }
            }
        }

        private void ExpandToNodes(List<int> nodeIds)
        {
            var ancestorIds = new HashSet<int>();

            foreach (var nodeId in nodeIds)
            {
                CollectAncestors(_fullTreeData, nodeId, ancestorIds);
            }

            var ancestorList = ancestorIds.ToList();
            ancestorList.Sort((a, b) =>
            {
                var depthA = GetNodeDepth(_fullTreeData, a);
                var depthB = GetNodeDepth(_fullTreeData, b);
                return depthA.CompareTo(depthB);
            });

            foreach (var ancestorId in ancestorList)
            {
                var index = _atlasTree.viewController.GetIndexForId(ancestorId);
                if (index >= 0)
                {
                    _atlasTree.ExpandItem(ancestorId);
                }
            }
        }

        private int GetNodeDepth(List<TreeViewItemData<(string, string, Color, AreaDisplayType)>> items, int targetId, int currentDepth = 0)
        {
            if (items == null)
                return -1;

            foreach (var item in items)
            {
                if (item.id == targetId)
                    return currentDepth;

                if (item.children != null)
                {
                    var depth = GetNodeDepth(item.children.ToList(), targetId, currentDepth + 1);
                    if (depth >= 0)
                        return depth;
                }
            }

            return -1;
        }

        private bool CollectAncestors(
            List<TreeViewItemData<(string, string, Color, AreaDisplayType)>> items,
            int targetId,
            HashSet<int> ancestors)
        {
            if (items == null)
                return false;

            foreach (var item in items)
            {
                if (item.id == targetId)
                {
                    return true;
                }

                if (item.children != null)
                {
                    if (CollectAncestors(item.children.ToList(), targetId, ancestors))
                    {
                        ancestors.Add(item.id);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Disables WASD and arrow key navigation on a TreeView to prevent conflicts with probe 3D controls.
        /// Tab key is still allowed for accessibility.
        /// </summary>
        private void DisableTreeViewKeyboardNavigation(TreeView treeView)
        {
            treeView.RegisterCallback<KeyDownEvent>(evt =>
            {
                // Block WASD keys
                if (evt.keyCode == KeyCode.W || evt.keyCode == KeyCode.A || 
                    evt.keyCode == KeyCode.S || evt.keyCode == KeyCode.D ||
                    // Block arrow keys
                    evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.DownArrow ||
                    evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.RightArrow)
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                }
                // Allow Tab key to pass through for accessibility
            }, TrickleDown.TrickleDown);
        }
    }
}
