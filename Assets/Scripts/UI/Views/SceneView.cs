using System.ComponentModel;
using System.Linq;
using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Utils.Types;

namespace UI.Views
{
    public class SceneView
    {
        #region Component References

        private readonly ListView _probeListView;
        private readonly ListView _manipulatorListView;

        #endregion

        private readonly SceneViewModel _sceneViewModel;

        public SceneView(SceneViewModel sceneViewModel)
        {
            // Get root visual element.
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>("scene-view");

            // Register view model and property changes.
            _sceneViewModel = sceneViewModel;
            _sceneViewModel.PropertyChanged += OnPropertyChanged;
            root.dataSource = sceneViewModel;

            // Register component references.
            var addNeuropixels10 = root.Q<MenuItem>("scene__add-probe-menu__neuropixels__1-0");
            var addNeuropixels20 = root.Q<MenuItem>("scene__add-probe-menu__neuropixels__2-0");
            var addNeuropixels204Shank = root.Q<MenuItem>(
                "scene__add-probe-menu__neuropixels__2-0-4-shank"
            );
            var addNeuropixels2X24 = root.Q<MenuItem>("scene__add-probe-menu__neuropixels__2x-2-4");
            var addPipette25Um = root.Q<MenuItem>("scene__add-probe-menu__pipette__25um");
            var addPipette50Um = root.Q<MenuItem>("scene__add-probe-menu__pipette__50um");
            var addPipette100Um = root.Q<MenuItem>("scene__add-probe-menu__pipette__100um");
            var addPipette200Um = root.Q<MenuItem>("scene__add-probe-menu__pipette__200um");
            var addUcla128K = root.Q<MenuItem>("scene__add-probe-menu__ucla__128k");
            var addUcla256F = root.Q<MenuItem>("scene__add-probe-menu__ucla__256f");

            _probeListView = root.Q<ListView>("scene__probe-list-view");
            _manipulatorListView = root.Q<ListView>("scene__manipulators-list");

            // Add event listeners.
            addNeuropixels10.clickable.clicked += () =>
                _sceneViewModel.AddProbeCommand.Execute(ProbeType.Neuropixels1);
            addNeuropixels20.clickable.clicked += () =>
                _sceneViewModel.AddProbeCommand.Execute(ProbeType.Neuropixels21);
            addNeuropixels204Shank.clickable.clicked += () =>
                _sceneViewModel.AddProbeCommand.Execute(ProbeType.Neuropixels24);
            addNeuropixels2X24.clickable.clicked += () =>
                _sceneViewModel.AddProbeCommand.Execute(ProbeType.Neuropixels24x2);
            addPipette25Um.clickable.clicked += () =>
                _sceneViewModel.AddProbeCommand.Execute(ProbeType.Pipette25);
            addPipette50Um.clickable.clicked += () =>
                _sceneViewModel.AddProbeCommand.Execute(ProbeType.Pipette50);
            addPipette100Um.clickable.clicked += () =>
                _sceneViewModel.AddProbeCommand.Execute(ProbeType.Pipette100);
            addPipette200Um.clickable.clicked += () =>
                _sceneViewModel.AddProbeCommand.Execute(ProbeType.Pipette200);
            addUcla128K.clickable.clicked += () =>
                _sceneViewModel.AddProbeCommand.Execute(ProbeType.UCLA128K);
            addUcla256F.clickable.clicked += () =>
                _sceneViewModel.AddProbeCommand.Execute(ProbeType.UCLA256F);
            _probeListView.selectedIndicesChanged += indices =>
            {
                var indicesList = indices.ToList();
                sceneViewModel.SetActiveProbeCommand.Execute(
                    indicesList.Any() ? indicesList[0] : -1
                );

                // Clear the manipulator selection when a probe is selected.
                _manipulatorListView.selectedIndex = -1;
            };
            _manipulatorListView.selectedIndicesChanged += indices =>
            {
                var indicesList = indices.ToList();
                sceneViewModel.SetActiveManipulatorCommand.Execute(
                    indicesList.Any() ? indicesList[0] : -1
                );

                // Clear the probe selection when a manipulator is selected.
                _probeListView.selectedIndex = -1;
            };

            // Build probe list view.
            _probeListView.itemsSource = _sceneViewModel.ProbeListItemViewModels;
            _probeListView.bindItem = (element, i) =>
                _ = new ProbeListItem(element, _sceneViewModel.ProbeListItemViewModels[i]);

            // Build manipulator list view.
            _manipulatorListView.itemsSource = _sceneViewModel.ManipulatorIds;
            _manipulatorListView.makeItem = () => new Heading { size = HeadingSize.S };
            _manipulatorListView.bindItem = (element, i) =>
                ((Heading)element).text = sceneViewModel.ManipulatorIds[i];
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_sceneViewModel.SelectedProbeIndex):
                    _probeListView.selectedIndex = _sceneViewModel.SelectedProbeIndex;
                    break;
                case nameof(_sceneViewModel.ProbeListItemViewModels):
                    _probeListView.itemsSource = _sceneViewModel.ProbeListItemViewModels;
                    _probeListView.Rebuild();
                    break;
                case nameof(_sceneViewModel.ManipulatorIds):
                    _manipulatorListView.itemsSource = _sceneViewModel.ManipulatorIds;
                    _manipulatorListView.Rebuild();
                    break;
            }
        }
    }
}
