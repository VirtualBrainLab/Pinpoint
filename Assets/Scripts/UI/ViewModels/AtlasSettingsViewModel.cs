using System;
using System.ComponentModel;
using BrainAtlas;
using Models;
using Models.Settings;
using Services;
using UI;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace UI.ViewModels
{
    public class AtlasSettingsViewModel
    {
        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _atlasSettingsStateSubscription;
        private PinpointAtlasManager _pinpointAtlasManager;

        public AtlasSettingsViewModel(StoreService storeService)
        {
            _storeService = storeService;

            _atlasSettingsStateSubscription = storeService.Store.Subscribe(
    state => state.Get<AtlasSettingsState>(SliceNames.ATLAS_SETTINGS_SLICE),
                OnAtlasSettingsStateChanged,
    new SubscribeOptions<AtlasSettingsState> { fireImmediately = true }
      );

            App.shuttingDown += OnShuttingDown;
        }

        private void OnAtlasSettingsStateChanged(AtlasSettingsState state)
        {
            if (_pinpointAtlasManager == null)
                _pinpointAtlasManager = GameObject.FindFirstObjectByType<PinpointAtlasManager>();

            if (_pinpointAtlasManager == null)
                return;

            ApplyAtlasTransform(state.AtlasTransformName);
            ApplyShow3DSlices(state.Show3DSlices);
        }

        private void ApplyAtlasTransform(string transformName)
        {
            if (BrainAtlasManager.AtlasTransforms == null)
                return;
            var newTransform = BrainAtlasManager.AtlasTransforms.Find(x => x.Name.Equals(transformName));

            if (newTransform != null)
            {
                _pinpointAtlasManager.SetNewTransform(newTransform);
            }
            else
            {
                _pinpointAtlasManager.SetNewTransform(BrainAtlasManager.AtlasTransforms[0]);
                Debug.LogWarning($"(AtlasSettingsViewModel) No matching atlas transform exists for {transformName}, reverting to NULL transform.");
            }
        }

        private void ApplyShow3DSlices(bool show)
        {
            var sliceRenderer = GameObject.FindFirstObjectByType<TP_SliceRenderer>();
            if (sliceRenderer != null)
                sliceRenderer.ToggleSliceVisibility(show ? 1 : 0);
        }

        private void OnShuttingDown()
        {
            _atlasSettingsStateSubscription?.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }
    }
}
