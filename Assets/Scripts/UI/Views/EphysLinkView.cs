using System;
using System.Linq;
using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine.UIElements;
using Utils.Types;
using Button = Unity.AppUI.UI.Button;

namespace UI.Views
{
    public class EphysLinkView
    {
        #region Component References

        private readonly Dropdown _platformTypeDropdown;

        #endregion

        private readonly EphysLinkViewModel _ephysLinkViewModel;

        public EphysLinkView(TemplateContainer root, EphysLinkViewModel ephysLinkViewModel)
        {
            _ephysLinkViewModel = ephysLinkViewModel;
            root.dataSource = _ephysLinkViewModel;

            // Register component references.
            _platformTypeDropdown = root.Q<Dropdown>("ephys-link__platform-type-dropdown");
            var connectButton = root.Q<Button>("ephys-link__connect-button");
            var disconnectButton = root.Q<Button>("ephys-link__disconnect-button");

            BuildPlatformTypeDropdown();

            // Initialize component state.
            _platformTypeDropdown.value = new[] { (int)_ephysLinkViewModel.SelectedServerConnectionType };

            // Register event handlers.
            connectButton.clickable.clicked += _ephysLinkViewModel.ConnectCommand.Execute;
            disconnectButton.clickable.clicked += _ephysLinkViewModel.DisconnectCommand.Execute;
        }

        private void BuildPlatformTypeDropdown()
        {
            _platformTypeDropdown.bindItem = (item, index) =>
            {
                item.label = Enum.GetValues(typeof(ServerConnectionType)).GetValue(index) switch
                {
                    ServerConnectionType.SensapexUmp => "Sensapex uMp",
                    ServerConnectionType.NewScalePathfinderMpm => "New Scale Pathfinder MPM",
                    ServerConnectionType.Custom => "Custom Server",
                    _ => throw new ArgumentOutOfRangeException(),
                };
            };
            _platformTypeDropdown.sourceItems = Enum.GetValues(typeof(ServerConnectionType));
            _platformTypeDropdown.RegisterValueChangedCallback(evt =>
                _ephysLinkViewModel.SetSelectedPlatformTypeCommand.Execute(
                    (ServerConnectionType)evt.newValue.FirstOrDefault()
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
                ServerConnectionType,
                StyleEnum<DisplayStyle>
            >(
                "PlatformTypeToPathfinderMPMHTTPServerConnectionVisibility",
                (ref ServerConnectionType serverConnectionType) =>
                    serverConnectionType == ServerConnectionType.NewScalePathfinderMpm
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                ServerConnectionType,
                StyleEnum<DisplayStyle>
            >(
                "PlatformTypeToCustomServerConnectionVisibility",
                (ref ServerConnectionType serverConnectionType) =>
                    serverConnectionType == ServerConnectionType.Custom ? DisplayStyle.Flex : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                EphysLinkConnectionState,
                StyleEnum<DisplayStyle>
            >(
                "ConnectionStateToConnectButtonVisibility",
                (ref EphysLinkConnectionState connectionState) =>
                    connectionState == EphysLinkConnectionState.Disconnected
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                EphysLinkConnectionState,
                StyleEnum<DisplayStyle>
            >(
                "ConnectionStateToDisconnectButtonVisibility",
                (ref EphysLinkConnectionState connectionState) =>
                    connectionState == EphysLinkConnectionState.Connected
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                EphysLinkConnectionState,
                StyleEnum<DisplayStyle>
            >(
                "ConnectionStateToConnectingProgressVisibility",
                (ref EphysLinkConnectionState connectionState) =>
                    connectionState == EphysLinkConnectionState.Connecting
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "ConnectionStateToSettingsEnabled",
                (ref EphysLinkConnectionState connectionState) =>
                    connectionState == EphysLinkConnectionState.Disconnected
            );
        }
    }
}
