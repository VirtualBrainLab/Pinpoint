using Services;
using UI.ViewModels;
using UI.Views;
using Unity.AppUI.MVVM;

namespace UI
{
    /// <summary>
    /// Builds and configures the Pinpoint application using the UIToolkitAppBuilder.
    /// Responsible for registering services, view models, and views for dependency injection.
    /// </summary>
    public class PinpointAppBuilder : UIToolkitAppBuilder<PinpointApp>
    {
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
            builder.services.AddSingleton<EphysLinkService>();

            // ViewModels.
            builder.services.AddSingleton<MainViewModel>();
            builder.services.AddSingleton<SceneViewModel>();
            builder.services.AddSingleton<AtlasViewModel>();

            builder.services.AddSingleton<SettingsViewModel>();
            builder.services.AddSingleton<ProbeViewModel>();
            builder.services.AddSingleton<EphysLinkViewModel>();
            builder.services.AddSingleton<RigViewModel>();
            builder.services.AddSingleton<AtlasSettingsViewModel>();

            builder.services.AddSingleton<ProbeInspectorViewModel>();
            builder.services.AddSingleton<ManipulatorInspectorViewModel>();
            builder.services.AddSingleton<AutomationViewModel>();

            // Views.
            builder.services.AddSingleton<MainView>();
            builder.services.AddSingleton<SceneView>();
            builder.services.AddSingleton<AtlasView>();

            builder.services.AddSingleton<SettingsView>();
            builder.services.AddSingleton<ProbeView>();
            builder.services.AddSingleton<EphysLinkView>();
            builder.services.AddSingleton<RigView>();
            builder.services.AddSingleton<AtlasSettingsView>();

            builder.services.AddSingleton<ProbeInspectorView>();
            builder.services.AddSingleton<ManipulatorInspectorView>();
        }
    }
}
