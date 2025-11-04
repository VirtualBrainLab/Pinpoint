using Models.Scene;
using Services;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using Utils.Types;
using UnityEngine.UIElements;
using UnityEngine;


namespace UI.ViewModels
{
    [ObservableObject]
    public partial class AtlasTreeItemViewModel
    {
        #region Properties

        [ObservableProperty]
        private Color _color;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _acronym;

        [ObservableProperty]
        private string _icon;

        [ObservableProperty]
        private float _transparency;

        #endregion

        public AtlasTreeItemViewModel((string acronym, string name, Color color, AreaDisplayType type) itemData)
        {
            Color = itemData.color;
            Name = itemData.name;
            Acronym = itemData.acronym;

            UpdateDisplayType(itemData.type);
        }

        public void UpdateDisplayType(AreaDisplayType type)
        {
            switch (type)
            {
                case AreaDisplayType.Opaque:
                    Icon = "eye";
                    Transparency = 1f;
                    break;
                case AreaDisplayType.Transparent:
                    Icon = "eye";
                    Transparency = 0.5f;
                    break;
                case AreaDisplayType.Hidden:
                    Icon = "eye-slash";
                    Transparency = 0.5f;
                    break;
            }
        }
    }
}