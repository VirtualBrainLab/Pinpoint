using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Models.Scene;
using NUnit.Framework;
using UI.Utils;
using UI.ViewModels;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class AutomationView
    {
        #region Component References

        private readonly Button _resetReferenceCoordinateButton;
        private readonly RadioButtonGroup _targetChoicesGroup;
        private readonly Button _targetEntryDriveButton;
        private readonly Button _targetStopButton;
        private readonly Button _duraResetButton;
        private readonly Button _insertionDriveButton;
        private readonly Button _insertionExitButton;
        private readonly Button _insertionStopButton;

        #endregion

        private readonly AutomationViewModel _automationViewModel;

        public AutomationView(
            TemplateContainer root,
            AutomationViewModel automationViewModel,
            EphysLinkViewModel ephysLinkViewModel
        )
        {
            // _automationViewModel = automationViewModel;
            // root.dataSource = _automationViewModel;
            // _automationViewModel.PropertyChanged += OnPropertyChanged;
            //
            // // Register component references.
            // _resetReferenceCoordinateButton = root.Q<Button>("reference-coordinate__reset-button");
            // _targetChoicesGroup = root.Q<RadioButtonGroup>("target__choices-group");
            // _targetEntryDriveButton = root.Q<Button>("target__entry-drive-button");
            // _targetStopButton = root.Q<Button>("target__stop-button");
            // _duraResetButton = root.Q<Button>("dura__reset-button");
            // _insertionDriveButton = root.Q<Button>("insertion__drive-button");
            // _insertionExitButton = root.Q<Button>("insertion__exit-button");
            // _insertionStopButton = root.Q<Button>("insertion__stop-button");

            // Initialize subviews.
            _ = new EphysLinkView(root.Q<TemplateContainer>("ephys-link-view"), ephysLinkViewModel);

            // // Edit default components.
            // var referenceCoordinateDepthLabel = root.Q<FloatField>("unity-w-input").Q<Label>();
            // referenceCoordinateDepthLabel.text = "Depth";
            //
            // // Register callbacks.
            // _resetReferenceCoordinateButton.clicked += _automationViewModel
            //     .ResetReferenceCoordinateCommand
            //     .Execute;
            // _targetEntryDriveButton.clicked += _automationViewModel
            //     .DriveToTargetEntryCoordinateCommand
            //     .Execute;
            // _targetStopButton.clicked += _automationViewModel
            //     .StopDriveToTargetEntryCoordinateCommand
            //     .Execute;
            // _duraResetButton.clicked += _automationViewModel.ResetDuraOffsetCommand.Execute;
            // _insertionDriveButton.clicked += _automationViewModel.InsertionDriveCommand.Execute;
            // _insertionExitButton.clicked += _automationViewModel.InsertionExitCommand.Execute;
            // _insertionStopButton.clicked += _automationViewModel.StopInsertionDriveCommand.Execute;
            //
            // // Initialize view from view model state.
            // ApplyProbeColorsToTargetChoices();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_automationViewModel.TargetInsertionProbeStates):
                    ApplyProbeColorsToTargetChoices();
                    break;
            }
        }

        private void ApplyProbeColorsToTargetChoices()
        {
            _targetChoicesGroup
                .Query<Label>()
                .ForEach(label =>
                {
                    // Skip the "None" option.
                    if (label.text == "None")
                    {
                        return;
                    }

                    var checkMarkVisualElement = label.parent.Children().First();
                    var probeColor = _automationViewModel
                        .TargetInsertionProbeStates.First(state =>
                            state.UUID[..8] == label.text[..8]
                        )
                        .Color;
                    checkMarkVisualElement.style.backgroundColor = probeColor;
                });
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
                        .Select(probeState => $"{probeState.UUID[..8]}: {probeState.APMLDV}")
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
