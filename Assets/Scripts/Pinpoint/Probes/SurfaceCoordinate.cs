using Models;
using Models.Scene;
using Models.Settings;
using Services;
using UI;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

public class SurfaceCoordinate : MonoBehaviour
{
    private IDisposableSubscription _probeWorldStateSubscription;
    private IDisposableSubscription _settingsStateSubscription;

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;

    private bool _showSurfaceCoordinate;
    private bool _isProbeInBrain;
    private string _activeProbeWorldStateName;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();

        // Start hidden by disabling the renderer
        if (_renderer != null)
            _renderer.enabled = false;
    }

    private void Start()
    {
#if APP_UI
        var storeService = PinpointApp.Services.GetRequiredService<StoreService>();

        // Subscribe to settings state for visibility toggle
        _settingsStateSubscription = storeService.Store.Subscribe(
            state => state.Get<SettingsState>(SliceNames.SETTINGS_SLICE),
            OnSettingsStateChanged,
      new SubscribeOptions<SettingsState> { fireImmediately = true }
    );

        // Subscribe to probe world state for position and brain status
        _probeWorldStateSubscription = storeService.Store.Subscribe(
  state =>
   {
       var sceneState = state.Get<SceneState>(SliceNames.SCENE_SLICE);
       var probeWorldSlice = state.Get<ProbeWorldStateSlice>(SliceNames.PROBE_WORLD_SLICE);
       return probeWorldSlice.GetProbeWorldState(sceneState.ActiveProbeName);
   },
  OnProbeWorldStateChanged,
         new SubscribeOptions<ProbeWorldState> { fireImmediately = true }
        );
#endif
    }

    private void OnDestroy()
    {
        _settingsStateSubscription?.Dispose();
        _probeWorldStateSubscription?.Dispose();
    }

    private void OnSettingsStateChanged(SettingsState state)
    {
        _showSurfaceCoordinate = state.ShowSurfaceCoordinate;
        UpdateVisibility();
    }

    private void OnProbeWorldStateChanged(ProbeWorldState probeWorldState)
    {
        if (probeWorldState == null || string.IsNullOrEmpty(probeWorldState.Name))
        {
            _activeProbeWorldStateName = null;
            _isProbeInBrain = false;
            UpdateVisibility();
            return;
        }

        _activeProbeWorldStateName = probeWorldState.Name;
        _isProbeInBrain = probeWorldState.IsProbeInBrain;

        if (_isProbeInBrain)
        {
            // Update position
            transform.position = probeWorldState.SurfaceCoordinateWorldT;

            // Update color to match probe color - need to get from SceneState
            var storeService = PinpointApp.Services.GetRequiredService<StoreService>();
            var sceneState = storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
            var probeState = sceneState.Probes.Find(p => p.Name == _activeProbeWorldStateName);

            if (probeState != null)
            {
                Color color = ProbeProperties.ProbeColors[(int)probeState.Color];
                _renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_Color", color);
                _renderer.SetPropertyBlock(_propertyBlock);
            }
        }

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_renderer == null)
            return;

        // Show only if setting is enabled, probe is in brain, and we have an active probe
        bool shouldBeVisible = _showSurfaceCoordinate &&
                 _isProbeInBrain &&
         !string.IsNullOrEmpty(_activeProbeWorldStateName);

        _renderer.enabled = shouldBeVisible;
    }
}
