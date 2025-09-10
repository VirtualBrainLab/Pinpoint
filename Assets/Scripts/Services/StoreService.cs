using Models;
using Models.Automation;
using Models.Scene;
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
            var initialEphysLinkState = _localStorageService.GetValue(
                SliceNames.EPHYS_LINK_SLICE,
                new EphysLinkState()
            );

            // Initialize the Redux store.
            var mainSlice = StoreFactory.CreateSlice(
                SliceNames.MAIN_SLICE,
                initialMainState,
                builder =>
                {
                    builder
                        .AddCase(
                            MainActions.SET_IS_AUTOMATION_MODE_ACTIVE,
                            MainReducers.SetIsAutomationModeActiveReducer
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
            var ephysLinkSlice = StoreFactory.CreateSlice(
                SliceNames.EPHYS_LINK_SLICE,
                initialEphysLinkState,
                builder =>
                {
                    builder.AddCase(
                        EphysLinkActions.SET_SELECTED_EPHYS_LINK_PLATFORM_TYPE,
                        EphysLinkReducers.SetSelectedEphysLinkPlatformTypeReducer
                    );
                    builder.AddCase(
                        EphysLinkActions.SET_NEW_SCALE_PATHFINDER_MPM_PORT,
                        EphysLinkReducers.SetNewScalePathfinderMpmPortReducer
                    );
                    builder.AddCase(
                        EphysLinkActions.SET_CUSTOM_SERVER_IP_ADDRESS,
                        EphysLinkReducers.SetCustomServerIpAddressReducer
                    );
                    builder.AddCase(
                        EphysLinkActions.SET_CUSTOM_SERVER_PORT,
                        EphysLinkReducers.SetCustomServerPortReducer
                    );
                    builder.AddCase(
                        EphysLinkActions.SET_CONNECTION_STATE,
                        EphysLinkReducers.SetConnectionStateReducer
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
                        .AddCase(SceneActions.DUPLICATE_PROBE, SceneReducers.DuplicateProbeReducer)
                        .AddCase(SceneActions.REMOVE_PROBE, SceneReducers.RemoveProbeReducer)
                        .AddCase(
                            SceneActions.REMOVE_ALL_PROBES,
                            SceneReducers.RemoveAllProbesReducer
                        )
                        // Active Probe.
                        .AddCase(SceneActions.SET_ACTIVE_PROBE, SceneReducers.SetActiveProbeReducer)
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
                            SceneActions.CHANGE_PROBE_POSITION_BY,
                            SceneReducers.ChangeProbePositionByReducer
                        )
                        .AddCase(
                            SceneActions.CHANGE_PROBE_ANGLES_BY,
                            SceneReducers.ChangeProbeAnglesByReducer
                        )
                        .AddCase(SceneActions.SET_PROBE_COLOR, SceneReducers.SetProbeColorReducer)
                        .AddCase(SceneActions.SET_PROBE_LOCKED, SceneReducers.SetProbeLockedReducer)
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
                            SceneActions.ROTATE_AREA_VISIBILITY,
                            SceneReducers.RotateAreaVisibilityReducer
                        );
                }
            );
            Store = StoreFactory.CreateStore(
                new ISlice<PartitionedState>[] { mainSlice, ephysLinkSlice, sceneSlice }
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

            // Ephys link state.
            _localStorageService.SetValue(
                SliceNames.EPHYS_LINK_SLICE,
                Store.GetState<EphysLinkState>(SliceNames.EPHYS_LINK_SLICE)
            );
        }
    }
}
