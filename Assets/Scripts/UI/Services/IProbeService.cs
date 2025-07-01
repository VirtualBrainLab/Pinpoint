using System;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Services
{
    /// <summary>
    /// Interface for probe services, providing methods to start polling and retrieve
    /// the active probe's color and override name.
    /// </summary>
    /// <remarks>Polls for probe data automatically. Assumes it is a singleton.</remarks>
    public interface IProbeService : IDisposable
    {
        event Action OnPropertyChanged;
        
        /// <summary>
        /// Color of the currently active probe. Gray if no active probe is set.
        /// </summary>
        Color ActiveProbeColor { get; }
        
        /// <summary>
        /// Override name of the currently active probe. "No Active Probe" if none is set.
        /// </summary>
        string ActiveProbeName { get; }
    }
}
