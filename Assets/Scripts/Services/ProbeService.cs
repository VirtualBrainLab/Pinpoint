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
        private readonly Timer _timer;

        #region Properties

        public event Action OnPropertyChanged;
        public Color ActiveProbeColor { get; private set; } = Color.gray;
        public string ActiveProbeName { get; private set; } = "No Active Probe";
        public int ActiveProbeAutomationStateIndex { get; private set; } = -1;
        public Vector3 ActiveProbeAngles { get; private set; }
        public Vector4 ActiveProbeReferenceCoordinate { get; private set; }
        public IEnumerable<ProbeManager> TargetableInsertionProbeManagers { get; private set; }

        #endregion

        public ProbeService()
        {
            _timer = new Timer(250); // Poll 4 Hz.
            _timer.Elapsed += Update;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void Update(object sender, ElapsedEventArgs e)
        {
            var activeProbeManager = ProbeManager.ActiveProbeManager;

            // Exit if there is no active probe manager.
            if (!activeProbeManager)
            {
                return;
            }

            // Flag for changes.
            var hasChanged = false;

            // Check for changes.

            if (activeProbeManager.Color != ActiveProbeColor)
            {
                ActiveProbeColor = activeProbeManager.Color;
                hasChanged = true;
            }

            if (
                activeProbeManager.UUID != ActiveProbeName
                || (
                    activeProbeManager.OverrideName != null
                    && activeProbeManager.OverrideName != ActiveProbeName
                )
            )
            {
                ActiveProbeName = activeProbeManager.OverrideName ?? activeProbeManager.UUID;
                hasChanged = true;
            }

            if (activeProbeManager.ProbeController.Insertion.Angles != ActiveProbeAngles)
            {
                ActiveProbeAngles = activeProbeManager.ProbeController.Insertion.Angles;
                hasChanged = true;
            }

            switch (activeProbeManager.IsEphysLinkControlled)
            {
                case false when ActiveProbeAutomationStateIndex != -1:
                    ActiveProbeAutomationStateIndex = -1;
                    hasChanged = true;
                    break;
                case false when ActiveProbeReferenceCoordinate != Vector4.zero:
                    ActiveProbeReferenceCoordinate = Vector4.zero;
                    hasChanged = true;
                    break;
                case true
                    when activeProbeManager.ManipulatorBehaviorController.ProbeAutomationStateIndex
                        != ActiveProbeAutomationStateIndex:
                    ActiveProbeAutomationStateIndex = activeProbeManager
                        .ManipulatorBehaviorController
                        .ProbeAutomationStateIndex;
                    hasChanged = true;
                    break;
                case true
                    when activeProbeManager.ManipulatorBehaviorController.ReferenceCoordinateOffset
                        != ActiveProbeReferenceCoordinate:
                    ActiveProbeReferenceCoordinate = activeProbeManager
                        .ManipulatorBehaviorController
                        .ReferenceCoordinateOffset;
                    hasChanged = true;
                    break;
            }

            var targetableInsertionProbeManagers = ProbeManager
                .Instances
                // 1. Are not EphysLink controlled.
                .Where(manager => !manager.IsEphysLinkControlled)
                // 2. Are inside the brain (non-NaN entry coordinate).
                .Where(manager =>
                    !float.IsNaN(
                        manager
                            .FindEntryIdxCoordinate(
                                BrainAtlasManager.ActiveReferenceAtlas.World2AtlasIdx(
                                    manager.ProbeController.Insertion.PositionWorldU()
                                ),
                                BrainAtlasManager.ActiveReferenceAtlas.World2Atlas_Vector(
                                    manager.ProbeController.GetTipWorldU().tipUpWorldU
                                )
                            )
                            .x
                    )
                );

            if (!targetableInsertionProbeManagers.Equals(TargetableInsertionProbeManagers))
            {
                TargetableInsertionProbeManagers = targetableInsertionProbeManagers;
                hasChanged = true;
            }

            // Signal property change if any value has changed.
            if (hasChanged)
            {
                OnPropertyChanged?.Invoke();
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public void SetActiveProbeAutomationStateIndex(int index)
        {
            var activeProbeManager = ProbeManager.ActiveProbeManager;

            // Exit if there is no active probe manager.
            if (!activeProbeManager || !activeProbeManager.IsEphysLinkControlled)
            {
                return;
            }

            activeProbeManager.ManipulatorBehaviorController.ProbeAutomationStateIndex = index;
        }

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
