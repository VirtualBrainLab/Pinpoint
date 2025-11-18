using UI.ViewModels;
using Unity.AppUI.MVVM;
using Unity.Properties;
using UnityEngine.UIElements;

#if APP_UI
namespace UI.Views
{
    public class ChannelMapView
    {
        private readonly ListView _textItemsList;
        private readonly ChannelMapViewModel _channelMapViewModel;

        public ChannelMapView(ChannelMapViewModel channelMapViewModel)
        {
            _channelMapViewModel = channelMapViewModel;

            var root = PinpointApp.RootVisualElement.Q("text-parent");
            if (root == null)
            {
                UnityEngine.Debug.LogError("Could not find text-parent element in ChannelMapView");
                return;
            }

            root.dataSource = channelMapViewModel;

            _textItemsList = root.Q<ListView>("text-items-list");
            if (_textItemsList == null)
            {
                UnityEngine.Debug.LogError("Could not find text-items-list ListView in ChannelMapView");
                return;
            }

            _textItemsList.makeItem = MakeItem;
            _textItemsList.bindItem = BindItem;
        }

        private VisualElement MakeItem()
        {
            var container = new VisualElement();
            container.style.position = Position.Absolute;
            container.style.width = new StyleLength(new Length(100, LengthUnit.Percent));

            var label = new Unity.AppUI.UI.Text();
            label.name = "area-label";
            label.style.flexShrink = 0;

            container.Add(label);

            return container;
        }

        private void BindItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _channelMapViewModel.TextItems.Count)
                return;

            var textItem = _channelMapViewModel.TextItems[index];
            element.dataSource = textItem;

            var label = element.Q<Unity.AppUI.UI.Text>("area-label");
            if (label != null)
            {
                label.SetBinding("text", new DataBinding
                {
                    dataSourcePath = new PropertyPath("Name"),
                    bindingMode = BindingMode.ToTarget
                });
            }

            element.style.bottom = new StyleLength(new Length(textItem.PositionPerc * 100, LengthUnit.Percent));
        }
    }
}
#endif
