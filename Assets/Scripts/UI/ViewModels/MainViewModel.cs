using UI.Models;
using UI.Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;

namespace UI.ViewModels
{
    public class MainViewModel: ObservableObject
    {
        #region Services

        private readonly ILocalStorageService _localStorageService;
        private readonly IStoreService _storeService;
        private readonly Unsubscriber _unsubscribe;
        private const string SLICE_NAME = "MainState";

        #endregion
        #region Commands

        public RelayCommand ToggleSidePanelLeftCommand { get; }

        #endregion

        #region Properties

        private bool _isSidePanelLeftOpen = true;

        public bool IsSidePanelLeftOpen
        {
            get => _isSidePanelLeftOpen;
            private set => SetProperty(ref _isSidePanelLeftOpen, value);
        }

        #endregion

        public MainViewModel(ILocalStorageService localStorageService, IStoreService storeService)
        {
            // Services.
            _localStorageService = localStorageService;
            _storeService = storeService;
            
            // Commands.
            ToggleSidePanelLeftCommand = new RelayCommand(ToggleSidePanelLeft);
            
            // State.
            var initialState = _localStorageService.GetValue(SLICE_NAME, new MainState());
        }

        #region Command Implementation

        private void ToggleSidePanelLeft()
        {
            IsSidePanelLeftOpen = !IsSidePanelLeftOpen;
        }

        #endregion
    }
}