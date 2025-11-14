# ProbeWorldState - World-Space Probe Position State

## Overview

The `ProbeWorldState` slice is an unsaved (transient) Redux state that provides computed world-space position and orientation data for all probes in the scene. This state is automatically updated whenever probes move and their transforms are recalculated.

## Purpose

This state slice was created to:
1. Expose computed world-space values from ProbeManager and ProbeController
2. Allow other components to subscribe to and react to probe position changes
3. Separate transient computed values from saved state (like SceneState)
4. Provide a single source of truth for probe world-space positions

## Data Structure

### ProbeWorldState
Each probe has a `ProbeWorldState` record containing:

- **Name**: Identifier of the probe (matches ProbeState.Name)
- **World-Space Position & Orientation**:
  - `TipPositionWorldU`: Tip position in untransformed coordinates
  - `TipPositionWorldT`: Tip position in transformed coordinates
  - `TipRightWorldU`: Right vector in world space
  - `TipUpWorldU`: Up vector in world space
  - `TipForwardWorldU`: Forward vector in world space
- **Surface Coordinates**:
  - `SurfaceCoordinateWorldT`: Brain surface in transformed world space
  - `SurfaceCoordinateWorldU`: Brain surface in untransformed world space
  - `SurfaceCoordinateT`: Brain surface in atlas transformed space
  - `IsProbeInBrain`: Boolean indicating if probe is in brain
- **Recording Region**:
  - `RecRegionBaseCoordWorldU`: Base of recording region
  - `RecRegionTopCoordWorldU`: Top of recording region

### ProbeWorldStateSlice
The slice contains:
- `ProbeWorldStates`: List of all probe world states
- `GetProbeWorldState(string name)`: Helper method to get a specific probe's state

## Update Flow

1. ProbeController updates probe transform based on CCF coordinates (APMLDV)
2. `ProbeController.SetTipWorldU()` computes tip coordinates
3. `ProbeManager.ProbeMoved()` computes recording region coordinates
4. `ProbeManager.UpdateSurfacePosition()` computes surface coordinates
5. `ProbeManager.DispatchProbeWorldState()` dispatches to Redux store
6. All subscribers are notified of the update

## Usage

### Subscribing to Updates

```csharp
using Models;
using Models.Scene;
using UI;
using Unity.AppUI.MVVM;

public class MyComponent : MonoBehaviour
{
    private IDisposableSubscription _subscription;

    void Start()
    {
        // Subscribe to all probe world states
        _subscription = PinpointApp.StoreServiceStore.Subscribe(
            state => state.Get<ProbeWorldStateSlice>(SliceNames.PROBE_WORLD_SLICE),
            OnProbeWorldStateChanged,
            new SubscribeOptions<ProbeWorldStateSlice> { fireImmediately = true }
        );
    }

    void OnProbeWorldStateChanged(ProbeWorldStateSlice worldState)
    {
        foreach (var probe in worldState.ProbeWorldStates)
        {
            // React to probe world state changes
            Debug.Log($"Probe {probe.Name} at {probe.TipPositionWorldU}");
        }
    }

    void OnDestroy()
    {
        _subscription?.Dispose();
    }
}
```

### Subscribing to a Specific Probe

```csharp
void Start()
{
    string targetProbeName = "my-probe-name";
    
    _subscription = PinpointApp.StoreServiceStore.Subscribe(
        state => state.Get<ProbeWorldStateSlice>(SliceNames.PROBE_WORLD_SLICE)
                     .GetProbeWorldState(targetProbeName),
        OnSpecificProbeChanged,
        new SubscribeOptions<ProbeWorldState> { fireImmediately = true }
    );
}

void OnSpecificProbeChanged(ProbeWorldState probeWorld)
{
    if (probeWorld == null) return;
    
    // React to specific probe changes
    if (probeWorld.IsProbeInBrain)
    {
        Vector3 surface = probeWorld.SurfaceCoordinateWorldT;
        // Do something with surface coordinate
    }
}
```

## Actions

### UPDATE_PROBE_WORLD_STATE
Updates or adds a probe's world state.
```csharp
PinpointApp.StoreServiceStore.Dispatch(
    ProbeWorldActions.UPDATE_PROBE_WORLD_STATE, 
    probeWorldState
);
```

### REMOVE_PROBE_WORLD_STATE
Removes a probe's world state when the probe is destroyed.
```csharp
PinpointApp.StoreServiceStore.Dispatch(
    ProbeWorldActions.REMOVE_PROBE_WORLD_STATE, 
    probeName
);
```

### CLEAR_ALL_PROBE_WORLD_STATES
Clears all probe world states.
```csharp
PinpointApp.StoreServiceStore.Dispatch(
    ProbeWorldActions.CLEAR_ALL_PROBE_WORLD_STATES
);
```

## Important Notes

1. **Unsaved State**: This state is NOT persisted to local storage. It's computed on-demand.
2. **Automatic Updates**: The state is automatically updated by ProbeManager - you don't need to dispatch updates manually.
3. **Computed Values**: All values are computed after CCF coordinate updates are complete.
4. **Performance**: Updates only occur when probes actually move (via UpdateSurfacePosition).

## See Also

- `ProbeState.cs` - Saved probe state (CCF coordinates, angles, etc.)
- `SceneState.cs` - Overall scene state
- `ProbeManager.cs` - Manages probe lifecycle and computes world state
- `ProbeController.cs` - Controls probe transforms
