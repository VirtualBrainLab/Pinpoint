using UI.Services;
using UI.ViewModels;
using UI.Views;
using Unity.AppUI.MVVM;
using UnityEngine.UIElements;

namespace UI
{
    public class PinpointAppBuilder: UIToolkitAppBuilder<PinpointApp>
    {
        public VisualTreeAsset MainUIDocument;
        
        internal static PinpointAppBuilder Instance { get; private set; }

        protected override void OnConfiguringApp(AppBuilder builder)
        {
            base.OnConfiguringApp(builder);
            
            // Singleton instance.
            Instance = this;
            
            // Services.
            builder.services.AddSingleton<ILocalStorageService, LocalStorageService>();
            builder.services.AddSingleton<IStoreService, StoreService>();
            
            // ViewModels.
            builder.services.AddTransient<MainViewModel>();
            
            // Views.
            builder.services.AddTransient<MainView>();
        }
    }
}