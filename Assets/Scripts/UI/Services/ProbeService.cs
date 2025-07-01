using System;
using System.Timers;
using UnityEngine;

namespace UI.Services
{
    public class ProbeService : IProbeService
    {
        private readonly Timer _timer;

        #region Properties

        public event Action OnPropertyChanged;
        public Color ActiveProbeColor { get; private set; } = Color.gray;
        public string ActiveProbeName { get; private set; } = "No Active Probe";
        public int ActiveProbeAutomationStateIndex { get; private set; } = -1;
        public Vector4 ActiveProbeReferenceCoordinate { get; private set; } = Vector4.zero;

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

        public void setActiveProbeReferenceCoordinate(Vector4 coordinate)
        {
            var activeProbeManager = ProbeManager.ActiveProbeManager;

            // Exit if there is no active probe manager.
            if (!activeProbeManager || !activeProbeManager.IsEphysLinkControlled)
            {
                return;
            }

            activeProbeManager.ManipulatorBehaviorController.ReferenceCoordinateOffset = coordinate;
        }
    }
}
