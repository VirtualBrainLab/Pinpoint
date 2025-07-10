using System;
using System.Linq;
using Models.Automation;
using UI.Utils;
using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;

namespace UI.Views
{
    public class EphysLinkView
    {
        #region Component References

        private readonly Dropdown _platformTypeDropdown;
        private readonly Button _connectButton;
        private readonly Button _disconnectButton;

        #endregion

        private readonly EphysLinkViewModel _ephysLinkViewModel;

        public EphysLinkView(TemplateContainer root, EphysLinkViewModel ephysLinkViewModel)
        {
            _ephysLinkViewModel = ephysLinkViewModel;
            root.dataSource = _ephysLinkViewModel;

            // Register component references.
            _platformTypeDropdown = root.Q<Dropdown>("ephys-link__platform-type-dropdown");
            _connectButton = root.Q<Button>("ephys-link__connect-button");
            _disconnectButton = root.Q<Button>("ephys-link__disconnect-button");

            BuildPlatformTypeDropdown();

            // Initialize component state.
            _platformTypeDropdown.value = new[] { (int)_ephysLinkViewModel.SelectedPlatformType };
        }

        private void BuildPlatformTypeDropdown()
        {
            _platformTypeDropdown.bindItem = (item, index) =>
            {
                item.label = Enum.GetValues(typeof(PlatformType)).GetValue(index) switch
                {
                    PlatformType.SensapexUmp => "Sensapex uMp",
                    PlatformType.NewScalePathfinderMpm => "New Scale Pathfinder MPM",
                    PlatformType.Custom => "Custom Server",
                    _ => throw new ArgumentOutOfRangeException(),
                };
            };
            _platformTypeDropdown.sourceItems = Enum.GetValues(typeof(PlatformType));
            _platformTypeDropdown.RegisterValueChangedCallback(evt =>
                _ephysLinkViewModel.SetSelectedPlatformTypeCommand.Execute(
                    (PlatformType)evt.newValue.FirstOrDefault()
                )
            );
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RegisterAutomationViewConverters()
        {
            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                PlatformType,
                StyleEnum<DisplayStyle>
            >(
                "PlatformTypeToPathfinderMPMHTTPServerConnectionVisibility",
                (ref PlatformType platformType) =>
                    platformType == PlatformType.NewScalePathfinderMpm
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                PlatformType,
                StyleEnum<DisplayStyle>
            >(
                "PlatformTypeToCustomServerConnectionVisibility",
                (ref PlatformType platformType) =>
                    platformType == PlatformType.Custom ? DisplayStyle.Flex : DisplayStyle.None
            );
        }
    }
}
