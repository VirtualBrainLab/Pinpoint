using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using BrainAtlas;
using UnityEngine;

namespace Services
{
    public static class ProbeService
    {
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
    }
}
