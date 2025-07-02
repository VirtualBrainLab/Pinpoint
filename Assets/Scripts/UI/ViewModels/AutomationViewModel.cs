using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Models;
using Models.Scene;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class AutomationViewModel
    {
        #region Services

        private readonly IStoreService _storeService;
        private readonly IDisposableSubscription _automationStateSubscription;
        private readonly IDisposableSubscription _probeAutomationStateSubscription;
        private readonly IProbeService _probeService;

        #endregion
        #region Properties

        [ObservableProperty]
        private bool _isAutomationEnabled = true;

        [ObservableProperty]
        private Vector4 _referenceCoordinate;

        // TODO: This should be a list of probe data models when that is implemented.
        /// <summary>
        /// Filtered list of targetable insertion probes for the active manipulator probe.
        ///
        /// For insertions that are co-terminal and have not been selected yet.
        /// </summary>
        [ObservableProperty]
        private List<ProbeManager> _targetInsertionProbeManagers;

        /// <summary>
        /// Selected target insertion probe manager dropdown index.
        /// </summary>
        [ObservableProperty]
        private int _selectedTargetInsertionProbeManagerIndex;

        #endregion

        public AutomationViewModel(IStoreService storeService, IProbeService probeService)
        {
            // Register services.
            _storeService = storeService;
            _probeService = probeService;

            // Initialize properties from the store.
            var initialAutomationState = _storeService.Store.GetState<SceneState>(
                SliceNames.SCENE_SLICE
            );
            OnAutomationStateChanged(initialAutomationState);
            OnExternalPropertiesChanged();

            // Subscribe to state changes.
            _automationStateSubscription = _storeService.Store.Subscribe(
                state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                OnAutomationStateChanged
            );
            probeService.OnPropertyChanged += OnExternalPropertiesChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnAutomationStateChanged(SceneState state)
        {
            // Check if an active manipulator probe is selected.
            // TODO: Re-enable when actually using automation.
            IsAutomationEnabled = state.ActiveProbeIndex > -1;

            // Exit if not enabled.
            if (!IsAutomationEnabled)
            {
                return;
            }

            // Get this probe's automation state.
            var probeAutomationState = state.Probes[state.ActiveProbeIndex];

            // Check for the index of the selected target insertion probe manager.
            var selectedTargetInsertionProbeManagerIndex = TargetInsertionProbeManagers.IndexOf(
                probeAutomationState.SelectedTargetInsertionProbeState
            );

            // Reset the selected target insertion probe manager index if it is not valid.
            if (selectedTargetInsertionProbeManagerIndex < 0)
            {
                // TODO: dispatch an action to reset the selected target insertion probe manager.
            }

            // Set the selected target insertion probe manager index to the resolved index.
            SelectedTargetInsertionProbeManagerIndex = selectedTargetInsertionProbeManagerIndex;
        }

        private void OnExternalPropertiesChanged()
        {
            // Keep the active probe in sync with the store.
            _storeService.Store.Dispatch(
                SceneActions.SET_ACTIVE_PROBE_INDEX,
                _probeService.ActiveProbeAutomationStateIndex
            );

            ReferenceCoordinate = _probeService.ActiveProbeReferenceCoordinate;

            TargetInsertionProbeManagers = _probeService
                .TargetableInsertionProbeManagers.Where(manager =>
                    IsCoterminal(
                        manager.ProbeController.Insertion.APMLDV,
                        _probeService.ActiveProbeAngles
                    )
                )
                .ToList();
            return;

            bool IsCoterminal(Vector3 first, Vector3 second)
            {
                return Mathf.Abs(first.x - second.x) % 360 < 0.01f
                    && Mathf.Abs(first.y - second.y) % 360 < 0.01f
                    && Mathf.Abs(first.z - second.z) % 360 < 0.01f;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedTargetInsertionProbeManagerIndex):
                    var selectedTargetInsertionProbeManager = TargetInsertionProbeManagers[
                        SelectedTargetInsertionProbeManagerIndex
                    ];
                    _storeService.Store.Dispatch(
                        SceneActions.SET_SELECTED_TARGET_INSERTION_PROBE_STATE,
                        selectedTargetInsertionProbeManager
                    );
                    break;
            }
        }

        private void OnShuttingDown()
        {
            _probeService.OnPropertyChanged -= OnExternalPropertiesChanged;
            App.shuttingDown -= OnShuttingDown;
            _automationStateSubscription.Dispose();
            _probeService.Dispose();
        }

        #region Commands

        [ICommand]
        private void ResetReferenceCoordinate()
        {
            _probeService
                .ResetActiveProbeReferenceCoordinate()
                .ContinueWith(task =>
                {
                    if (!task.Result)
                    {
                        return;
                    }

                    // TODO: Advance to the next state.
                });
        }

        #endregion
    }
}
