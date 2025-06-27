using UI.Models;
using UI.Utils;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace UI.Services
{
    /// <summary>
    /// Provides access to the application's Redux store implementation.
    /// </summary>
    public class StoreService : IStoreService
    {
        private readonly ILocalStorageService _localStorageService;

        public IStore<PartitionedState> Store { get; }

        public StoreService(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;

            // Initialize state in memory.
            var initialMainState = _localStorageService.GetValue(
                SliceNames.MAIN_SLICE,
                new MainState()
            );

            // Initialize the Redux store.
            Store = StoreFactory.CreateStore(
                new[]
                {
                    StoreFactory.CreateSlice(
                        SliceNames.MAIN_SLICE,
                        initialMainState,
                        builder =>
                        {
                            builder
                                .AddCase(
                                    MainActions.TOGGLE_SIDE_PANEL_LEFT,
                                    MainReducers.ToggleSidePanelLeftReducer
                                )
                                .AddCase(
                                    MainActions.TOGGLE_SIDE_PANEL_RIGHT,
                                    MainReducers.ToggleSidePanelRightReducer
                                );
                        }
                    ),
                }
            );
        }

        public void Save()
        {
            // Get the current state from memory.
            var currentMainState = Store.GetState<MainState>(SliceNames.MAIN_SLICE);
            
            // Save the current state to local storage.
            _localStorageService.SetValue(SliceNames.MAIN_SLICE, currentMainState);
        }
    }
}
