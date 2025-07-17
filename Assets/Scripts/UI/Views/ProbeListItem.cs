using System.ComponentModel;
using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;

namespace UI.Views
{
    public class ProbeListItem
    {
        #region Component References

        private readonly Icon _icon;
        private readonly ActionButton _hideActionButton;
        private readonly Button _deleteButton;

        #endregion

        public ProbeListItem(VisualElement root, ProbeListItemViewModel probeListItemViewModel)
        {
            // Attach view model and register property changes.
            root.dataSource = probeListItemViewModel;

            // Register component references.
            _icon = root.Q<Icon>();
            _hideActionButton = root.Q<ActionButton>();
            _deleteButton = root.Q<Button>();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ProbeListItemViewModel.Color):
                    break;
            }
        }
    }
}
