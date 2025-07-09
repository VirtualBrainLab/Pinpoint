using Models.Automation;
using UI.Utils;
using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;

namespace UI.Views
{
    public class EphysLinkView
    {
        #region Component References

        private readonly Button _connectButton;
        private readonly Button _disconnectButton;

        #endregion

        private readonly EphysLinkViewModel _ephysLinkViewModel;

        public EphysLinkView(VisualElement root, EphysLinkViewModel ephysLinkViewModel)
        {
            _ephysLinkViewModel = ephysLinkViewModel;
            root.dataSource = _ephysLinkViewModel;

            // Register component references.
            _connectButton = root.Q<Button>("ephys-link__connect-button");
            _disconnectButton = root.Q<Button>("ephys-link__disconnect-button");

            var group = root.Q<RadioGroup>();
            Debug.Log($"Group value: {group.value}");
            foreach (var radio in group.Query<Radio>().ToList())
            {
                Debug.Log($"Radio {radio.label}: {radio.value}");
            }
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RegisterAutomationViewConverters()
        {
            DataTypeConverters.RegisterBidirectionalConverterGroup(
                "PlatformTypeToIndexString",
                (ref PlatformType platformType) => ((int)platformType).ToString(),
                (ref string platformTypeString) => (PlatformType)int.Parse(platformTypeString)
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "First",
                (ref PlatformType first) => first == PlatformType.SensapexUmp
            );
            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "Second",
                (ref PlatformType second) => second == PlatformType.NewScalePathfinderMpm
            );
            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "Third",
                (ref PlatformType third) => third == PlatformType.Custom
            );

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
