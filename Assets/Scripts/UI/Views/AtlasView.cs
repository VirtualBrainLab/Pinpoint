using System.ComponentModel;
using System.Collections.Generic;
using UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;
using Utils.Types;

namespace UI.Views
{
    public class AtlasView
    {
        private readonly VisualElement _root;

        private readonly TreeView _atlasTree;

        private readonly AtlasViewModel _atlasViewModel;

        public AtlasView(VisualElement root, AtlasViewModel atlasViewModel)
        {
            _root = root;

            _atlasViewModel = atlasViewModel;

            _atlasTree = _root.Q<TreeView>("atlas-tree");

            _atlasTree.SetRootItems(atlasViewModel.AtlasTreeData);
            _atlasTree.bindItem = bindItem;
            _atlasTree.selectionType = SelectionType.Multiple;
            _atlasTree.Rebuild();

            _atlasViewModel.PropertyChanged += OnPropertyChanged;

            // Callback invoked when the user double clicks an item
            _atlasTree.itemsChosen += OnItemsChosen;

            // Callback invoked when the user changes the selection inside the TreeView
            _atlasTree.selectedIndicesChanged += OnSelectedIndicesChanged;
        }

        public void Dispose()
        {
            _atlasViewModel.PropertyChanged -= OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_atlasViewModel.AtlasTreeData):
                    _atlasTree.SetRootItems(_atlasViewModel.AtlasTreeData);
                    _atlasTree.Rebuild();
                    break;
            }
        }

        private void OnItemsChosen(IEnumerable<object> selectedItems)
        {
            Debug.Log("Items chosen: " + string.Join(", ", selectedItems));
            
            // Get the selected indices to correlate with IDs and data
            var selectedIndices = _atlasTree.selectedIndices;
            
            foreach (var index in selectedIndices)
            {
                var id = _atlasTree.GetIdForIndex(index);
                var data = _atlasTree.GetItemDataForIndex<(string, string, Color, AreaDisplayType)>(index);
                
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

        private void bindItem(VisualElement e, int i)
        {
            var itemData = _atlasTree.GetItemDataForIndex<(string, string, Color, AreaDisplayType)>(i);
            e.dataSource = new AtlasTreeItemViewModel(itemData);
        }
    }
}
