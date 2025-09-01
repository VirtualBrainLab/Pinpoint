using UI.Utils;
using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine.UIElements;

#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace UI.Views
{
    public class ManipulatorListItem
    {
        #region Component References

        private readonly Dropdown _visualizationProbeDropdown;

        #endregion
        public ManipulatorListItem(
            VisualElement root,
            ManipulatorListItemViewModel manipulatorListItemViewModel
        )
        {
            // Attach view model and register property changes.
            root.dataSource = manipulatorListItemViewModel;
            
            // Register component references.
            _visualizationProbeDropdown = root.Q<Dropdown>(
                "manipulator-list-item__visualization-probe-dropdown"
            );
            _visualizationProbeDropdown.sourceItems = manipulatorListItemViewModel.VisualizationProbeOptions;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void RegisterMainViewConverters()
        {
            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "BooleanToCheckboxState",
                (ref bool isChecked) => isChecked ? CheckboxState.Checked : CheckboxState.Unchecked
            );
        }
    }
}
