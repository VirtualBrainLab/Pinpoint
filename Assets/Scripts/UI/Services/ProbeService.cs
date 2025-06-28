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
        public string ActiveProbeOverrideName { get; private set; } = "No Active Probe";

        #endregion

        public ProbeService()
        {
            _timer = new Timer(500); // Poll every 500 milliseconds.
            _timer.Elapsed += Update;
            _timer.AutoReset = true;
            _timer.Start();
        }


        private void Update(object sender, ElapsedEventArgs e)
        {
            var activeProbeManager = ProbeManager.ActiveProbeManager;
            var hasChanged = false;

            if (activeProbeManager.Color != ActiveProbeColor)
            {
                ActiveProbeColor = activeProbeManager.Color;
                hasChanged = true;
            }

            if (activeProbeManager.OverrideName != ActiveProbeOverrideName)
            {
                ActiveProbeOverrideName = activeProbeManager.OverrideName;
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
    }
}
