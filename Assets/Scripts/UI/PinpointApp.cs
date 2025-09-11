using System;
using System.Linq;
using Services;
using UI.Views;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine.UIElements;

namespace UI
{
    /// <summary>
    /// Represents the main application class for the Pinpoint UI.
    /// Inherits from <see cref="App"/> and is responsible for initializing the main view.
    /// </summary>
    public class PinpointApp : App
    {
        #region Static Accessors

        public static IServiceProvider Services => current.services;

        public static VisualElement RootVisualElement => current.rootVisualElement;

        public static IStore<PartitionedState> StoreServiceStore =>
            Services.GetRequiredService<StoreService>().Store;

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
            _ = services.GetRequiredService<MainView>();
        }
    }
}
