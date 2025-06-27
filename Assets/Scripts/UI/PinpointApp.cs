using UI.Views;
using Unity.AppUI.MVVM;

namespace UI
{
    public class PinpointApp: App
    {
        public static PinpointApp Current => (PinpointApp)current;

        public override void InitializeComponent()
        {
            base.InitializeComponent();
            rootVisualElement.Add(services.GetRequiredService<MainView>());
        }
    }
}