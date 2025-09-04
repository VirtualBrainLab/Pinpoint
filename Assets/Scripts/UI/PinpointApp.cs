using System.Linq;
using Services;
using UI.Views;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

namespace UI
{
    /// <summary>
    /// Represents the main application class for the Pinpoint UI.
    /// Inherits from <see cref="App"/> and is responsible for initializing the main view.
    /// </summary>
    public class PinpointApp : App
    {
        /// <summary>
        /// Gets the current instance of <see cref="PinpointApp"/>.
        /// </summary>
        public static PinpointApp Current => (PinpointApp)current;

        #region Static Service Accessors

        public static IStore<PartitionedState> StoreServiceStore =>
            Current.services.GetRequiredService<StoreService>().Store;

        public static AtlasService AtlasService =>
            Current.services.GetRequiredService<AtlasService>();
        public static MainView MainView => Current.services.GetRequiredService<MainView>();

        #endregion

        /// <summary>
        /// Initializes the application components and adds the <see cref="MainView"/> to the root visual element.
        /// </summary>
        public override void InitializeComponent()
        {
            base.InitializeComponent();

            // Set the root to the panel in the UI document to get the correct hierarchy.
            rootVisualElement = PinpointAppBuilder
                .Instance.uiDocument.rootVisualElement.Children()
                .First();

            // Instantiate the main view.
            services.GetRequiredService<MainView>();
        }
    }
}
