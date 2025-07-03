using System;
using System.Collections.Generic;
using Services;
using Unity.AppUI.MVVM;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.ViewModels
{
    [ObservableObject]
    public partial class AtlasViewModel
    {

        #region Properties

        [ObservableProperty]
        private List<TreeViewItemData<string>> _atlasTreeData;

        #endregion

        public AtlasViewModel(AtlasService atlasService)
        {

        }

        [ICommand]
        private void SelectArea()
        {
            // [TODO] Service to trigger selection in AtlasManager
        }
    }

}