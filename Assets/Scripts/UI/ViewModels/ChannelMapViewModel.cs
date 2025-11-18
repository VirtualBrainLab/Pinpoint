using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using Models.Scene;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class ChannelMapViewModel
    {
        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;

        [ObservableProperty]
        private List<ChannelMapTextItemViewModel> _textItems = new();

        public ChannelMapViewModel(StoreService storeService)
        {
            _storeService = storeService;

            _sceneStateSubscription = _storeService.Store.Subscribe(
                     state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
               OnSceneStateChanged,
               new SubscribeOptions<SceneState> { fireImmediately = true }
          );

            App.shuttingDown += OnShuttingDown;
        }

        private void OnSceneStateChanged(SceneState state)
        {
            if (string.IsNullOrEmpty(state.ActiveProbeName))
            {
                TextItems = new List<ChannelMapTextItemViewModel>();
                return;
            }

            var activeProbe = state.Probes.FirstOrDefault(p => p.Name == state.ActiveProbeName);
            if (activeProbe == null)
            {
                TextItems = new List<ChannelMapTextItemViewModel>();
                return;
            }

            var probeManager = ProbeManager.Instances.FirstOrDefault(pm => pm.name == state.ActiveProbeName);
            if (probeManager == null)
            {
                TextItems = new List<ChannelMapTextItemViewModel>();
                return;
            }

            var probePanels = probeManager.GetProbeUIManagers();
            if (probePanels == null || probePanels.Count == 0)
            {
                TextItems = new List<ChannelMapTextItemViewModel>();
                return;
            }

            var probePanel = probePanels[0].GetProbePanel();
            if (probePanel == null)
            {
                TextItems = new List<ChannelMapTextItemViewModel>();
                return;
            }
        }

        public void UpdateTextItems(List<string> names, List<float> positionPercentages)
        {
            var newItems = new List<ChannelMapTextItemViewModel>();

            for (int i = 0; i < names.Count && i < positionPercentages.Count; i++)
            {
                var activeProbeState = _storeService.Store
             .GetState<SceneState>(SliceNames.SCENE_SLICE)
                .Probes.FirstOrDefault(p => p.Name ==
               _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE).ActiveProbeName);

                if (activeProbeState != null)
                {
                    newItems.Add(new ChannelMapTextItemViewModel(
                   activeProbeState,
                _storeService
                       )
                    {
                        Name = names[i],
                        PositionPerc = positionPercentages[i]
                    });
                }
            }

            TextItems = newItems;
        }

        private void OnShuttingDown()
        {
            _sceneStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }
    }
}