using UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;
using TreeView = UnityEngine.UIElements.TreeView;

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
            _atlasTree.makeItem = makeItem;
            _atlasTree.bindItem = bindItem;
            _atlasTree.selectionType = SelectionType.Multiple;
            _atlasTree.Rebuild();

            // Callback invoked when the user double clicks an item
            _atlasTree.itemsChosen += (selectedItems) =>
            {
                Debug.Log("Items chosen: " + string.Join(", ", selectedItems));
            };

            // Callback invoked when the user changes the selection inside the TreeView
            _atlasTree.selectedIndicesChanged += (selectedIndices) =>
            {
                var log = "IDs selected: ";
                foreach (var index in selectedIndices)
                {
                    log += $"{_atlasTree.GetIdForIndex(index)}, ";
                }
                Debug.Log(log.TrimEnd(',', ' '));
            };
        }

        private VisualElement makeItem()
        {
            return new Label();
        }

        private void bindItem(VisualElement e, int i)
        {
            var item = _atlasTree.GetItemDataForIndex<string>(i);
            var id = _atlasTree.GetIdForIndex(i);
            ((Label)e).text = $"ID {id} - {item}";
        }
    }
}
