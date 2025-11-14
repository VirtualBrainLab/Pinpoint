# Implementation Summary: World-Space Probe Position Subscribable State

## Issue Addressed
Created an unsaved state slice that reflects world-space computed values from Probe Manager and Probe Controller, allowing other components to subscribe to and react to changes in probe positions.

## Implementation Overview

### Files Created

1. **ProbeWorldState.cs**
   - Record type containing world-space probe data
   - Fields include:
     - Tip position (WorldU and WorldT)
     - Tip orientation vectors (right, up, forward)
     - Surface coordinates (T, WorldT, WorldU)
     - Recording region coordinates
     - IsProbeInBrain flag

2. **ProbeWorldStateSlice.cs**
   - Container for all probe world states
   - Provides helper method to get specific probe state by name

3. **ProbeWorldReducers.cs**
   - Redux reducers for updating, removing, and clearing probe world states
   - Actions: UPDATE_PROBE_WORLD_STATE, REMOVE_PROBE_WORLD_STATE, CLEAR_ALL_PROBE_WORLD_STATES

4. **ProbeWorldState_README.md**
   - Comprehensive documentation
   - Usage examples
   - API reference

5. **ProbeWorldStateSubscriptionExample.cs**
   - Working example showing how to subscribe to the state
   - Demonstrates both full slice and specific probe subscriptions

### Files Modified

1. **SliceNames.cs**
   - Added `PROBE_WORLD_SLICE` constant

2. **StoreService.cs**
   - Registered the probe world slice with Redux store
   - Initialized as unsaved state (not persisted to local storage)
   - Configured reducers for all actions

3. **ProbeManager.cs**
   - Added `DispatchProbeWorldState()` method to create and dispatch world state
   - Modified `UpdateSurfacePosition()` to dispatch state after all computations
   - Modified `OnDestroy()` to remove probe world state on cleanup

## Data Flow

```
1. User/System moves probe (updates APMLDV in SceneState)
   ↓
2. ProbeController.SetProbePosition() updates transform
   ↓
3. ProbeController.SetTipWorldU() computes tip coordinates
   ↓
4. ProbeManager.ProbeMoved() computes recording region
   ↓
5. ProbeManager.UpdateSurfacePosition() computes surface coordinates
   ↓
6. ProbeManager.DispatchProbeWorldState() dispatches to Redux
   ↓
7. All subscribers receive ProbeWorldStateSlice update
```

## Key Design Decisions

### 1. Unsaved State
- The slice is NOT persisted to local storage
- Rationale: This is computed/transient data derived from saved state
- Benefits: Reduces storage size, always fresh on load

### 2. Update Location
- World state is dispatched from `UpdateSurfacePosition()`
- Rationale: This is called AFTER all probe transforms are updated
- Benefits: Ensures all computed values are current and consistent

### 3. Data Structure
- Mirrors SceneState pattern with list of states
- Rationale: Consistency with existing codebase patterns
- Benefits: Familiar structure, easy to work with

### 4. Conditional Compilation
- Uses `#if APP_UI` guards around Redux code
- Rationale: Maintains compatibility with non-APP_UI builds
- Benefits: Backwards compatibility

## Usage Pattern

Components can subscribe to probe world state changes:

```csharp
// Subscribe to all probes
_subscription = PinpointApp.StoreServiceStore.Subscribe(
    state => state.Get<ProbeWorldStateSlice>(SliceNames.PROBE_WORLD_SLICE),
    OnProbeWorldStateChanged,
    new SubscribeOptions<ProbeWorldStateSlice> { fireImmediately = true }
);

// Subscribe to specific probe
_subscription = PinpointApp.StoreServiceStore.Subscribe(
    state => state.Get<ProbeWorldStateSlice>(SliceNames.PROBE_WORLD_SLICE)
                 .GetProbeWorldState(probeName),
    OnSpecificProbeChanged,
    new SubscribeOptions<ProbeWorldState> { fireImmediately = true }
);
```

## Testing Recommendations

1. **Unit Tests**: Test reducers independently
   - UpdateProbeWorldStateReducer adds/updates correctly
   - RemoveProbeWorldStateReducer removes correctly
   - ClearAllProbeWorldStatesReducer clears all states

2. **Integration Tests**: Test with ProbeManager
   - World state updates when probe moves
   - World state removed when probe destroyed
   - Surface coordinates computed correctly

3. **Subscription Tests**: Test reactive updates
   - Subscribers notified on probe move
   - Multiple subscribers receive same update
   - Subscription cleanup works properly

## Future Enhancements

1. **Performance**: Consider debouncing updates for rapid movements
2. **Filtering**: Add options to filter which probes dispatch updates
3. **Middleware**: Add middleware for logging state changes
4. **Validation**: Add validation for world state values
5. **Serialization**: Add JSON serialization for debugging

## Compatibility

- Compatible with APP_UI builds (conditional compilation)
- No changes to existing saved state format
- No breaking changes to existing APIs
- Backwards compatible with non-Redux code paths

## Security Considerations

- No security vulnerabilities detected by CodeQL
- No sensitive data stored in state
- No external dependencies added
- Follows existing security patterns
