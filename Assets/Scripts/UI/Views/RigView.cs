using Models;
using Models.Settings;
using Services;
using UI;
using UI.ViewModels;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class RigView
    {
        #region Component References

        private readonly VisualElement _wellElement;
        private readonly VisualElement _rigWidefieldElement;
        private readonly VisualElement _mouseSkullElement;
        private readonly VisualElement _ratSkullElement;
        private readonly VisualElement _iblCenterElement;
        private readonly VisualElement _iblFrontElement;
        private readonly VisualElement _iblBackElement;
        private readonly VisualElement _uclaElement;

        #endregion

        private readonly RigViewModel _rigViewModel;
        private readonly IDisposableSubscription _rigStateSubscription;
        private readonly StoreService _storeService;

        public RigView(RigViewModel rigViewModel, StoreService storeService)
        {
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>("rig-settings-view");
            _rigViewModel = rigViewModel;
            _storeService = storeService;
            root.dataSource = _rigViewModel;

            _wellElement = root.Q<VisualElement>("well");
            _rigWidefieldElement = root.Q<VisualElement>("rig-widefield");
            _mouseSkullElement = root.Q<VisualElement>("mouse-skull");
            _ratSkullElement = root.Q<VisualElement>("rat-skull");
            _iblCenterElement = root.Q<VisualElement>("ibl-center");
            _iblFrontElement = root.Q<VisualElement>("ibl-front");
            _iblBackElement = root.Q<VisualElement>("ibl-back");
            _uclaElement = root.Q<VisualElement>("ucla");

            RegisterClickHandlers();

            _rigStateSubscription = _storeService.Store.Subscribe(
              state => state.Get<RigState>(SliceNames.RIG_SLICE),
          OnRigStateChanged,
          new SubscribeOptions<RigState> { fireImmediately = true }
                );

            App.shuttingDown += OnShuttingDown;
        }

        private void RegisterClickHandlers()
        {
            _wellElement.RegisterCallback<ClickEvent>(_ => _storeService.Store.Dispatch(RigActions.TOGGLE_WELL, true));
            _rigWidefieldElement.RegisterCallback<ClickEvent>(_ => _storeService.Store.Dispatch(RigActions.TOGGLE_RIG_WIDEFIELD, true));
            _mouseSkullElement.RegisterCallback<ClickEvent>(_ => _storeService.Store.Dispatch(RigActions.TOGGLE_MOUSE_SKULL, true));
            _ratSkullElement.RegisterCallback<ClickEvent>(_ => _storeService.Store.Dispatch(RigActions.TOGGLE_RAT_SKULL, true));
            _iblCenterElement.RegisterCallback<ClickEvent>(_ => _storeService.Store.Dispatch(RigActions.TOGGLE_IBL_CENTER, true));
            _iblFrontElement.RegisterCallback<ClickEvent>(_ => _storeService.Store.Dispatch(RigActions.TOGGLE_IBL_FRONT, true));
            _iblBackElement.RegisterCallback<ClickEvent>(_ => _storeService.Store.Dispatch(RigActions.TOGGLE_IBL_BACK, true));
            _uclaElement.RegisterCallback<ClickEvent>(_ => _storeService.Store.Dispatch(RigActions.TOGGLE_UCLA, true));
        }

        private void OnRigStateChanged(RigState state)
        {
            UpdateVisualState(_wellElement, state.WellVisible);
            UpdateVisualState(_rigWidefieldElement, state.RigWidefieldVisible);
            UpdateVisualState(_mouseSkullElement, state.MouseSkullVisible);
            UpdateVisualState(_ratSkullElement, state.RatSkullVisible);
            UpdateVisualState(_iblCenterElement, state.IblCenterVisible);
            UpdateVisualState(_iblFrontElement, state.IblFrontVisible);
            UpdateVisualState(_iblBackElement, state.IblBackVisible);
            UpdateVisualState(_uclaElement, state.UclaVisible);
        }

        private void UpdateVisualState(VisualElement element, bool isActive)
        {
            if (element == null) return;

            if (isActive)
            {
                element.style.opacity = 1.0f;
                element.style.borderBottomWidth = 3;
                element.style.borderTopWidth = 3;
                element.style.borderLeftWidth = 3;
                element.style.borderRightWidth = 3;
                element.style.borderBottomColor = new StyleColor(new Color(0.2f, 0.8f, 0.2f));
                element.style.borderTopColor = new StyleColor(new Color(0.2f, 0.8f, 0.2f));
                element.style.borderLeftColor = new StyleColor(new Color(0.2f, 0.8f, 0.2f));
                element.style.borderRightColor = new StyleColor(new Color(0.2f, 0.8f, 0.2f));
            }
            else
            {
                element.style.opacity = 0.5f;
                element.style.borderBottomWidth = 0;
                element.style.borderTopWidth = 0;
                element.style.borderLeftWidth = 0;
                element.style.borderRightWidth = 0;
            }
        }

        private void OnShuttingDown()
        {
            _rigStateSubscription?.Dispose();
            App.shuttingDown -= OnShuttingDown;
        }
    }
}
