using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Services
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
        /// Name of the currently active probe. "No Active Probe" if none is set.
        /// </summary>
        string ActiveProbeName { get; }

        /// <summary>
        /// Automation state list index of the currently active probe.
        /// </summary>
        int ActiveProbeAutomationStateIndex { get; }
        
        Vector3 ActiveProbeAngles { get; }

        /// <summary>
        /// Reference coordinate offset of the currently active probe.
        /// </summary>
        Vector4 ActiveProbeReferenceCoordinate { get; }

        /// <summary>
        /// All targetable insertion probe managers.
        /// </summary>
        IEnumerable<ProbeManager> TargetableInsertionProbeManagers { get; }
        
        /// <summary>
        /// Set the active probe's automation state index.
        /// </summary>
        /// <param name="index">Index in the state list.</param>
        void SetActiveProbeAutomationStateIndex(int index);

        /// <summary>
        /// Call for resetting the active probe's reference coordinate.
        /// </summary>
        Task<bool> ResetActiveProbeReferenceCoordinate();
    }
}
