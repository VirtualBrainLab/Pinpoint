using Models;
using Models.Scene;
using Models.Settings;
using Unity.AppUI.Redux;

namespace Services
{
    /// <summary>
    ///     Provides access to the application's Redux store implementation.
    ///     Handles initialization and persistence of application state using local storage.
    /// </summary>
    public class StoreService
    {
        private readonly LocalStorageService _localStorageService;

        /// <summary>
        ///     Gets the Redux store instance for partitioned application state.
        /// </summary>
        public IStore<PartitionedState> Store { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="StoreService" /> class.
        ///     Loads initial state from local storage and configures the Redux store.
        /// </summary>
        /// <param name="localStorageService">The local storage service for state persistence.</param>
        public StoreService(LocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;

            // Initialize state in memory.
            var initialMainState = _localStorageService.GetValue(
                SliceNames.MAIN_SLICE,
                new MainState()
            );
            var initialSceneState = _localStorageService.GetValue(
                SliceNames.SCENE_SLICE,
                new SceneState()
            );
            var initialSettingsState = _localStorageService.GetValue(
                SliceNames.SETTINGS_SLICE,
                new SettingsState()
            );

            // Initialize the Redux store.
            var mainSlice = StoreFactory.CreateSlice(
                SliceNames.MAIN_SLICE,
                initialMainState,
                builder =>
                {
                    builder
                        .AddCase(
                            MainActions.SET_MAIN_SPLIT_VIEW_STATE,
                            MainReducers.SetMainSplitViewStateReducer
                        )
                        .AddCase(
                            MainActions.SET_LEFT_SIDE_PANEL_TAB_INDEX,
                            MainReducers.SetLeftSidePanelTabIndexReducer
                        );
                }
            );
            var sceneSlice = StoreFactory.CreateSlice(
                SliceNames.SCENE_SLICE,
                initialSceneState,
                builder =>
                {
                    builder
                        // Probe list.
                        .AddCase(SceneActions.ADD_PROBE, SceneReducers.AddProbeReducer)
                        .AddCase(
                            SceneActions.ADD_VISUALIZATION_PROBE,
                            SceneReducers.AddVisualizationProbeReducer
                        )
                        .AddCase(SceneActions.DUPLICATE_PROBE, SceneReducers.DuplicateProbeReducer)
                        .AddCase(SceneActions.REMOVE_PROBE, SceneReducers.RemoveProbeReducer)
                        .AddCase(
                            SceneActions.REMOVE_ALL_VISUALIZATION_PROBES,
                            SceneReducers.RemoveAllVisualizationProbesReducer
                        )
                        // Manipulator list.
                        .AddCase(
                            SceneActions.SET_MANIPULATORS,
                            SceneReducers.SetManipulatorsReducer
                        )
                        // Active Item.
                        .AddCase(SceneActions.SET_ACTIVE_PROBE, SceneReducers.SetActiveProbeReducer)
                        .AddCase(
                            SceneActions.SET_ACTIVE_MANIPULATOR,
                            SceneReducers.SetActiveManipulatorReducer
                        )
                        // Probe.
                        .AddCase(
                            SceneActions.SET_PROBE_POSITION,
                            SceneReducers.SetProbePositionReducer
                        )
                        .AddCase(
                            SceneActions.SET_PROBE_POSITION_BY,
                            SceneReducers.SetProbePositionByReducer
                        )
                        .AddCase(SceneActions.SET_PROBE_ANGLES, SceneReducers.SetProbeAnglesReducer)
                        .AddCase(
                            SceneActions.SET_PROBE_POSITION_AND_ANGLES_BY,
                            SceneReducers.SetProbePositionAndAnglesByReducer
                        )
                        .AddCase(
                            SceneActions.BULK_SET_PROBE_POSITION_AND_ANGLES_BY,
                            SceneReducers.BulkSetProbePositionAndAnglesByReducer
                        )
                        .AddCase(
                            SceneActions.CHANGE_PROBE_POSITION_BY,
                            SceneReducers.ChangeProbePositionByReducer
                        )
                        .AddCase(
                            SceneActions.CHANGE_PROBE_ANGLES_BY,
                            SceneReducers.ChangeProbeAnglesByReducer
                        )
                        .AddCase(SceneActions.SET_PROBE_COLOR, SceneReducers.SetProbeColorReducer)
                        .AddCase(SceneActions.SET_PROBE_LOCKED, SceneReducers.SetProbeLockedReducer)
                        // Manipulator.
                        .AddCase(
                            SceneActions.SET_MANIPULATOR_ANGLES,
                            SceneReducers.SetManipulatorAnglesReducer
                        )
                        .AddCase(
                            SceneActions.SET_MANIPULATOR_HANDEDNESS,
                            SceneReducers.SetManipulatorHandednessReducer
                        )
                        .AddCase(
                            SceneActions.SET_MANIPULATOR_REFERENCE_COORDINATE_OFFSET,
                            SceneReducers.SetManipulatorReferenceCoordinateOffsetReducer
                        )
                        .AddCase(
                            SceneActions.SET_MANIPULATOR_DURA_OFFSET,
                            SceneReducers.SetManipulatorDuraOffsetReducer
                        )
                        .AddCase(
                            SceneActions.CHANGE_MANIPULATOR_DURA_OFFSET_BY,
                            SceneReducers.ChangeManipulatorDuraOffsetByReducer
                        )
                        .AddCase(
                            SceneActions.SET_MANIPULATOR_MANUAL_CONTROL_ENABLED,
                            SceneReducers.SetManipulatorManualControlEnabledReducer
                        )
                        // Automation.
                        .AddCase(
                            SceneActions.SET_SELECTED_TARGET_INSERTION_PROBE_NAME,
                            SceneReducers.SetSelectedTargetInsertionProbeNameReducer
                        )
                        .AddCase(
                            SceneActions.SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE,
                            SceneReducers.SetActiveProbeAutomationProgressStateReducer
                        )
                        .AddCase(
                            SceneActions.SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE_TO_NEXT_DRIVING,
                            SceneReducers.SetActiveProbeAutomationProgressStateToNextDrivingReducer
                        )
                        .AddCase(
                            SceneActions.SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE_TO_NEXT_EXITING,
                            SceneReducers.SetActiveProbeAutomationProgressStateToNextExitingReducer
                        )
                        .AddCase(
                            SceneActions.COMPLETE_ACTIVE_PROBE_AUTOMATION_INTERMEDIATE_PROGRESS,
                            SceneReducers.CompleteActiveProbeAutomationIntermediateProgressReducer
                        )
                        .AddCase(
                            SceneActions.CANCEL_ACTIVE_PROBE_AUTOMATION_INTERMEDIATE_PROGRESS,
                            SceneReducers.CancelActiveProbeAutomationIntermediateProgressReducer
                        )
                        .AddCase(
                            SceneActions.SET_ACTIVE_PROBE_REFERENCE_COORDINATE,
                            SceneReducers.SetActiveProbeReferenceCoordinateReducer
                        )
                        .AddCase(
                            SceneActions.SET_ACTIVE_PROBE_DURA_OFFSET,
                            SceneReducers.SetActiveProbeDuraOffsetReducer
                        )
                        .AddCase(
                            SceneActions.SET_ACTIVE_PROBE_INSERTION_BASE_SPEED,
                            SceneReducers.SetActiveProbeTargetInsertionBaseSpeedReducer
                        )
                        .AddCase(
                            SceneActions.SET_ACTIVE_PROBE_DRIVE_PAST_DISTANCE,
                            SceneReducers.SetActiveProbeDrivePastDistanceReducer
                        )
                        .AddCase(
                            SceneActions.INITIALIZE_AREA_VISIBILITY,
                            SceneReducers.InitializeAreaVisibilityReducer
                        )
                        .AddCase(
                            SceneActions.ROTATE_AREA_VISIBILITY,
                            SceneReducers.RotateAreaVisibilityReducer
                        )
                        // Platform Info.
                        .AddCase(
                            SceneActions.SET_PLATFORM_INFO,
                            SceneReducers.SetPlatformInfoReducer
                        );
                }
            );
            var settingsSlice = StoreFactory.CreateSlice(
                SliceNames.SETTINGS_SLICE,
                initialSettingsState,
                builder =>
                {
                    builder
                        .AddCase(SettingsActions.SET_TAB_INDEX, SettingsReducers.SetTabIndexReducer)
                        // Probe Settings.
                        .AddCase(
                            SettingsActions.SET_DETECT_COLLISIONS,
                            SettingsReducers.SetDetectCollisionsReducer
                        )
                        // Ephys Link.
                        .AddCase(
                            SettingsActions.SET_SELECTED_EPHYS_LINK_PLATFORM_TYPE,
                            SettingsReducers.SetSelectedEphysLinkPlatformTypeReducer
                        )
                        .AddCase(
                            SettingsActions.SET_NEW_SCALE_PATHFINDER_MPM_PORT,
                            SettingsReducers.SetNewScalePathfinderMpmPortReducer
                        )
                        .AddCase(
                            SettingsActions.SET_CUSTOM_SERVER_IP_ADDRESS,
                            SettingsReducers.SetCustomServerIpAddressReducer
                        )
                        .AddCase(
                            SettingsActions.SET_CUSTOM_SERVER_PORT,
                            SettingsReducers.SetCustomServerPortReducer
                        )
                        .AddCase(
                            SettingsActions.SET_EPHYS_LINK_CONNECTION_STATE,
                            SettingsReducers.SetEphysLinkConnectionStateReducer
                        );
                }
            );
            Store = StoreFactory.CreateStore(
                new ISlice<PartitionedState>[] { mainSlice, sceneSlice, settingsSlice }
            );
        }

        /// <summary>
        ///     Saves the chosen slices to local storage.
        /// </summary>
        public void Save()
        {
            // Main state.
            _localStorageService.SetValue(
                SliceNames.MAIN_SLICE,
                Store.GetState<MainState>(SliceNames.MAIN_SLICE)
            );

            // Scene state.
            _localStorageService.SetValue(
                SliceNames.SCENE_SLICE,
                Store.GetState<SceneState>(SliceNames.SCENE_SLICE)
            );

            // Settings state.
            _localStorageService.SetValue(
                SliceNames.SETTINGS_SLICE,
                Store.GetState<SettingsState>(SliceNames.SETTINGS_SLICE)
            );
        }
    }
}
