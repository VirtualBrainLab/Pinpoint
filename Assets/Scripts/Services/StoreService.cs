using Models;
using Models.Scene;
using Unity.AppUI.Redux;

namespace Services
{
    /// <summary>
    /// Provides access to the application's Redux store implementation.
    /// Handles initialization and persistence of application state using local storage.
    /// </summary>
    public class StoreService
    {
        private readonly LocalStorageService _localStorageService;

        /// <summary>
        /// Gets the Redux store instance for partitioned application state.
        /// </summary>
        public IStore<PartitionedState> Store { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreService"/> class.
        /// Loads initial state from local storage and configures the Redux store.
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

            // Initialize the Redux store.
            var mainSlice = StoreFactory.CreateSlice(
                SliceNames.MAIN_SLICE,
                initialMainState,
                builder =>
                {
                    builder
                        .AddCase(MainActions.SET_MODE, MainReducers.SetModeReducer)
                        .AddCase(
                            MainActions.TOGGLE_LEFT_SIDE_PANEL,
                            MainReducers.ToggleLeftSidePanelReducer
                        )
                        .AddCase(
                            MainActions.TOGGLE_RIGHT_SIDE_PANEL,
                            MainReducers.ToggleRightSidePanelReducer
                        );
                }
            );
            var automationSlice = StoreFactory.CreateSlice(
                SliceNames.SCENE_SLICE,
                initialSceneState,
                builder =>
                {
                    builder
                        .AddCase(SceneActions.ADD_PROBE, SceneReducers.AddProbeReducer)
                        .AddCase(SceneActions.REMOVE_PROBE, SceneReducers.RemoveProbeReducer)
                        .AddCase(
                            SceneActions.REMOVE_ALL_PROBES,
                            SceneReducers.RemoveAllProbesReducer
                        )
                        .AddCase(
                            SceneActions.SET_ACTIVE_PROBE_UUID,
                            SceneReducers.SetActiveProbeUUIDReducer
                        )
                        .AddCase(
                            SceneActions.SET_SELECTED_TARGET_INSERTION_PROBE_UUID,
                            SceneReducers.SetSelectedTargetInsertionProbeUUIDReducer
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
                            SceneActions.SET_ACTIVE_PROBE_REFERENCE_COORDINATE,
                            SceneReducers.SetActiveProbeReferenceCoordinateReducer
                        )
                        .AddCase(
                            SceneActions.SET_ACTIVE_PROBE_DURA_OFFSET,
                            SceneReducers.SetActiveProbeDuraOffsetReducer
                        );
                }
            );
            Store = StoreFactory.CreateStore(
                new ISlice<PartitionedState>[] { mainSlice, automationSlice }
            );
        }

        /// <summary>
        /// Saves the chosen slices to local storage.
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
        }
    }
}
