using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using BrainAtlas;
using UnityEngine;

namespace Services
{
    public class ProbeService
    {
        public async Task<bool> ResetActiveProbeReferenceCoordinate()
        {
            var activeProbeManager = ProbeManager.ActiveProbeManager;

            // Exit if there is no active probe manager.
            if (!activeProbeManager || !activeProbeManager.IsEphysLinkControlled)
            {
                return false;
            }

            return await activeProbeManager.ManipulatorBehaviorController.ResetReferenceCoordinate();
        }
    }
}
