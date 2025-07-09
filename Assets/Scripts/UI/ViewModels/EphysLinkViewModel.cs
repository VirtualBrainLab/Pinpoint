using System.ComponentModel;
using Models.Automation;
using Unity.AppUI.MVVM;
using UnityEngine;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class EphysLinkViewModel
    {
        #region Properties

        [ObservableProperty]
        private PlatformType _selectedPlatformType;

        [ObservableProperty]
        private string _platformValue;

        #endregion

        public EphysLinkViewModel()
        {
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedPlatformType):
                    Debug.Log($"Selected platform type: {SelectedPlatformType}");
                    break;
                case nameof(PlatformValue):
                    Debug.Log($"Platform value: {PlatformValue}");
                    break;
            }
        }
    }
}
