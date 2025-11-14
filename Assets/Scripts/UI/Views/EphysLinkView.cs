using System;
using System.Linq;
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
    public class EphysLinkView
    {
        #region Component References

        private readonly Dropdown _platformTypeDropdown;

        #endregion

        private readonly EphysLinkViewModel _ephysLinkViewModel;

        public EphysLinkView(EphysLinkViewModel ephysLinkViewModel)
        {
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>("ephys-link-view");
            _ephysLinkViewModel = ephysLinkViewModel;
            root.dataSource = _ephysLinkViewModel;

            // Register component references.
            _platformTypeDropdown = root.Q<Dropdown>("ephys-link__platform-type-dropdown");
            var connectButton = root.Q<Button>("ephys-link__connect-button");
            var disconnectButton = root.Q<Button>("ephys-link__disconnect-button");

            BuildPlatformTypeDropdown();

            // Initialize component state.
            _platformTypeDropdown.value = new[] { (int)_ephysLinkViewModel.SelectedPlatformType };

            // Register event handlers.
            connectButton.clickable.clicked += _ephysLinkViewModel.ConnectCommand.Execute;
            disconnectButton.clickable.clicked += _ephysLinkViewModel.DisconnectCommand.Execute;
        }

        private void BuildPlatformTypeDropdown()
        {
            _platformTypeDropdown.bindItem = (item, index) =>
            {
                item.label = Enum.GetValues(typeof(EphysLinkPlatformType)).GetValue(index) switch
                {
                    EphysLinkPlatformType.SensapexUmp => "Sensapex uMp",
                    EphysLinkPlatformType.NewScalePathfinderMpm => "New Scale Pathfinder MPM",
                    EphysLinkPlatformType.Custom => "Custom Server",
                    _ => throw new ArgumentOutOfRangeException(),
                };
            };
            _platformTypeDropdown.sourceItems = Enum.GetValues(typeof(EphysLinkPlatformType));
            _platformTypeDropdown.RegisterValueChangedCallback(evt =>
                _ephysLinkViewModel.SetSelectedPlatformTypeCommand.Execute(
                    (EphysLinkPlatformType)evt.newValue.FirstOrDefault()
                )
            );
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void RegisterAutomationViewConverters()
        {
            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                EphysLinkPlatformType,
                StyleEnum<DisplayStyle>
            >(
                "EphysLinkPlatformTypeToPathfinderMPMHTTPServerConnectionVisibility",
                (ref EphysLinkPlatformType platformType) =>
                    platformType == EphysLinkPlatformType.NewScalePathfinderMpm
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                EphysLinkPlatformType,
                StyleEnum<DisplayStyle>
            >(
                "EphysLinkPlatformTypeToCustomServerConnectionVisibility",
                (ref EphysLinkPlatformType platformType) =>
                    platformType == EphysLinkPlatformType.Custom
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                EphysLinkConnectionState,
                StyleEnum<DisplayStyle>
            >(
                "EphysLinkConnectionStateToConnectButtonVisibility",
                (ref EphysLinkConnectionState connectionState) =>
                    connectionState == EphysLinkConnectionState.Disconnected
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                EphysLinkConnectionState,
                StyleEnum<DisplayStyle>
            >(
                "EphysLinkConnectionStateToDisconnectButtonVisibility",
                (ref EphysLinkConnectionState connectionState) =>
                    connectionState == EphysLinkConnectionState.Connected
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                EphysLinkConnectionState,
                StyleEnum<DisplayStyle>
            >(
                "EphysLinkConnectionStateToConnectingProgressVisibility",
                (ref EphysLinkConnectionState connectionState) =>
                    connectionState == EphysLinkConnectionState.Connecting
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "EphysLinkConnectionStateToSettingsEnabled",
                (ref EphysLinkConnectionState connectionState) =>
                    connectionState == EphysLinkConnectionState.Disconnected
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                string,
                StyleEnum<DisplayStyle>
            >(
                "StringToDisplayStyleConverter",
                (ref string value) =>
                    !string.IsNullOrEmpty(value) ? DisplayStyle.Flex : DisplayStyle.None
            );
        }
    }
}
