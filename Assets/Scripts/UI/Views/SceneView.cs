using System.Collections.Generic;
using System.ComponentModel;
using NUnit.Framework;
using UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class SceneView
    {
        #region Component References

        private readonly ListView _manipulatorListView;

        #endregion

        private readonly SceneViewModel _sceneViewModel;

        public SceneView(TemplateContainer root, SceneViewModel sceneViewModel)
        {
            // Register view model and property changes.
            _sceneViewModel = sceneViewModel;
            _sceneViewModel.PropertyChanged += OnPropertyChanged;
            root.dataSource = sceneViewModel;

            // Register component references.
            _manipulatorListView = root.Q<ListView>("scene__manipulators-list");

            // Build manipulator list view.
            _manipulatorListView.bindItem = (element, i) =>
                _ = new ManipulatorListItem(
                    element,
                    _sceneViewModel.ManipulatorListItemViewModels[i]
                );
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_sceneViewModel.ManipulatorListItemViewModels):
                    Debug.Log($"Rebuilding list: {_sceneViewModel.ManipulatorListItemViewModels.Count}");
                    _manipulatorListView.itemsSource = _sceneViewModel.ManipulatorListItemViewModels;
                    _manipulatorListView.Rebuild();
                    break;
            }
        }
    }
}
