using System;
using System.Collections.Generic;
using Unisave.Editor.Windows.Main.Tabs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unisave.Editor.Windows.Main
{
    public class UnisaveMainWindow : EditorWindow
    {
        public const string WindowTitle = "Unisave";

        private const string OpenedTabPrefsKey =
            "unisave.openedUnisaveWindowTab";

        /// <summary>
        /// Controls the tab opening and tab heads
        /// </summary>
        private TabsController tabsController;
        
        /// <summary>
        /// Tab content controllers for each tab
        /// </summary>
        private Dictionary<MainWindowTab, ITabContentController> tabContents;

        /// <summary>
        /// Where can the window be opened in the Unity menus
        /// </summary>
        public const string UnityMenuPath = "Tools/Unisave/Unisave Window";
        private const string UnityLegacyMenuPath = "Window/Unisave/Unisave Window";
        
        [MenuItem(UnityMenuPath, false, 1)]
        [MenuItem(UnityLegacyMenuPath, false, 1)]
        public static void ShowWindow()
        {
            ShowTab(MainWindowTab.Home);
        }

        /// <summary>
        /// Call this to open/focus the window on a specific tab when something
        /// important happens and the user needs to know about it
        /// </summary>
        /// <param name="tab">Which tab should the window show</param>
        public static UnisaveMainWindow ShowTab(MainWindowTab tab)
        {
            var window = EditorWindow.GetWindow<UnisaveMainWindow>(
                utility: false,
                title: WindowTitle,
                focus: true
            );
            window.Show();
            window.OpenTab(tab);
            return window;
        }

        private void OpenTab(MainWindowTab tab)
        {
            // save the old tab content
            OnWriteExternalState();
            
            // open the new tab
            tabsController?.RenderOpenedTab(tab); // skipped if called before CreateGUI
            
            // store the opened tab
            EditorPrefs.SetInt(OpenedTabPrefsKey, (int) tab);
            
            // let the new tab refresh its content
            OnObserveExternalState();
        }
        
        private void CreateGUI()
        {
            titleContent.image = AssetDatabase.LoadAssetAtPath<Texture>(
                EditorGUIUtility.isProSkin ?
                    "Assets/Plugins/Unisave/Images/WindowIconWhite.png" :
                    "Assets/Plugins/Unisave/Images/WindowIcon.png"
            );
            
            // set up UI tree
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Plugins/Unisave/Editor/Windows/Main/UI/UnisaveMainWindow.uxml"
            );
            rootVisualElement.Add(visualTree.Instantiate());
            
            // register mouse leave event
            rootVisualElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            // set up the tab head controller
            tabsController = new TabsController(rootVisualElement, OpenTab);
            tabsController.RegisterCallbacks();
            
            // create individual tab controllers
            tabContents = new Dictionary<MainWindowTab, ITabContentController>();

            tabContents[MainWindowTab.Home] = new HomeTabController(
                rootVisualElement.Q(name: "tab-content__Home")
            );
            tabContents[MainWindowTab.Connection] = new ConnectionTabController(
                rootVisualElement.Q(name: "tab-content__Connection")
            );
            tabContents[MainWindowTab.Backend] = new BackendTabController(
                rootVisualElement.Q(name: "tab-content__Backend")
            );

            foreach (var pair in tabContents)
            {
                pair.Value.SetTaint = (taint) => {
                    tabsController.RenderTabTaint(pair.Key, taint);
                };
                pair.Value.OnCreateGUI();
            }
            
            // restore the last opened tab
            MainWindowTab lastOpenedTab = (MainWindowTab) EditorPrefs.GetInt(
                OpenedTabPrefsKey, (int) MainWindowTab.Home
            );
            OpenTab(lastOpenedTab);
        }

        private void OnFocus() => OnObserveExternalState();
        private void OnLostFocus() => OnWriteExternalState();
        private void OnMouseLeave(MouseLeaveEvent e) => OnWriteExternalState();
        
        /// <summary>
        /// Called when the content of the window should update to correspond
        /// with the reality outside the window and inside the unity editor
        /// and the filesystem. (When the window should refresh what it displays)
        /// </summary>
        private void OnObserveExternalState()
        {
            if (tabContents == null || tabsController == null)
                return;
            
            // refresh all tabs, since they might update their header
            foreach (ITabContentController tab in tabContents.Values)
                tab.OnObserveExternalState();
        }

        /// <summary>
        /// Called when the modified content of the window should be written
        /// to the surrounding editor and filesystem. (When the window should
        /// save any modifications)
        /// </summary>
        private void OnWriteExternalState()
        {
            if (tabContents == null || tabsController == null)
                return;
            
            // write only the opened tab
            if (tabContents.ContainsKey(tabsController.CurrentTab))
                tabContents[tabsController.CurrentTab].OnWriteExternalState();
        }
    }
}