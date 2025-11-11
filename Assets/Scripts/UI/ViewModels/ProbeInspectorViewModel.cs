using System.Linq;
using Models;
using Models.Scene;
using Models.Settings;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using Utils.Types;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class ProbeInspectorViewModel
    {
        #region Constants

        private readonly Vector2 _pitchRange = new(0, 90);

        #endregion

        #region Services

        private readonly StoreService _storeService;
        private readonly IDisposableSubscription _sceneStateSubscription;
        private readonly IDisposableSubscription _settingsStateSubscription;

        private string ActiveProbeName =>
   _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE).ActiveProbeName;

        #endregion

        #region Properties

        [ObservableProperty]
        private Vector3 _position;

        [ObservableProperty]
        private Vector3 _angles;

        [ObservableProperty]
        private bool _locked;

        [ObservableProperty]
        private ProbeColor _probeColor;

        [ObservableProperty]
        private string _visualizingManipulatorId;

        [ObservableProperty]
        private bool _convertAPML2Probe;

        #endregion

        public ProbeInspectorViewModel(StoreService storeService)
        {
            _storeService = storeService;

            _sceneStateSubscription = _storeService.Store.Subscribe(
                           state => state.Get<SceneState>(SliceNames.SCENE_SLICE),
                  OnSceneStateChanged,
                  new SubscribeOptions<SceneState> { fireImmediately = true }
                     );
            _settingsStateSubscription = _storeService.Store.Subscribe(
      state => state.Get<SettingsState>(SliceNames.SETTINGS_SLICE),
   OnSettingsStateChanged,
      new SubscribeOptions<SettingsState> { fireImmediately = true }
        );
            App.shuttingDown += OnShuttingDown;
        }

        private void OnSceneStateChanged(SceneState sceneState)
        {
            if (string.IsNullOrEmpty(sceneState.ActiveProbeName))
                return;

            Vector3 apmldv = sceneState.ActiveProbeState.APMLDV;
            Vector3 angles = sceneState.ActiveProbeState.Angles;

            if (_convertAPML2Probe)
            {
                float cos = Mathf.Cos(-angles.x * Mathf.Deg2Rad);
                float sin = Mathf.Sin(-angles.x * Mathf.Deg2Rad);

                float xRot = apmldv.x * cos - apmldv.y * sin;
                float yRot = apmldv.x * sin + apmldv.y * cos;

                Position = new Vector3(xRot, yRot, apmldv.z);
            }
            else
            {
                Position = apmldv;
            }

            Angles = angles;
            Locked = sceneState.ActiveProbeState.Locked;
            ProbeColor = sceneState.ActiveProbeState.Color;
            VisualizingManipulatorId =
               sceneState
             .Manipulators.FirstOrDefault(state =>
          state.VisualizationProbeName == sceneState.ActiveProbeName
            )
    ?.Id ?? string.Empty;
        }

        private void OnSettingsStateChanged(SettingsState settingsState)
        {
            ConvertAPML2Probe = settingsState.ConvertAPML2Probe;
        }

        private void OnShuttingDown()
        {
            _sceneStateSubscription.Dispose();
            _settingsStateSubscription.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }

        #region Commands

        [ICommand]
        private void SetPosition(Vector3 position)
        {
            Vector3 apmldv = position;

            if (_convertAPML2Probe)
            {
                var sceneState = _storeService.Store.GetState<SceneState>(SliceNames.SCENE_SLICE);
                Vector3 angles = sceneState.ActiveProbeState.Angles;

                float cos = Mathf.Cos(-angles.x * Mathf.Deg2Rad);
                float sin = Mathf.Sin(-angles.x * Mathf.Deg2Rad);

                float xRot = position.x * cos + position.y * sin;
                float yRot = -position.x * sin + position.y * cos;

                apmldv = new Vector3(xRot, yRot, position.z);
            }

            _storeService.Store.Dispatch(
   SceneActions.SET_PROBE_POSITION,
      (ActiveProbeName, apmldv)
         );
        }

        [ICommand]
        private void SetAngles(Vector3 angles)
        {
            _storeService.Store.Dispatch(
                SceneActions.SET_PROBE_ANGLES,
 (ActiveProbeName, angles, _pitchRange)
    );
        }

        [ICommand]
        private void LockProbe()
        {
            _storeService.Store.Dispatch(SceneActions.SET_PROBE_LOCKED, (ActiveProbeName, !Locked));
        }

        [ICommand]
        private void DuplicateProbe()
        {
            _storeService.Store.Dispatch(SceneActions.DUPLICATE_PROBE, ActiveProbeName);
        }

        [ICommand]
        private void MoveProbeToReferenceCoordinate()
        {
            _storeService.Store.Dispatch(
   SceneActions.SET_PROBE_POSITION,
            (ActiveProbeName, Vector3.zero)
            );
        }

        [ICommand]
        private void MoveProbeToDura()
        {
            ProbeManager
              .Instances.First(manager => manager.name == ActiveProbeName)
                .DropProbeToBrainSurface();
        }

        [ICommand]
        private void InspectVisualizingManipulator()
        {
            _storeService.Store.Dispatch(
     SceneActions.SET_ACTIVE_MANIPULATOR,
    VisualizingManipulatorId
            );
        }

        [ICommand]
        private void SetProbeColor(ProbeColor color)
        {
            _storeService.Store.Dispatch(SceneActions.SET_PROBE_COLOR, (ActiveProbeName, color));
        }

        #endregion
    }
}
