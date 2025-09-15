using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Models.Scene;
using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;
using Utils.Types;
using Button = UnityEngine.UIElements.Button;
#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace UI.Views
{
    public class AutomationView
    {
        #region Services

        private readonly AutomationViewModel _automationViewModel;

        #endregion

        public AutomationView(AutomationViewModel automationViewModel)
        {
            // Get root and apply data source.
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>("automation-view");
            root.dataSource = automationViewModel;

            // Register component references.
            var targetDropdown = root.Q<Dropdown>("automation-view__target-dropdown");
            targetDropdown.sourceItems = new[] { "Hello", "World" };
            targetDropdown.bindItem = (element, i) =>
            {
                element.label = (string)targetDropdown.sourceItems[i];
                element.icon = "circle";
                Debug.Log(element.Query<Icon>().AtIndex(1).iconName);
            };
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void RegisterAutomationViewConverters()
        {
            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "TargetableProbeStatesToTargetInsertionOptions",
                (ref IEnumerable<ProbeState> targetableProbeStates) =>
                    targetableProbeStates
                        .Select(probeState => $"{probeState.Name[..8]}: {probeState.APMLDV}")
                        .Prepend("None")
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                AutomationProgressState,
                StyleEnum<DisplayStyle>
            >(
                "AutomationProgressStateToDriveToTargetEntryCoordinateButtonVisibility",
                (ref AutomationProgressState automationProgressState) =>
                    automationProgressState
                    == AutomationProgressState.DrivingToTargetEntryCoordinate
                        ? DisplayStyle.None
                        : DisplayStyle.Flex
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                AutomationProgressState,
                StyleEnum<DisplayStyle>
            >(
                "AutomationProgressStateToStopDriveToTargetEntryCoordinateButtonVisibility",
                (ref AutomationProgressState automationProgressState) =>
                    automationProgressState
                    == AutomationProgressState.DrivingToTargetEntryCoordinate
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "ETASecondsToETAText",
                (ref int etaSeconds) => $"ETA: {etaSeconds / 60}:{etaSeconds % 60:D2}"
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<int, StyleEnum<DisplayStyle>>(
                "ETASecondsToETAVisibility",
                (ref int etaSeconds) => etaSeconds > 0 ? DisplayStyle.Flex : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<int, StyleEnum<DisplayStyle>>(
                "SelectedInsertionSpeedIndexToCustomSpeedVisibility",
                (ref int selectedTargetInsertionSpeedIndex) =>
                    selectedTargetInsertionSpeedIndex == 4 ? DisplayStyle.Flex : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                AutomationProgressState,
                StyleEnum<DisplayStyle>
            >(
                "AutomationProgressStateToInsertionDriveButtonVisibility",
                (ref AutomationProgressState automationProgressState) =>
                    automationProgressState
                        is AutomationProgressState.AtDuraInsert
                            or AutomationProgressState.AtNearTargetInsert
                            or AutomationProgressState.AtPastTarget
                            or AutomationProgressState.AtTarget
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                AutomationProgressState,
                StyleEnum<DisplayStyle>
            >(
                "AutomationProgressStateToInsertionExitButtonVisibility",
                (ref AutomationProgressState automationProgressState) =>
                    automationProgressState
                        is AutomationProgressState.AtDuraInsert
                            or AutomationProgressState.AtNearTargetInsert
                            or AutomationProgressState.AtPastTarget
                            or AutomationProgressState.AtTarget
                            or AutomationProgressState.AtDuraExit
                            or AutomationProgressState.AtExitMargin
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<
                AutomationProgressState,
                StyleEnum<DisplayStyle>
            >(
                "AutomationProgressStateToInsertionStopButtonVisibility",
                (ref AutomationProgressState automationProgressState) =>
                    automationProgressState
                        is AutomationProgressState.DrivingToNearTarget
                            or AutomationProgressState.DrivingToPastTarget
                            or AutomationProgressState.ReturningToTarget
                            or AutomationProgressState.ExitingToDura
                            or AutomationProgressState.ExitingToMargin
                            or AutomationProgressState.ExitingToTargetEntryCoordinate
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
            );
        }
    }
}
