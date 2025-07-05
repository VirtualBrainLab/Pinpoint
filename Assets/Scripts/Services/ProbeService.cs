using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using BrainAtlas;
using Models;
using Models.Scene;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace Services
{
    public class ProbeService
    {
        [Service]
        private StoreService _storeService;

        public static async Task<bool> ResetActiveProbeReferenceCoordinate()
        {
            var activeProbeManager = ProbeManager.ActiveProbeManager;

            // Exit if there is no active probe manager.
            if (!activeProbeManager || !activeProbeManager.IsEphysLinkControlled)
            {
                return false;
            }

            return await activeProbeManager.ManipulatorBehaviorController.ResetReferenceCoordinate();
        }

        public static async Task<bool> DriveActiveProbeToTargetEntryCoordinate()
        {
            var activeProbeManager = ProbeManager.ActiveProbeManager;

            // Exit if there is no active probe manager.
            if (!activeProbeManager || !activeProbeManager.IsEphysLinkControlled)
            {
                return false;
            }

            return await activeProbeManager.ManipulatorBehaviorController.DriveToTargetEntryCoordinate();
        }

        public static async Task<bool> StopActiveProbeDriveToTargetEntryCoordinate()
        {
            var activeProbeManager = ProbeManager.ActiveProbeManager;

            // Exit if there is no active probe manager.
            if (!activeProbeManager || !activeProbeManager.IsEphysLinkControlled)
            {
                return false;
            }

            return await activeProbeManager.ManipulatorBehaviorController.StopDriveToTargetEntryCoordinate();
        }

        public static async Task<bool> ResetActiveProbeDuraOffset()
        {
            var activeProbeManager = ProbeManager.ActiveProbeManager;

            // Exit if there is no active probe manager.
            if (!activeProbeManager || !activeProbeManager.IsEphysLinkControlled)
            {
                return false;
            }

            return await activeProbeManager.ManipulatorBehaviorController.ResetDuraOffset();
        }

        public void InsertionDriveActiveProbe()
        {
            var activeProbeState = _storeService
                .Store.GetState<SceneState>(SliceNames.SCENE_SLICE)
                .ActiveProbeState;
            var activeProbeManager = ProbeManager.ActiveProbeManager;

            // Exit if there is no active probe state or active automation probe.
            if (
                activeProbeState == null
                || !activeProbeManager
                || !activeProbeManager.IsEphysLinkControlled
            )
            {
                return;
            }

            // TODO: Update to not use ProbeManager directly.
            _ = activeProbeManager.ManipulatorBehaviorController.Drive(
                new ProbeManager(),
                activeProbeState.InsertionBaseSpeed,
                activeProbeState.DrivePastDistance
            );
        }

        public async Task<bool> InsertionExitActiveProbe()
        {
            var activeProbeState = _storeService
                .Store.GetState<SceneState>(SliceNames.SCENE_SLICE)
                .ActiveProbeState;
            var activeProbeManager = ProbeManager.ActiveProbeManager;

            // Exit if there is no active probe manager.
            if (
                activeProbeState == null
                || !activeProbeManager
                || !activeProbeManager.IsEphysLinkControlled
            )
            {
                return false;
            }

            // TODO: Update to not use ProbeManager data directly.
            return await activeProbeManager.ManipulatorBehaviorController.Exit(
                new ProbeManager(),
                activeProbeState.InsertionBaseSpeed
            );
        }

        public static async Task<bool> StopInsertionDriveActiveProbe()
        {
            var activeProbeManager = ProbeManager.ActiveProbeManager;

            // Exit if there is no active probe manager.
            if (!activeProbeManager || !activeProbeManager.IsEphysLinkControlled)
            {
                return false;
            }

            return await activeProbeManager.ManipulatorBehaviorController.StopInsertion();
        }
    }
}
