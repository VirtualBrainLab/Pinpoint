using System;
using System.ComponentModel;
using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using Utils.Types;
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

        #region Properties

        private readonly ProbeListItemViewModel _probeListItemViewModel;

        private string _lastAppliedClass;

        #endregion

        public ProbeListItem(VisualElement root, ProbeListItemViewModel probeListItemViewModel)
        {
            // Attach view model and register property changes.
            _probeListItemViewModel = probeListItemViewModel;
            _probeListItemViewModel.PropertyChanged += OnPropertyChanged;
            root.dataSource = _probeListItemViewModel;

            // Register component references.
            _icon = root.Q<Icon>();
            _hideActionButton = root.Q<ActionButton>();
            _deleteButton = root.Q<Button>();

            // Initialize components.
            UpdateColor();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ProbeListItemViewModel.Color):
                    UpdateColor();
                    break;
            }
        }

        private void UpdateColor()
        {
            _icon.RemoveFromClassList(_lastAppliedClass);
            var newClass = _probeListItemViewModel.Color switch
            {
                PinpointColor.DarkBlue => "icon--dark-blue",
                PinpointColor.LightBlue => "icon--light-blue",
                PinpointColor.DarkOrange => "icon--dark-orange",
                PinpointColor.LightOrange => "icon--light-orange",
                PinpointColor.DarkGreen => "icon--dark-green",
                PinpointColor.LightGreen => "icon--light-green",
                PinpointColor.DarkRed => "icon--dark-red",
                PinpointColor.LightRed => "icon--light-red",
                PinpointColor.DarkPurple => "icon--dark-purple",
                PinpointColor.LightPurple => "icon--light-purple",
                PinpointColor.DarkBrown => "icon--dark-brown",
                PinpointColor.LightBrown => "icon--light-brown",
                PinpointColor.DarkPink => "icon--dark-pink",
                PinpointColor.LightPink => "icon--light-pink",
                PinpointColor.DarkGray => "icon--dark-gray",
                PinpointColor.LightGray => "icon--light-gray",
                PinpointColor.DarkCyan => "icon--dark-cyan",
                PinpointColor.LightCyan => "icon--light-cyan",
                _ => null
            };
            if (string.IsNullOrEmpty(newClass)) return;
            _icon.AddToClassList(newClass);
            _lastAppliedClass = newClass;
        }
    }
}
