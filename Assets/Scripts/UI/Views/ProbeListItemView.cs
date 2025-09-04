using System;
using System.ComponentModel;
using System.Linq;
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

        #endregion

        #region Properties

        private readonly ProbeListItemViewModel _probeListItemViewModel;

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
            var deleteButton = root.Q<Button>();

            // Register component events.
            deleteButton.clickable.clicked += _probeListItemViewModel.RemoveProbeCommand.Execute;

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
            // Remove existing color classes.
            foreach (
                var className in _icon
                    .GetClasses()
                    .ToList()
                    .Where(className => className.StartsWith("probe-icon--"))
            )
                _icon.RemoveFromClassList(className);

            // Compute the new color class.
            var newClass = _probeListItemViewModel.Color switch
            {
                ProbeColor.DarkBlue => "probe-icon--dark-blue",
                ProbeColor.LightBlue => "probe-icon--light-blue",
                ProbeColor.DarkOrange => "probe-icon--dark-orange",
                ProbeColor.LightOrange => "probe-icon--light-orange",
                ProbeColor.DarkGreen => "probe-icon--dark-green",
                ProbeColor.LightGreen => "probe-icon--light-green",
                ProbeColor.DarkRed => "probe-icon--dark-red",
                ProbeColor.LightRed => "probe-icon--light-red",
                ProbeColor.DarkPurple => "probe-icon--dark-purple",
                ProbeColor.LightPurple => "probe-icon--light-purple",
                ProbeColor.DarkBrown => "probe-icon--dark-brown",
                ProbeColor.LightBrown => "probe-icon--light-brown",
                ProbeColor.DarkPink => "probe-icon--dark-pink",
                ProbeColor.LightPink => "probe-icon--light-pink",
                ProbeColor.DarkGray => "probe-icon--dark-gray",
                ProbeColor.LightGray => "probe-icon--light-gray",
                ProbeColor.LivelyLaugh => "probe-icon--lively-laugh",
                ProbeColor.DarkCyan => "probe-icon--dark-cyan",
                ProbeColor.LightCyan => "probe-icon--light-cyan",
                _ => null,
            };
            if (string.IsNullOrEmpty(newClass))
                return;

            // Apply.
            _icon.AddToClassList(newClass);
        }
    }
}
