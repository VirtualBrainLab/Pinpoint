using System.ComponentModel;
using UI.ViewModels;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;

namespace UI.Views
{
    public class SceneView
    {
        #region Component References

        private readonly Button _addProbeButton;
        private readonly ListView _probeListView;
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
            _addProbeButton = root.Q<Button>("scene__add-probe-button");
            _probeListView = root.Q<ListView>("scene__probe-list-view");
            _manipulatorListView = root.Q<ListView>("scene__manipulators-list-view");

            // Add event listeners.
            _addProbeButton.clickable.clicked += _sceneViewModel.AddProbeCommand.Execute;

            // Build list views.
            _probeListView.itemsSource = _sceneViewModel.ProbeListItemViewModels;
            _probeListView.bindItem = (element, i) =>
                _ = new ProbeListItem(element, _sceneViewModel.ProbeListItemViewModels[i]);
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
                case nameof(_sceneViewModel.ProbeListItemViewModels):
                    _probeListView.itemsSource = _sceneViewModel.ProbeListItemViewModels;
                    _probeListView.Rebuild();
                    break;
                case nameof(_sceneViewModel.ManipulatorListItemViewModels):
                    _manipulatorListView.itemsSource =
                        _sceneViewModel.ManipulatorListItemViewModels;
                    _manipulatorListView.Rebuild();
                    break;
            }
        }
    }
}
