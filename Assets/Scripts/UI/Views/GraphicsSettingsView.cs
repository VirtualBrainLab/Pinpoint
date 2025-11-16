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


namespace UI.Views
{
    public class GraphicsSettingsView
    {
        #region Component References

        private readonly Unity.AppUI.UI.Toggle _whiteBackgroundToggle;
        private readonly Unity.AppUI.UI.Toggle _surfaceCoordToggle;
        private readonly Unity.AppUI.UI.Toggle _refCoordToggle;
        private readonly Unity.AppUI.UI.Toggle _inactiveTransparentToggle;

        #endregion

        #region Event Handlers

        private EventCallback<ChangeEvent<bool>> _whiteBackgroundToggleChangedHandler;
        private EventCallback<ChangeEvent<bool>> _surfaceCoordToggleChangedHandler;
        private EventCallback<ChangeEvent<bool>> _refCoordToggleChangedHandler;
        private EventCallback<ChangeEvent<bool>> _inactiveTransparentToggleChangedHandler;

        #endregion

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _settingsStateSubscription;
        private readonly TrajectoryPlannerManager _trajectoryPlannerManager;
        private readonly UIManager _uiManager;

        private bool _isUpdatingFromState = false;

        public GraphicsSettingsView(StoreService storeService)
        {
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>("graphics-settings-view");
            _storeService = storeService;

            _whiteBackgroundToggle = root.Q<Unity.AppUI.UI.Toggle>("toggle-white-background");
            _surfaceCoordToggle = root.Q<Unity.AppUI.UI.Toggle>("toggle-surface-coord");
            _refCoordToggle = root.Q<Unity.AppUI.UI.Toggle>("toggle-ref-coord");
            _inactiveTransparentToggle = root.Q<Unity.AppUI.UI.Toggle>("toggle-inactive-transparent");

            // Get references to managers
            _trajectoryPlannerManager = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>();
            _uiManager = UIManager.Instance;

            RegisterEventHandlers();

            _settingsStateSubscription = _storeService.Store.Subscribe(
            state => state.Get<SettingsState>(SliceNames.SETTINGS_SLICE),
               OnSettingsStateChanged,
                        new SubscribeOptions<SettingsState> { fireImmediately = true }
              );

            App.shuttingDown += OnShuttingDown;
        }

        private void RegisterEventHandlers()
        {
            _whiteBackgroundToggleChangedHandler = evt =>
            {
                if (_isUpdatingFromState) return;
                var newColor = evt.newValue ? Color.white : Color.black;
                _storeService.Store.Dispatch(SettingsActions.SET_BACKGROUND, newColor);

                // Apply background color change immediately
                if (_uiManager != null)
                    _uiManager.SetBackgroundWhite(evt.newValue);
            };
            _whiteBackgroundToggle.RegisterValueChangedCallback(_whiteBackgroundToggleChangedHandler);

            _surfaceCoordToggleChangedHandler = evt =>
                {
                    if (_isUpdatingFromState) return;
                    _storeService.Store.Dispatch(SettingsActions.SET_SHOW_SURFACE_COORDINATE, evt.newValue);
                };
            _surfaceCoordToggle.RegisterValueChangedCallback(_surfaceCoordToggleChangedHandler);

            _refCoordToggleChangedHandler = evt =>
                      {
                          if (_isUpdatingFromState) return;
                          _storeService.Store.Dispatch(SettingsActions.SET_SHOW_BREGMA_AXIS, evt.newValue);

                          // Update reference coordinate visibility immediately
                          if (_trajectoryPlannerManager != null)
                              _trajectoryPlannerManager.SetReferenceCoordActive(evt.newValue);
                      };
            _refCoordToggle.RegisterValueChangedCallback(_refCoordToggleChangedHandler);

            _inactiveTransparentToggleChangedHandler = evt =>
                  {
                      if (_isUpdatingFromState) return;
                      _storeService.Store.Dispatch(SettingsActions.SET_GHOST_INACTIVE_PROBES, evt.newValue);

                      // Update probe ghosting immediately
                      if (_trajectoryPlannerManager != null)
                          _trajectoryPlannerManager.SetGhostProbeVisibility();
                  };
            _inactiveTransparentToggle.RegisterValueChangedCallback(_inactiveTransparentToggleChangedHandler);
        }

        private void UnregisterEventHandlers()
        {
            _whiteBackgroundToggle?.UnregisterValueChangedCallback(_whiteBackgroundToggleChangedHandler);
            _surfaceCoordToggle?.UnregisterValueChangedCallback(_surfaceCoordToggleChangedHandler);
            _refCoordToggle?.UnregisterValueChangedCallback(_refCoordToggleChangedHandler);
            _inactiveTransparentToggle?.UnregisterValueChangedCallback(_inactiveTransparentToggleChangedHandler);
        }

        private void OnSettingsStateChanged(SettingsState state)
        {
            _isUpdatingFromState = true;

            _whiteBackgroundToggle.SetValueWithoutNotify(state.Background == Color.white);
            _surfaceCoordToggle.SetValueWithoutNotify(state.ShowSurfaceCoordinate);
            _refCoordToggle.SetValueWithoutNotify(state.ShowBregmaAxis);
            _inactiveTransparentToggle.SetValueWithoutNotify(state.GhostInactiveProbes);

            _isUpdatingFromState = false;
        }

        private void OnShuttingDown()
        {
            UnregisterEventHandlers();
            _settingsStateSubscription?.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }
    }
}
