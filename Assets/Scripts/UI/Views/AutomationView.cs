using System.Collections.Generic;
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

        private readonly VisualElement _root;

        private readonly Button _resetReferenceCoordinateButton;
        private readonly Button _targetEntryDriveButton;
        private readonly Button _targetStopButton;
        private readonly Button _duraResetButton;
        private readonly Button _insertionDriveButton;
        private readonly Button _insertionStopButton;
        private readonly Button _insertionResetButton;

        #endregion

        private readonly AutomationViewModel _automationViewModel;

        public AutomationView(VisualElement root, AutomationViewModel automationViewModel)
        {
            _root = root;
            _automationViewModel = automationViewModel;
            _root.dataSource = _automationViewModel;

            // Register component references.
            _resetReferenceCoordinateButton = _root.Q<Button>("reference-coordinate__reset-button");
            _targetEntryDriveButton = _root.Q<Button>("target__entry-drive-button");
            _targetStopButton = _root.Q<Button>("target__stop-button");
            _duraResetButton = _root.Q<Button>("dura__reset-button");
            _insertionDriveButton = _root.Q<Button>("insertion__drive-button");
            _insertionStopButton = _root.Q<Button>("insertion__stop-button");
            _insertionResetButton = _root.Q<Button>("insertion__reset-button");

            // Edit default components.
            var referenceCoordinateDepthLabel = _root.Q<FloatField>("unity-w-input").Q<Label>();
            referenceCoordinateDepthLabel.text = "Depth";

            // Register callbacks.
            _resetReferenceCoordinateButton.clicked += _automationViewModel
                .ResetReferenceCoordinateCommand
                .Execute;
            _targetEntryDriveButton.clicked += _automationViewModel
                .DriveToTargetEntryCoordinateCommand
                .Execute;
            _targetStopButton.clicked += _automationViewModel
                .StopDriveToTargetEntryCoordinateCommand
                .Execute;
            _duraResetButton.clicked += _automationViewModel.ResetDuraOffsetCommand.Execute;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RegisterAutomationViewConverters()
        {
            DataTypeConverters.RegisterUnidirectionalConverterGroup(
                "TargetableProbeStatesToTargetInsertionOptions",
                (ref List<ProbeState> targetableProbeManagers) =>
                    targetableProbeManagers
                        .Select(probeState => $"{probeState.Name}: {probeState.APMLDV}")
                        .Prepend("None")
                        .ToList()
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
        }
    }
}
