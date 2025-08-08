using System.ComponentModel;
using UI.ViewModels;
using UnityEngine.UIElements;
using Utils.Types;
using Button = Unity.AppUI.UI.Button;

namespace UI.Views
{
    public class SceneView
    {
        #region Component References

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
            var addNeuropixels10 = root.Q<Button>("scene__add-probe-menu__neuropixels__1-0");
            var addNeuropixels20 = root.Q<Button>("scene__add-probe-menu__neuropixels__2-0");
            var addNeuropixels204Shank = root.Q<Button>("scene__add-probe-menu__neuropixels__2-0-4-shank");
            var addNeuropixels2X24 = root.Q<Button>("scene__add-probe-menu__neuropixels__2x-2-4");
            var addPipette25Um = root.Q<Button>("scene__add-probe-menu__pipette__25um");
            var addPipette50Um = root.Q<Button>("scene__add-probe-menu__pipette__50um");
            var addPipette100Um = root.Q<Button>("scene__add-probe-menu__pipette__100um");
            var addPipette200Um = root.Q<Button>("scene__add-probe-menu__pipette__200um");
            var addUcla128K = root.Q<Button>("scene__add-probe-menu__ucla__128k");
            var addUcla256F = root.Q<Button>("scene__add-probe-menu__ucla__256f");
            var addProbeButton = root.Q<Button>("scene__add-probe-button");
            _probeListView = root.Q<ListView>("scene__probe-list-view");
            _manipulatorListView = root.Q<ListView>("scene__manipulators-list-view");

            // Add event listeners.
            addProbeButton.clickable.clicked += () => _sceneViewModel.AddProbeCommand.Execute(ProbeType.Neuropixels1);
            addNeuropixels10.clickable.clicked += () => _sceneViewModel.AddProbeCommand.Execute(ProbeType.Neuropixels1);
            addNeuropixels20.clickable.clicked +=
                () => _sceneViewModel.AddProbeCommand.Execute(ProbeType.Neuropixels21);
            addNeuropixels204Shank.clickable.clicked +=
                () => _sceneViewModel.AddProbeCommand.Execute(ProbeType.Neuropixels24);
            addNeuropixels2X24.clickable.clicked +=
                () => _sceneViewModel.AddProbeCommand.Execute(ProbeType.Neuropixels24x2);
            addPipette25Um.clickable.clicked += () => _sceneViewModel.AddProbeCommand.Execute(ProbeType.Pipette25);
            addPipette50Um.clickable.clicked += () => _sceneViewModel.AddProbeCommand.Execute(ProbeType.Pipette50);
            addPipette100Um.clickable.clicked += () => _sceneViewModel.AddProbeCommand.Execute(ProbeType.Pipette100);
            addPipette200Um.clickable.clicked += () => _sceneViewModel.AddProbeCommand.Execute(ProbeType.Pipette200);
            addUcla128K.clickable.clicked += () => _sceneViewModel.AddProbeCommand.Execute(ProbeType.UCLA128K);
            addUcla256F.clickable.clicked += () => _sceneViewModel.AddProbeCommand.Execute(ProbeType.UCLA256F);

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