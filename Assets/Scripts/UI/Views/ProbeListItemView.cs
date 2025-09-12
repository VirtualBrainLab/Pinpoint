using UI.ViewModels;
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

        private readonly Icon _icon;
        private readonly ActionButton _hideActionButton;

        #endregion

        public ProbeListItem(VisualElement root, ProbeListItemViewModel probeListItemViewModel)
        {
            root.dataSource = probeListItemViewModel;

            // Register component references.
            _icon = root.Q<Icon>();
            _hideActionButton = root.Q<ActionButton>();
            var deleteButton = root.Q<Button>();

            // Register component events.
            deleteButton.clickable.clicked += probeListItemViewModel.RemoveProbeCommand.Execute;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void RegisterMainViewConverters()
        {
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
