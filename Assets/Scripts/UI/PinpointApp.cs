using UI.Views;
using Unity.AppUI.MVVM;

namespace UI
{
    public class PinpointApp: App
    {
        public PinpointApp(MainView mainView)
        {
            mainPage = mainView;
        }
    }
}