using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using LightJson;
using System.Linq;

namespace Unisave.Editor.JsonEditor
{
    public class JsonTreeView : TreeView
    {
        /// <summary>
        /// Active item that we work on
        /// </summary>
        public StringTreeViewItem activeItem { get; private set; }

        /// <summary>
        /// When the json data is changed by the user
        /// </summary>
        public event Action OnChange;

        // this object is used for root building
        // it's basically an "argument" for the BuildRoot method
        private JsonObject buildSubject;

        // Holds the old root item
        // This is because I represent the data by the tree view item structure
        // itself instead of a separate data model, to keep properties like
        // order persistent.
        private TreeViewItem oldRoot;

        /// <summary>
        /// Id allocation
        /// </summary>
        private int nextId = 0;

        /// <summary>
        /// Returns the next id for an item
        /// </summary>
        public int NextId() => nextId++;

        public JsonTreeView(TreeViewState treeViewState) : base(treeViewState)
        {
            showBorder = true;

            SetValue(new JsonObject());
        }

        /// <summary>
        /// Returns height of the tree view
        /// </summary>
        public float GetHeight() => totalHeight + rowHeight; // real height + some space

        /// <summary>
        /// Returns the value that's currently inside the editor
        /// </summary>
        public JsonObject GetValue()
        {
            return ((StringTreeViewItem)oldRoot).ToJson().AsJsonObject;
        }

        /// <summary>
        /// Sets the value displayed inside the tree view
        /// </summary>
        public void SetValue(JsonObject value)
        {
            SelectItem(null);
            nextId = 0;
            buildSubject = value;
            oldRoot = null;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = oldRoot;

            // first time creation (else just fresh up item depth)
            if (root == null)
            {
                // reset id allocation
                nextId = 1;

                // create root object
                root = new ObjectTreeViewItem(this, buildSubject) {
                    Index = 0,
                    id = 0,
                    depth = -1,
                    displayName = "Root"
                };
            }

            SetupDepthsFromParentsAndChildren(root);

            oldRoot = root;

            OnChange?.Invoke();

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is StringTreeViewItem)
            {
                Rect rr = args.rowRect;
                rr.x += GetContentIndent(args.item);
                ((StringTreeViewItem)args.item).ItemGUI(rr);
                return;
            }
        }

        /// <summary>
        /// Selects a single item
        /// </summary>
        public void SelectItem(TreeViewItem item)
        {
            List<int> selection = new List<int>();

            if (item != null)
                selection.Add(item.id);

            SetSelection(selection);

            // seems strange, but needed, it doesn't get refreshed for some reason
            // Reproduce error: Click "add +" and then immediately delete; wont work
            SelectionChanged(selection);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 0)
            {
                activeItem = null;
                return;
            }

            activeItem = FindItem(selectedIds[0], rootItem) as StringTreeViewItem;

            if (activeItem.JsonType == JsonType.NotJson)
                activeItem = null;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        /// <summary>
        /// Expand a given item
        /// </summary>
        public void ExpandItem(TreeViewItem item)
        {
            var expanded = new List<int>(GetExpanded());
            expanded.Add(item.id);
            SetExpanded(expanded);
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return ((StringTreeViewItem)args.draggedItem).JsonType != JsonType.NotJson;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            if (args.draggedItemIDs.Count == 0)
                return;

            StringTreeViewItem item = FindItem(args.draggedItemIDs[0], rootItem) as StringTreeViewItem;

            if (item == null || item.JsonType == JsonType.NotJson)
                return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(typeof(StringTreeViewItem).FullName, item);
            DragAndDrop.StartDrag(item.Label + item.StringValue);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            var data = DragAndDrop.GetGenericData(typeof(StringTreeViewItem).FullName) as StringTreeViewItem;

            if (data == null)
                return DragAndDropVisualMode.None;

            if (args.performDrop)
            {
                if (args.dragAndDropPosition == DragAndDropPosition.OutsideItems)
                {
                    data.RemoveField();
                    ((StringTreeViewItem)rootItem).DropItemAt(rootItem.children.Count - 1, data);
                    DragAndDrop.AcceptDrag();
                }
                else if (args.dragAndDropPosition == DragAndDropPosition.BetweenItems)
                {
                    var parent = args.parentItem as StringTreeViewItem;
                    var index = args.insertAtIndex;

                    // fix index when dragging within a single object
                    if (parent.children.Contains(data) && parent.children.IndexOf(data) < index)
                        index--;

                    data.RemoveField();
                    parent.DropItemAt(index, data);
                    DragAndDrop.AcceptDrag();
                }
                else if (args.dragAndDropPosition == DragAndDropPosition.UponItem)
                {
                    var parent = args.parentItem.parent as StringTreeViewItem;
                    var index = args.parentItem.parent.children.IndexOf(args.parentItem);

                    // fix index when dragging within a single object
                    if (parent.children.Contains(data) && parent.children.IndexOf(data) < index)
                        index--;

                    data.RemoveField();
                    parent.DropItemAt(index, data);
                    DragAndDrop.AcceptDrag();
                }
            }

            return DragAndDropVisualMode.Move;
        }

        protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            float indent = GetContentIndent(item);
            rowRect.x += indent;
            rowRect.width -= indent;

            if (item is StringTreeViewItem)
                return ((StringTreeViewItem)item).GetRenameRect(rowRect);

            return base.GetRenameRect(rowRect, row, item);
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as StringTreeViewItem;

            if (item != null)
                item.DoubleClickedItem();
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return item is StringTreeViewItem;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            var item = FindItem(args.itemID, rootItem) as StringTreeViewItem;

            if (item != null)
                item.RenameEnded(args.acceptedRename, args.newName);

            OnChange?.Invoke();
        }
    }
}
