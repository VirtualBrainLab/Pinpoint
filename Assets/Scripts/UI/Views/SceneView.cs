using System.Collections.Generic;
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

        public SceneView(TemplateContainer root, SceneViewModel sceneViewModel)
        {
            // Register component references.
            _manipulatorListView = root.Q<ListView>("scene__manipulators-list");

            _manipulatorListView.itemsSource = sceneViewModel.ManipulatorListItemViewModels;
            _manipulatorListView.bindItem = (element, i) =>
                _ = new ManipulatorListItem(
                    element,
                    sceneViewModel.ManipulatorListItemViewModels[i]
                );
        }
    }
}
