using System;
using System.Linq;
using UI.ViewModels;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using UnityEditor;
using Utils;
using Utils.Types;
using Toggle = Unity.AppUI.UI.Toggle;
using System.Collections.Generic;
#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace UI.Views
{
    public class ProbeView
    {
        #region Component References

        private readonly Toggle _detectCollisionsToggle;
        private readonly Toggle _probeLocalAxisToggle;
        private readonly Toggle _showCraniotomyControlsToggle;
        private readonly Dropdown _angleConventionDropdown;

        #endregion

        private readonly ProbeViewModel _probeViewModel;

        // Define angle convention options matching the available conventions
        private readonly string[] _angleConventionOptions = { "Pinpoint", "IBL", "New Scale MIS", "Coronal/Sagittal" };

        public ProbeView(ProbeViewModel probeViewModel)
        {
            var root = PinpointApp.RootVisualElement.Q<TemplateContainer>("probe-view");
            _probeViewModel = probeViewModel;
            root.dataSource = _probeViewModel;

            // Register component references.
            _detectCollisionsToggle = root.Q<Toggle>("detect-collisions");
            _probeLocalAxisToggle = root.Q<Toggle>("probe-local-axis");
            _showCraniotomyControlsToggle = root.Q<Toggle>("show-craniotomy-controls");
            _angleConventionDropdown = root.Q<Dropdown>("angle-convention");

            //TODO: Initialize component state from ViewModel properties
            _detectCollisionsToggle.value = _probeViewModel.DetectCollisions;
            _probeLocalAxisToggle.value = _probeViewModel.ConvertAPML2Probe;
            _showCraniotomyControlsToggle.value = _probeViewModel.AxisControl;

            // TODO: Build angle convention dropdown with available options
            BuildAngleConventionDropdown();

            // TODO: Register event handlers to update ViewModel when UI changes
            _detectCollisionsToggle.RegisterValueChangedCallback(evt =>
                _probeViewModel.DetectCollisions = evt.newValue);
            _probeLocalAxisToggle.RegisterValueChangedCallback(evt =>
                _probeViewModel.ConvertAPML2Probe = evt.newValue);
            _showCraniotomyControlsToggle.RegisterValueChangedCallback(evt =>
                _probeViewModel.AxisControl = evt.newValue);
            _angleConventionDropdown.RegisterValueChangedCallback(evt =>
                HandleAngleConventionChanged(evt));
        }

        // TODO: Implement angle convention dropdown building
        private void BuildAngleConventionDropdown()
        {
            // Populate dropdown with angle convention options (Pinpoint, IBL, MIS, Sagittal/Coronal)
            _angleConventionDropdown.bindItem = (item, index) =>
            {
                item.label = _angleConventionOptions[index];
            };
            _angleConventionDropdown.sourceItems = _angleConventionOptions;

            // Set initial value from ViewModel
            var currentConvention = _probeViewModel.AngleConvention;
            var index = Array.IndexOf(_angleConventionOptions, currentConvention);
            if (index >= 0)
            {
                _angleConventionDropdown.SetValueWithoutNotify(new[] { index });
            }
        }

        // TODO: Implement angle convention change handler
        private void HandleAngleConventionChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            // Update ViewModel with selected angle convention
            var selectedIndex = evt.newValue?.FirstOrDefault() ?? 0;
            if (selectedIndex >= 0 && selectedIndex < _angleConventionOptions.Length)
            {
                _probeViewModel.AngleConvention = _angleConventionOptions[selectedIndex];
            }
        }
    }
}
