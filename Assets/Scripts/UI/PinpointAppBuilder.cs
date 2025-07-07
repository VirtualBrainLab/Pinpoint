using Services;
using UI.ViewModels;
using UI.Views;
using Unity.AppUI.MVVM;
using UnityEngine.UIElements;

namespace UI
{
    /// <summary>
    /// Builds and configures the Pinpoint application using the UIToolkitAppBuilder.
    /// Responsible for registering services, view models, and views for dependency injection.
    /// </summary>
    public class PinpointAppBuilder : UIToolkitAppBuilder<PinpointApp>
    {
        /// <summary>
        /// The main UI document asset for the application.
        /// </summary>
        public VisualTreeAsset MainUIDocument;

        /// <summary>
        /// Gets the singleton instance of the <see cref="PinpointAppBuilder"/>.
        /// </summary>
        internal static PinpointAppBuilder Instance { get; private set; }

        /// <summary>
        /// Configures the application by registering services, view models, and views.
        /// </summary>
        /// <param name="builder">The application builder used for configuration.</param>
        protected override void OnConfiguringApp(AppBuilder builder)
        {
            base.OnConfiguringApp(builder);

            // Singleton instance.
            Instance = this;

            // Services.
            builder.services.AddSingleton<LocalStorageService>();
            builder.services.AddSingleton<StoreService>();
            builder.services.AddSingleton<ProbeService>();
            builder.services.AddSingleton<AtlasService>();

            // ViewModels.
            builder.services.AddSingleton<MainViewModel>();
            builder.services.AddSingleton<AutomationViewModel>();
            builder.services.AddSingleton<AtlasViewModel>();

            // Views.
            builder.services.AddSingleton<MainView>();
        }
    }
}
