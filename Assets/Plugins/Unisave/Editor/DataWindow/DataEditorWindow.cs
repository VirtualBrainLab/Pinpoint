using System;
using Unisave.Facets;
using Unisave.Foundation;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unisave.Editor.DataWindow
{
    public class DataEditorWindow : EditorWindow
    {
        /// <summary>
        /// Event that triggers refreshing
        /// </summary>
        private static event Action OnRefresh;
        
        [SerializeField]
        private TreeViewState treeViewState;
        private DataWindowTreeView treeView;

        [MenuItem("Tools/Unisave/Data", false, 100)]
        [MenuItem("Window/Unisave/Data", false, 3)] // legacy
		public static void ShowWindow()
		{
			EditorWindow.GetWindow(
				typeof(DataEditorWindow),
				utility: false,
				title: "Data",
                focus: true
			);
		}

        void OnEnable()
        {
            OnRefresh += PerformRefresh;

            titleContent.image = AssetDatabase.LoadAssetAtPath<Texture>(
                "Assets/Plugins/Unisave/Images/WindowIcon.png"
            );
            
            if (treeViewState == null)
                treeViewState = new TreeViewState();

            treeView = new DataWindowTreeView(treeViewState);
        }

        private void OnDisable()
        {
            OnRefresh -= PerformRefresh;
        }

        /// <summary>
        /// Refreshes any open data editor windows
        /// Call this anytime the data changes to reflect those changes
        /// </summary>
        public static void Refresh()
        {
            OnRefresh?.Invoke();
        }

        /// <summary>
        /// Performs the refresh when requested by the event invocation
        /// </summary>
        private void PerformRefresh()
        {
            treeView.Reload();
        }

        void OnGUI()
        {
            /*
                - client entity cache
                    - MotorbikeEntity (yamaha)
                - emulated databases
                    - ...
                - database backups (database snapshots rather)
                    - my-cool-backup
                    - other backup
             */
            
            if (treeView == null)
                OnEnable();
            
            DrawToolbar();

            float h = EditorStyles.toolbar.fixedHeight;

            treeView.OnGUI(new Rect(0, h, position.width, position.height - h));
        }
        
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                Refresh();
            
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }
    }
}
