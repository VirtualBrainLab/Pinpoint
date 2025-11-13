using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Models.Scene;
using UI.ViewModels;
using Unity.AppUI.MVVM;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;
using Utils.Types;
using Button = Unity.AppUI.UI.Button;
using FloatField = Unity.AppUI.UI.FloatField;
using Vector4Field = Unity.AppUI.UI.Vector4Field;
#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace UI.Views
{
    public class AutomationInspectorView
    {
        #region Services

        private readonly AutomationInspectorViewModel _automationInspectorViewModel;

        #endregion

        #region Component References

        private readonly Dropdown _targetDropdown;

        #endregion

        public AutomationInspectorView(AutomationInspectorViewModel automationInspectorViewModel)
        {
            // Get root and apply data source.
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>(
                "automation-inspector-view"
            );
            _automationInspectorViewModel = automationInspectorViewModel;
            root.dataSource = _automationInspectorViewModel;

            // Reference coordinate offset controls.
            var referenceCoordinateOffsetField = root.Q<Vector4Field>(
                "automation-inspector__reference-coordinate-offset-field"
            );
            referenceCoordinateOffsetField.Q<FloatField>().unit = "D";
            referenceCoordinateOffsetField.RegisterValueChangedCallback(evt =>
            {
                _automationInspectorViewModel.SetReferenceCoordinateOffsetCommand.Execute(
                    evt.newValue
                );
            });
            
            var setReferenceCoordinateOffsetButton = root.Q<Button>(
                "automation-inspector__set-reference-coordinate-offset-button"
            );
            setReferenceCoordinateOffsetButton.clickable.clicked += _automationInspectorViewModel
                .UseCurrentPositionForReferenceCoordinateOffsetCommand
                .Execute;
            
            // Target selection controls.
            _targetDropdown = root.Q<Dropdown>("automation-inspector__target-selection--dropdown");
            _targetDropdown.RegisterValueChangedCallback(evt =>
            {
                _automationInspectorViewModel.SelectTargetInsertionProbeCommand.Execute(
                    _targetDropdown.selectedIndex
                );
            });
            _targetDropdown.sourceItems = _automationInspectorViewModel.TargetInsertionProbeStates;
            _targetDropdown.bindItem = (item, index) =>
            {
                // Build from probe state.
                var probeState = _automationInspectorViewModel.TargetInsertionProbeStates[index];
                item.label = $"{probeState.Name[..8]}: {probeState.APMLDV}";
                item.icon = "target";

                // Set icon color.
                var colorClass = probeState.Color switch
                {
                    ProbeColor.DarkBlue => "probe-icon--dark-blue",
                    ProbeColor.LightBlue => "probe-icon--light-blue",
                    ProbeColor.DarkOrange => "probe-icon--dark-orange",
                    ProbeColor.LightOrange => "probe-icon--light-orange",
                    ProbeColor.DarkGreen => "probe-icon--dark-green",
                    ProbeColor.LightGreen => "probe-icon--light-green",
                    ProbeColor.DarkRed => "probe-icon--dark-red",
                    ProbeColor.LightRed => "probe-icon--light-red",
                    ProbeColor.DarkPurple => "probe-icon--dark-purple",
                    ProbeColor.LightPurple => "probe-icon--light-purple",
                    ProbeColor.DarkBrown => "probe-icon--dark-brown",
                    ProbeColor.LightBrown => "probe-icon--light-brown",
                    ProbeColor.DarkPink => "probe-icon--dark-pink",
                    ProbeColor.LightPink => "probe-icon--light-pink",
                    ProbeColor.DarkGray => "probe-icon--dark-gray",
                    ProbeColor.LightGray => "probe-icon--light-gray",
                    ProbeColor.LivelyLaugh => "probe-icon--lively-laugh",
                    ProbeColor.DarkCyan => "probe-icon--dark-cyan",
                    ProbeColor.LightCyan => "probe-icon--light-cyan",
                    _ => "",
                };
                item.Query<Icon>().AtIndex(1).AddToClassList(colorClass);
            };
            
            var targetResetButton = root.Q<IconButton>(
                "automation-inspector__target-selection--reset-button"
            );
            targetResetButton.clickable.clicked += _automationInspectorViewModel
                .ResetTargetInsertionProbeSelectionCommand
                .Execute;
            
            var targetEntryDriveButton = root.Q<Button>(
                "automation-inspector__target-entry--drive-button"
            );
            var targetEntryStopButton = root.Q<Button>(
                "automation-inspector__target-entry--stop-button"
            );
            
            // Dura offset controls.
            var duraOffsetField = root.Q<FloatField>("automation-inspector__dura-offset-field");
            var recalculateDuraOffsetButton = root.Q<Button>(
                "automation-inspector__recalculate-dura-offset-button"
            );
            
            // Drive to target insertion controls.
            var targetInsertionSpeedDropdown = root.Q<Dropdown>(
                "automation-inspector__target-insertion--speed-dropdown"
            );
            targetInsertionSpeedDropdown.sourceItems = new[]
            {
                "1 μm/s",
                "2 μm/s",
                "5 μm/s",
                "10 μm/s",
                "20 μm/s",
                "50 μm/s",
                "Test (500 μm/s)",
                "Custom",
            };
            targetInsertionSpeedDropdown.bindItem = (item, i) =>
            {
                item.label = (string)targetInsertionSpeedDropdown.sourceItems[i];
                item.icon = i switch
                {
                    <= 2 => "tortoise",
                    <= 5 => "hare",
                    6 => "test-tube",
                    _ => "pen",
                };
            };
            
            var targetInsertionCustomSpeedField = root.Q<FloatField>(
                "automation-inspector__target-insertion--custom-speed-field"
            );
            var targetInsertionDrivePastDistanceField = root.Q<FloatField>(
                "automation-inspector__target-insertion--drive-past-distance-field"
            );
            var targetInsertionETAText = root.Q<Label>(
                "automation-inspector__target-insertion--eta-text"
            );
            var targetInsertionProgressBar = root.Q<ProgressBar>(
                "automation-inspector__target-insertion--progress-bar"
            );
            var targetInsertionDriveButton = root.Q<Button>(
                "automation-inspector__target-insertion--drive-button"
            );
            var targetInsertionExitButton = root.Q<Button>(
                "automation-inspector__target-insertion--exit-button"
            );
            var targetInsertionStopButton = root.Q<Button>(
                "automation-inspector__target-insertion--stop-button"
            );

            // Register property change handlers.
            _automationInspectorViewModel.PropertyChanged += OnPropertyChanged;
            App.shuttingDown += OnShuttingDown;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_automationInspectorViewModel.TargetInsertionProbeStates):
                    _targetDropdown.sourceItems =
                        _automationInspectorViewModel.TargetInsertionProbeStates;
                    _targetDropdown.Refresh();
                    break;
                case nameof(_automationInspectorViewModel.TargetInsertionProbeIndex):
                    if (_automationInspectorViewModel.TargetInsertionProbeIndex == -1)
                    {
                        _targetDropdown.SetValueWithoutNotify(Array.Empty<int>());
                    }
                    else
                    {
                        _targetDropdown.selectedIndex =
                            _automationInspectorViewModel.TargetInsertionProbeIndex;
                    }
                    break;
            }
        }

        private void OnShuttingDown()
        {
            _automationInspectorViewModel.PropertyChanged -= OnPropertyChanged;
            App.shuttingDown -= OnShuttingDown;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void RegisterAutomationViewConverters()
        {
            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "AutomationProgressStateToTargetSelectionEnabled",
                (ref AutomationProgressState automationProgressState) =>
                    automationProgressState
                    != AutomationProgressState.DrivingToTargetEntryCoordinate
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
                "AutomationProgressStateToTargetInsertionEnabled",
                (ref AutomationProgressState automationProgressState) =>
                    automationProgressState
                        is not (
                            AutomationProgressState.DrivingToNearTarget
                            or AutomationProgressState.DrivingToPastTarget
                            or AutomationProgressState.ReturningToTarget
                        )
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<int, StyleEnum<DisplayStyle>>(
                "SelectedInsertionSpeedIndexToCustomSpeedVisibility",
                (ref int selectedTargetInsertionSpeedIndex) =>
                    selectedTargetInsertionSpeedIndex == 7 ? DisplayStyle.Flex : DisplayStyle.None
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "ETASecondsToETAText",
                (ref int etaSeconds) => $"ETA: {etaSeconds / 60}:{etaSeconds % 60:D2}"
            );

            DataTypeConverters.RegisterUnidirectionalConverterGroup<int, StyleEnum<DisplayStyle>>(
                "ETASecondsToETAVisibility",
                (ref int etaSeconds) => etaSeconds > 0 ? DisplayStyle.Flex : DisplayStyle.None
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
