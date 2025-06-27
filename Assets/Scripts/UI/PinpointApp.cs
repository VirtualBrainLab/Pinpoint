using UI.Views;
using Unity.AppUI.MVVM;

namespace UI
{
    /// <summary>
    /// Represents the main application class for the Pinpoint UI.
    /// Inherits from <see cref="App"/> and is responsible for initializing the main view.
    /// </summary>
    public class PinpointApp: App
    {
        /// <summary>
        /// Gets the current instance of <see cref="PinpointApp"/>.
        /// </summary>
        public static PinpointApp Current => (PinpointApp)current;

        /// <summary>
        /// Initializes the application components and adds the <see cref="MainView"/> to the root visual element.
        /// </summary>
        public override void InitializeComponent()
        {
            base.InitializeComponent();
            rootVisualElement.Add(services.GetRequiredService<MainView>());
        }
    }
}