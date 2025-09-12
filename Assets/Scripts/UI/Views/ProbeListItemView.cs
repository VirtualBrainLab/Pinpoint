using System.ComponentModel;
using System.Linq;
using UI.ViewModels;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine.UIElements;
using Utils;
using Utils.Types;
using Button = Unity.AppUI.UI.Button;
#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace UI.Views
{
    public class ProbeListItem
    {
        #region Component References

        private readonly ProbeListItemViewModel _probeListItemViewModel;

        private readonly Icon _icon;
        private readonly ActionButton _hideActionButton;

        #endregion

        public ProbeListItem(VisualElement root, ProbeListItemViewModel probeListItemViewModel)
        {
            _probeListItemViewModel = probeListItemViewModel;
            _probeListItemViewModel.PropertyChanged += OnPropertyChanged;
            root.dataSource = _probeListItemViewModel;

            // Register component references.
            _icon = root.Q<Icon>();
            _hideActionButton = root.Q<ActionButton>();
            var deleteButton = root.Q<Button>();

            // Register component events.
            deleteButton.clickable.clicked += _probeListItemViewModel.RemoveProbeCommand.Execute;

            // Register events.
            App.shuttingDown += OnShuttingDown;

            // Initialize.
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

        private void OnShuttingDown()
        {
            _probeListItemViewModel.PropertyChanged -= OnPropertyChanged;
            App.shuttingDown -= OnShuttingDown;
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
                _ => null
            };
            if (string.IsNullOrEmpty(newClass))
                return;

            // Apply.
            _icon.AddToClassList(newClass);
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void RegisterMainViewConverters()
        {
            DataTypeConverters.RegisterUnidirectionalConverterGroup<bool, StyleEnum<DisplayStyle>>(
                "IsVisualizationProbeToProbeColorVisibility",
                (ref bool isVisualizationProbe) =>
                    isVisualizationProbe ? DisplayStyle.None : DisplayStyle.Flex
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "ProbeColorToStyleColor",
                (ref ProbeColor probeColor) =>
                    new StyleColor(ProbeProperties.ProbeColors[(int)probeColor])
            );
            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "FullNameToDisplayName",
                (ref string fullName) => fullName[..8]
            );
        }
    }
}