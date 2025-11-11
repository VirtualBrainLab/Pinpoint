using System;
using Models;
using Models.Settings;
using Services;
using UI;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace UI.ViewModels
{
    public class RigViewModel
    {
        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _rigStateSubscription;

        public RigViewModel(StoreService storeService)
        {
            _storeService = storeService;

            _rigStateSubscription = storeService.Store.Subscribe(
                           state => state.Get<RigState>(SliceNames.RIG_SLICE),
                OnRigStateChanged,
                  new SubscribeOptions<RigState> { fireImmediately = true }
                     );
            App.shuttingDown += OnShuttingDown;
        }

        private void OnRigStateChanged(RigState state)
        {
            var toggleRigs = GameObject.FindFirstObjectByType<TP_ToggleRigs>();
            if (toggleRigs == null)
                return;

            ApplyRigVisibility("well", state.WellVisible, toggleRigs);
            ApplyRigVisibility("rig-widefield", state.RigWidefieldVisible, toggleRigs);
            ApplyRigVisibility("mouse-skull", state.MouseSkullVisible, toggleRigs);
            ApplyRigVisibility("rat-skull", state.RatSkullVisible, toggleRigs);
            ApplyRigVisibility("ibl-center", state.IblCenterVisible, toggleRigs);
            ApplyRigVisibility("ibl-front", state.IblFrontVisible, toggleRigs);
            ApplyRigVisibility("ibl-back", state.IblBackVisible, toggleRigs);
            ApplyRigVisibility("ucla", state.UclaVisible, toggleRigs);
        }

        private void ApplyRigVisibility(string rigName, bool shouldBeVisible, TP_ToggleRigs toggleRigs)
        {
            toggleRigs.ToggleRigVisibility(rigName, shouldBeVisible);
        }

        private void OnShuttingDown()
        {
            _rigStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }
    }
}
