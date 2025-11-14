using Models;
using Models.Scene;
using UI;
using Unity.AppUI.MVVM;
using UnityEngine;

/// <summary>
/// Example component that demonstrates how to subscribe to the ProbeWorldState slice.
/// This can be used as a reference for other components that need to react to
/// world-space probe position updates.
/// </summary>
/// <example>
/// // In your MonoBehaviour or component:
/// private IDisposableSubscription _probeWorldStateSubscription;
///
/// void Start()
/// {
///     _probeWorldStateSubscription = PinpointApp.StoreServiceStore.Subscribe(
///         state => state.Get&lt;ProbeWorldStateSlice&gt;(SliceNames.PROBE_WORLD_SLICE),
///         OnProbeWorldStateChanged,
///         new SubscribeOptions&lt;ProbeWorldStateSlice&gt; { fireImmediately = true }
///     );
/// }
///
/// void OnProbeWorldStateChanged(ProbeWorldStateSlice probeWorldState)
/// {
///     // React to changes in probe world states
///     foreach (var probeWorld in probeWorldState.ProbeWorldStates)
///     {
///         Debug.Log($"Probe {probeWorld.Name} tip position: {probeWorld.TipPositionWorldU}");
///         Debug.Log($"Probe {probeWorld.Name} is in brain: {probeWorld.IsProbeInBrain}");
///         if (probeWorld.IsProbeInBrain)
///         {
///             Debug.Log($"Surface coordinate: {probeWorld.SurfaceCoordinateWorldT}");
///         }
///     }
/// }
///
/// void OnDestroy()
/// {
///     _probeWorldStateSubscription?.Dispose();
/// }
/// </example>
public class ProbeWorldStateSubscriptionExample : MonoBehaviour
{
    private IDisposableSubscription _probeWorldStateSubscription;

    void Start()
    {
#if APP_UI
        // Subscribe to the probe world state slice
        _probeWorldStateSubscription = PinpointApp.StoreServiceStore.Subscribe(
            state => state.Get<ProbeWorldStateSlice>(SliceNames.PROBE_WORLD_SLICE),
            OnProbeWorldStateChanged,
            new SubscribeOptions<ProbeWorldStateSlice> { fireImmediately = true }
        );
#endif
    }

    /// <summary>
    /// Called whenever the probe world state changes
    /// </summary>
    void OnProbeWorldStateChanged(ProbeWorldStateSlice probeWorldState)
    {
        // Example: Log all probe world states
        foreach (var probeWorld in probeWorldState.ProbeWorldStates)
        {
            Debug.Log($"[ProbeWorldState] Probe: {probeWorld.Name}");
            Debug.Log($"  - Tip Position (WorldU): {probeWorld.TipPositionWorldU}");
            Debug.Log($"  - Tip Position (WorldT): {probeWorld.TipPositionWorldT}");
            Debug.Log($"  - Forward Vector: {probeWorld.TipForwardWorldU}");
            Debug.Log($"  - In Brain: {probeWorld.IsProbeInBrain}");
            
            if (probeWorld.IsProbeInBrain)
            {
                Debug.Log($"  - Surface Coordinate (WorldT): {probeWorld.SurfaceCoordinateWorldT}");
                Debug.Log($"  - Surface Coordinate (WorldU): {probeWorld.SurfaceCoordinateWorldU}");
            }
        }
    }

    void OnDestroy()
    {
#if APP_UI
        // Clean up subscription
        _probeWorldStateSubscription?.Dispose();
#endif
    }
}
