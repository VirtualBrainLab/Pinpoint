using System;
using System.Collections.Generic;
using System.Linq;
using BrainAtlas;
using Models;
using Models.Settings;
using Services;
using TrajectoryPlanner;
using UI.ViewModels;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.UIElements;
using UrchinUtilsUtils = Urchin.Utils.Utils;

namespace UI.Views
{
    public class AtlasSettingsView
    {
        #region Component References

        private readonly Unity.AppUI.UI.Dropdown _atlasDropdown;
        private readonly Unity.AppUI.UI.Dropdown _transformDropdown;
        private readonly Unity.AppUI.UI.Vector3Field _refCoordField;
        private readonly Unity.AppUI.UI.ActionButton _setBregmaButton;
        private readonly Unity.AppUI.UI.ActionButton _setLambdaButton;
        private readonly Unity.AppUI.UI.Toggle _show3DSlicesToggle;

        #endregion

        private readonly AtlasSettingsViewModel _atlasSettingsViewModel;
        private readonly IDisposableSubscription _atlasSettingsStateSubscription;
        private readonly StoreService _storeService;

        private bool _isUpdatingFromState = false;

        public AtlasSettingsView(AtlasSettingsViewModel atlasSettingsViewModel, StoreService storeService)
        {
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>("atlas-settings-view");
            _atlasSettingsViewModel = atlasSettingsViewModel;
            _storeService = storeService;

            _atlasDropdown = root.Q<Unity.AppUI.UI.Dropdown>("atlas-dropdown");
            _transformDropdown = root.Q<Unity.AppUI.UI.Dropdown>("transform-dropdown");
            _refCoordField = root.Q<Unity.AppUI.UI.Vector3Field>("reference-coordinate");
            _setBregmaButton = root.Q<Unity.AppUI.UI.ActionButton>("set-bregma-button");
            _setLambdaButton = root.Q<Unity.AppUI.UI.ActionButton>("set-lambda-button");
            _show3DSlicesToggle = root.Q<Unity.AppUI.UI.Toggle>("show-3d-slices-toggle");

            RegisterEventHandlers();

            var trajectoryPlannerManager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
            trajectoryPlannerManager.StartupEvent_RefAtlasLoaded.AddListener(OnStartupComplete);

            _atlasSettingsStateSubscription = _storeService.Store.Subscribe(
                   state => state.Get<AtlasSettingsState>(SliceNames.ATLAS_SETTINGS_SLICE),
              OnAtlasSettingsStateChanged,
                       new SubscribeOptions<AtlasSettingsState> { fireImmediately = true }
                   );

            App.shuttingDown += OnShuttingDown;
        }

        private void OnStartupComplete()
        {
            Debug.Log("(AtlasSettingsView) StartupEvent_RefAtlasLoaded received, populating dropdowns");
            PopulateDropdowns();
        }

        private void PopulateDropdowns()
        {
            var atlasNames = BrainAtlasManager.AtlasNames;

            _atlasDropdown.bindItem = (item, index) =>
                      {
                          item.label = atlasNames[index];
                      };
            _atlasDropdown.sourceItems = atlasNames;

            var atlasSettingsState = _storeService.Store.GetState<AtlasSettingsState>(SliceNames.ATLAS_SETTINGS_SLICE);

            var atlasIndex = atlasNames.FindIndex(x => x == atlasSettingsState.AtlasName);
            if (atlasIndex >= 0)
                _atlasDropdown.SetValueWithoutNotify(new[] { atlasIndex });

            var transformOptions = BrainAtlasManager.AtlasTransforms.Select(t => t.Name).ToList();

            _transformDropdown.bindItem = (item, index) =>
                       {
                           item.label = transformOptions[index];
                       };
            _transformDropdown.sourceItems = transformOptions;

            var transformIndex = transformOptions.FindIndex(x => x == atlasSettingsState.AtlasTransformName);
            if (transformIndex >= 0)
                _transformDropdown.SetValueWithoutNotify(new[] { transformIndex });
        }

        private void RegisterEventHandlers()
        {
            _atlasDropdown.RegisterValueChangedCallback(evt =>
            {
                if (_isUpdatingFromState) return;
                var selectedIndex = evt.newValue?.FirstOrDefault() ?? 0;
                if (selectedIndex >= 0 && selectedIndex < BrainAtlasManager.AtlasNames.Count)
                {
                    var selectedAtlas = BrainAtlasManager.AtlasNames[selectedIndex];
                    _storeService.Store.Dispatch(AtlasSettingsActions.SET_ATLAS_NAME, selectedAtlas);
                }
            });

            _transformDropdown.RegisterValueChangedCallback(evt =>
            {
                if (_isUpdatingFromState) return;
                var selectedIndex = evt.newValue?.FirstOrDefault() ?? 0;
                if (selectedIndex >= 0 && selectedIndex < BrainAtlasManager.AtlasTransforms.Count)
                {
                    var selectedTransform = BrainAtlasManager.AtlasTransforms[selectedIndex].Name;
                    _storeService.Store.Dispatch(AtlasSettingsActions.SET_ATLAS_TRANSFORM_NAME, selectedTransform);
                }
            });

            _refCoordField.RegisterValueChangedCallback(evt =>
            {
                if (_isUpdatingFromState) return;
                UpdateReferenceCoordinate();
            });

            _setBregmaButton.clicked += () =>
            {
                var activeAtlas = BrainAtlasManager.ActiveReferenceAtlas;
                if (activeAtlas != null && UrchinUtilsUtils.BregmaDefaults.ContainsKey(activeAtlas.Name))
                    _storeService.Store.Dispatch(AtlasSettingsActions.SET_REFERENCE_COORD, UrchinUtilsUtils.BregmaDefaults[activeAtlas.Name]);
                else
                    _storeService.Store.Dispatch(AtlasSettingsActions.SET_REFERENCE_COORD, Vector3.zero);
            };

            _setLambdaButton.clicked += () =>
            {
                var activeAtlas = BrainAtlasManager.ActiveReferenceAtlas;
                if (activeAtlas != null && UrchinUtilsUtils.LambdaDefaults.ContainsKey(activeAtlas.Name))
                    _storeService.Store.Dispatch(AtlasSettingsActions.SET_REFERENCE_COORD, UrchinUtilsUtils.LambdaDefaults[activeAtlas.Name]);
                else
                    _storeService.Store.Dispatch(AtlasSettingsActions.SET_REFERENCE_COORD, Vector3.zero);
            };

            _show3DSlicesToggle.RegisterValueChangedCallback(evt =>
            {
                if (_isUpdatingFromState) return;
                _storeService.Store.Dispatch(AtlasSettingsActions.TOGGLE_SHOW_3D_SLICES, evt.newValue);
            });
        }

        private void UpdateReferenceCoordinate()
        {
            try
            {
                var apmldv = _refCoordField.value;
                _storeService.Store.Dispatch(AtlasSettingsActions.SET_REFERENCE_COORD, apmldv);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Bad formatting in reference coordinate fields: {ex.Message}");
            }
        }

        private void OnAtlasSettingsStateChanged(AtlasSettingsState state)
        {
            _isUpdatingFromState = true;

            if (BrainAtlasManager.Instance == null || BrainAtlasManager.ActiveReferenceAtlas == null)
            {
                _isUpdatingFromState = false;
                return;
            }

            // Only update atlas dropdown if it has been populated
            if (_atlasDropdown?.sourceItems != null && BrainAtlasManager.AtlasNames?.Count > 0)
            {
                var atlasIndex = BrainAtlasManager.AtlasNames.FindIndex(x => x == state.AtlasName);
                if (atlasIndex >= 0)
                    _atlasDropdown.SetValueWithoutNotify(new[] { atlasIndex });
            }

            // Only update transform dropdown if it has been populated
            if (_transformDropdown?.sourceItems != null && BrainAtlasManager.AtlasTransforms?.Count > 0)
            {
                var transformIndex = BrainAtlasManager.AtlasTransforms.FindIndex(x => x.Name == state.AtlasTransformName);
                if (transformIndex >= 0)
                    _transformDropdown.SetValueWithoutNotify(new[] { transformIndex });
            }

            if (!float.IsNaN(state.ReferenceCoord.x))
            {
                _refCoordField.SetValueWithoutNotify(state.ReferenceCoord);
            }

            _show3DSlicesToggle.SetValueWithoutNotify(state.Show3DSlices);

            _isUpdatingFromState = false;
        }

        private void OnShuttingDown()
        {
            _atlasSettingsStateSubscription?.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }
    }
}
