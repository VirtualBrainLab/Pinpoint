using UI.Models;
using UI.Models.Scene;
using UI.Utils;
using Unity.AppUI.Redux;

namespace Services
{
    /// <summary>
    /// Provides access to the application's Redux store implementation.
    /// Handles initialization and persistence of application state using local storage.
    /// </summary>
    public class StoreService : IStoreService
    {
        private readonly ILocalStorageService _localStorageService;

        /// <summary>
        /// Gets the Redux store instance for partitioned application state.
        /// </summary>
        public IStore<PartitionedState> Store { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreService"/> class.
        /// Loads initial state from local storage and configures the Redux store.
        /// </summary>
        /// <param name="localStorageService">The local storage service for state persistence.</param>
        public StoreService(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;

            // Initialize state in memory.
            var initialMainState = _localStorageService.GetValue(
                SliceNames.MAIN_SLICE,
                new MainState()
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
                new SceneState(),
                builder =>
                {
                    builder
                        .AddCase(SceneActions.ADD_PROBE, SceneReducers.AddProbeReducer)
                        .AddCase(SceneActions.REMOVE_PROBE, SceneReducers.RemoveProbeReducer)
                        .AddCase(
                            SceneActions.SET_ACTIVE_PROBE_INDEX,
                            SceneReducers.SetActiveProbeIndexReducer
                        )
                        .AddCase(
                            SceneActions.SET_SELECTED_TARGET_INSERTION_PROBE_STATE,
                            SceneReducers.SetSelectedTargetInsertionReducer
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
        }
    }
}
