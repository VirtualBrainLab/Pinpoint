using System;
using System.ComponentModel;
using BrainAtlas;
using Models;
using Models.Settings;
using Services;
using TrajectoryPlanner;
using UI;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.ViewModels
{
    public class AtlasSettingsViewModel
    {
        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _atlasSettingsStateSubscription;
        private PinpointAtlasManager _pinpointAtlasManager;
        private TrajectoryPlannerManager _trajectoryPlannerManager;
        private string _previousAtlasName;
        private Vector3 _previousReferenceCoord = Vector3.negativeInfinity;

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

            if (_trajectoryPlannerManager == null)
                _trajectoryPlannerManager = GameObject.FindFirstObjectByType<TrajectoryPlannerManager>();

            if (_pinpointAtlasManager == null)
                return;

            if (string.IsNullOrEmpty(_previousAtlasName))
            {
                _previousAtlasName = state.AtlasName;
            }
            else if (!string.IsNullOrEmpty(state.AtlasName) && _previousAtlasName != state.AtlasName)
            {
                Debug.Log($"(AtlasSettingsViewModel) Atlas name changed from {_previousAtlasName} to {state.AtlasName}, reloading scene");
                PlayerPrefs.SetInt("scene-atlas-reset", 1);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            ApplyAtlasTransform(state.AtlasTransformName);
            ApplyReferenceCoord(state.ReferenceCoord);
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

            if (_trajectoryPlannerManager != null)
            {
                _trajectoryPlannerManager.UpdateAllProbePositions();
                _trajectoryPlannerManager.UpdateReferenceCoord();
            }
        }

        private void ApplyReferenceCoord(Vector3 referenceCoord)
        {
            if (float.IsNaN(referenceCoord.x))
                return;

            if (_previousReferenceCoord == referenceCoord)
                return;

            _previousReferenceCoord = referenceCoord;

            if (BrainAtlasManager.Instance == null || BrainAtlasManager.ActiveReferenceAtlas == null)
                return;

            BrainAtlasManager.ActiveReferenceAtlas.AtlasSpace.ReferenceCoord = referenceCoord;

            if (_trajectoryPlannerManager != null)
            {
                _trajectoryPlannerManager.UpdateAllProbePositions();
                _trajectoryPlannerManager.UpdateReferenceCoord();
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
