using System.Collections.Generic;
using Unisave.Editor.DataWindow.TreeItems;
using Unisave.Facades;
using Unisave.Foundation;
using Unisave.Sessions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Unisave.Editor.DataWindow
{
    class DataWindowTreeView : TreeView
    {
        public static TreeViewItem SelectedItem { get; private set; }

        public DataWindowTreeView(TreeViewState treeViewState) : base(treeViewState)
        {
            Reload();
        }
        
        protected override TreeViewItem BuildRoot()
        {
            var idAllocator = new IdAllocator();

            var root = new TreeViewItem {
                id = 0,
                depth = -1,
                displayName = "Root"
            };

            root.AddChild(new SessionIdItem(
                ClientFacade.ClientApp.Resolve<ClientSessionIdRepository>(),
                idAllocator
            ));

            SetupDepthsFromParentsAndChildren(root);

            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 0 || selectedIds.Count > 1)
            {
                SelectedItem = null;
                return;
            }

            SelectedItem = FindItem(selectedIds[0], rootItem);
            
            switch (SelectedItem)
            {
                // ...
                
                default:
                    Selection.SetActiveObjectWithContext(null, null);
                    break;
            }
        }
    }
}
