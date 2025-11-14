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
            var initialRigState = _localStorageService.GetValue(
                SliceNames.RIG_SLICE,
                new RigState()
            );
            var initialAtlasSettingsState = _localStorageService.GetValue(
                SliceNames.ATLAS_SETTINGS_SLICE,
                new AtlasSettingsState()
            );

            // Initialize the Redux store.
            var mainSlice = StoreFactory.CreateSlice(
                SliceNames.MAIN_SLICE,
                initialMainState,
                builder =>
                {
                    builder
                        .AddCase(
                            MainActions.SET_IS_AUTOMATION_ENABLED,
                            MainReducers.SetIsAutomationEnabledReducer
                        )
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
                        .AddCase(
                            SceneActions.SET_ALL_PROBES_TO_LINE,
                            SceneReducers.SetAllProbesToLineReducer
                        )
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
                        .AddCase(SceneActions.SET_DURA_OFFSET, SceneReducers.SetDuraOffsetReducer)
                        .AddCase(
                            SceneActions.RESET_DURA_OFFSET,
                            SceneReducers.ResetDuraOffsetReducer
                        )
                        .AddCase(
                            SceneActions.SET_MANIPULATOR_MANUAL_CONTROL_ENABLED,
                            SceneReducers.SetManipulatorManualControlEnabledReducer
                        )
                        .AddCase(
                            SceneActions.SET_MANIPULATOR_DEMO_HOME_COORDINATE,
                            SceneReducers.SetManipulatorDemoHomeCoordinateReducer
                        )
                        .AddCase(
                            SceneActions.SET_MANIPULATOR_DEMO_TARGET_COORDINATE,
                            SceneReducers.SetManipulatorDemoTargetCoordinateReducer
                        )
                        .AddCase(
                            SceneActions.SET_MANIPULATOR_DEMO_RUNNING,
                            SceneReducers.SetManipulatorDemoRunningReducer
                        )
                        // Automation.
                        .AddCase(
                            SceneActions.SET_TARGET_INSERTION_PROBE_NAME,
                            SceneReducers.SetTargetInsertionProbeNameReducer
                        )
                        .AddCase(
                            SceneActions.SET_AUTOMATION_PROGRESS_STATE,
                            SceneReducers.SetAutomationProgressStateReducer
                        )
                        .AddCase(
                            SceneActions.SET_AUTOMATION_PROGRESS_STATE_TO_NEXT_DRIVING,
                            SceneReducers.SetAutomationProgressStateToNextDrivingReducer
                        )
                        .AddCase(
                            SceneActions.SET_AUTOMATION_PROGRESS_STATE_TO_NEXT_EXITING,
                            SceneReducers.SetAutomationProgressStateToNextExitingReducer
                        )
                        .AddCase(
                            SceneActions.COMPLETE_AUTOMATION_INTERMEDIATE_PROGRESS,
                            SceneReducers.CompleteAutomationIntermediateProgressReducer
                        )
                        .AddCase(
                            SceneActions.CANCEL_AUTOMATION_INTERMEDIATE_PROGRESS,
                            SceneReducers.CancelAutomationIntermediateProgressReducer
                        )
                        .AddCase(
                            SceneActions.SET_INSERTION_SPEED,
                            SceneReducers.SetInsertionSpeedReducer
                        )
                        .AddCase(
                            SceneActions.SET_DRIVE_PAST_DISTANCE,
                            SceneReducers.SetDrivePastDistanceReducer
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
                        .AddCase(
                            SettingsActions.SET_CONVERT_APML2PROBE,
                            SettingsReducers.SetConvertAPML2ProbeReducer
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
            var rigSlice = StoreFactory.CreateSlice(
                SliceNames.RIG_SLICE,
                initialRigState,
                builder =>
                {
                    builder
                        .AddCase(RigActions.TOGGLE_WELL, RigReducers.ToggleWellReducer)
                        .AddCase(
                            RigActions.TOGGLE_RIG_WIDEFIELD,
                            RigReducers.ToggleRigWidefieldReducer
                        )
                        .AddCase(RigActions.TOGGLE_MOUSE_SKULL, RigReducers.ToggleMouseSkullReducer)
                        .AddCase(RigActions.TOGGLE_RAT_SKULL, RigReducers.ToggleRatSkullReducer)
                        .AddCase(RigActions.TOGGLE_IBL_CENTER, RigReducers.ToggleIblCenterReducer)
                        .AddCase(RigActions.TOGGLE_IBL_FRONT, RigReducers.ToggleIblFrontReducer)
                        .AddCase(RigActions.TOGGLE_IBL_BACK, RigReducers.ToggleIblBackReducer)
                        .AddCase(RigActions.TOGGLE_UCLA, RigReducers.ToggleUclaReducer);
                }
            );
            var atlasSettingsSlice = StoreFactory.CreateSlice(
                SliceNames.ATLAS_SETTINGS_SLICE,
                initialAtlasSettingsState,
                builder =>
                {
                    builder
                        .AddCase(
                            AtlasSettingsActions.SET_ATLAS_NAME,
                            AtlasSettingsReducers.SetAtlasNameReducer
                        )
                        .AddCase(
                            AtlasSettingsActions.SET_ATLAS_TRANSFORM_NAME,
                            AtlasSettingsReducers.SetAtlasTransformNameReducer
                        )
                        .AddCase(
                            AtlasSettingsActions.SET_REFERENCE_COORD,
                            AtlasSettingsReducers.SetReferenceCoordReducer
                        )
                        .AddCase(
                            AtlasSettingsActions.TOGGLE_SHOW_3D_SLICES,
                            AtlasSettingsReducers.ToggleShow3DSlicesReducer
                        );
                }
            );
            Store = StoreFactory.CreateStore(
                new ISlice<PartitionedState>[]
                {
                    mainSlice,
                    sceneSlice,
                    settingsSlice,
                    rigSlice,
                    atlasSettingsSlice,
                }
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

            // Rig state.
            _localStorageService.SetValue(
                SliceNames.RIG_SLICE,
                Store.GetState<RigState>(SliceNames.RIG_SLICE)
            );

            // Atlas settings state.
            _localStorageService.SetValue(
                SliceNames.ATLAS_SETTINGS_SLICE,
                Store.GetState<AtlasSettingsState>(SliceNames.ATLAS_SETTINGS_SLICE)
            );
        }
    }
}
