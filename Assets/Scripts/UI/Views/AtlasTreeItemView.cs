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
    public class AtlasTreeItemView
    {

        public AtlasTreeItemView(VisualElement root, AtlasTreeItemViewModel atlasTreeItemViewModel)
        {
            root.dataSource = atlasTreeItemViewModel;
        }
    }
}
