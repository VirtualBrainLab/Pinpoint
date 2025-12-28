using System;
using System.Collections.Generic;
using System.Linq;
using Pinpoint.CoordinateSystems;
using Unity.AppUI.Redux;
using UnityEngine;
using Utils.Types;

namespace Models.Scene
{
    public static class SceneReducers
    {
        #region Probe List Reducers

        public static SceneState AddProbeReducer(SceneState state, IAction<ProbeType> action)
        {
            var newProbes = new ProbeState[state.Probes.Length + 1];
            Array.Copy(state.Probes, newProbes, state.Probes.Length);
            newProbes[state.Probes.Length] = new ProbeState { ProbeType = action.payload };
            return state with { Probes = newProbes };
        }

        public static SceneState AddVisualizationProbeReducer(
            SceneState state,
            IAction<(string ManipulatorId, string ProbeName, ProbeType ProbeType)> action
        )
        {
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.ManipulatorId);
            if (index == -1)
                return state;

            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                VisualizationProbeName = action.payload.ProbeName,
            };

            var newProbes = new ProbeState[state.Probes.Length + 1];
            Array.Copy(state.Probes, newProbes, state.Probes.Length);
            newProbes[state.Probes.Length] = new ProbeState
            {
                Name = action.payload.ProbeName,
                ProbeType = action.payload.ProbeType,
            };

            return state with
            {
                Manipulators = newManipulators,
                Probes = newProbes,
            };
        }

        public static SceneState DuplicateProbeReducer(SceneState state, IAction<string> action)
        {
            // Find the probe to duplicate.
            var probeToDuplicate = state.Probes.FirstOrDefault(probe =>
                probe.Name == action.payload
            );
            if (probeToDuplicate == null)
                return state; // If not found, return the state unchanged.

            // Create a new probe with a new UUID but same properties as the original.
            var duplicatedProbe = probeToDuplicate with
            {
                Name = Guid.NewGuid().ToString(),
            };

            var newProbes = new ProbeState[state.Probes.Length + 1];
            Array.Copy(state.Probes, newProbes, state.Probes.Length);
            newProbes[state.Probes.Length] = duplicatedProbe;

            return state with
            {
                Probes = newProbes,
            };
        }

        /// <summary>
        ///     Remove all probes with the specified name.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="action">Target probe's name in payload.</param>
        /// <returns></returns>
        public static SceneState RemoveProbeReducer(SceneState state, IAction<string> action)
        {
            // Remove all probes with the specified UUID.
            var newProbesList = state.Probes.Where(probeState => probeState.Name != action.payload).ToArray();

            // If no probes were removed, return the state unchanged.
            if (newProbesList.Length == state.Probes.Length)
                return state;

            // Erase the visualization probe reference in manipulators if it points to the removed probe.
            var newManipulatorsList = (ManipulatorState[])state.Manipulators.Clone();
            for (var i = 0; i < newManipulatorsList.Length; i++)
                if (newManipulatorsList[i].VisualizationProbeName == action.payload)
                    newManipulatorsList[i] = newManipulatorsList[i] with
                    {
                        VisualizationProbeName = string.Empty,
                    };

            // Update the state with the new probes list, and update the active probe name if it was removed.
            return state with
            {
                Probes = newProbesList,
                Manipulators = newManipulatorsList,
                ActiveProbeName =
                    state.ActiveProbeName == action.payload ? string.Empty : state.ActiveProbeName,
            };
        }

        public static SceneState RemoveAllVisualizationProbesReducer(
            SceneState state,
            IAction action
        )
        {
            var newProbesList = state.Probes.Where(probeState => state.Manipulators.All(manipulatorState => manipulatorState.VisualizationProbeName != probeState.Name)).ToArray();

            var newManipulatorsList = (ManipulatorState[])state.Manipulators.Clone();
            for (var i = 0; i < newManipulatorsList.Length; i++)
                newManipulatorsList[i] = newManipulatorsList[i] with
                {
                    VisualizationProbeName = string.Empty,
                };

            return state with
            {
                Probes = newProbesList,
                Manipulators = newManipulatorsList,
            };
        }

        #endregion

        #region Manipulator List Reducers

        public static SceneState SetManipulatorsReducer(
            SceneState state,
            IAction<List<ManipulatorState>> action
        )
        {
            return state with { Manipulators = action.payload.ToArray(), ActiveManipulatorId = "" };
        }

        #endregion

        #region Active Item Reducers

        public static SceneState SetActiveProbeReducer(SceneState state, IAction<string> action)
        {
            // If not found, return the state unchanged.
            if (state.Probes.All(probe => probe.Name != action.payload))
                return state;

            // Build a set of visualization probe names for O(1) lookup
            var vizProbeNames = state.Manipulators
                .Select(m => m.VisualizationProbeName)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToHashSet();

            // Set opaque/transparent display for probes based on active probe.
            var probesCopy = (ProbeState[])state.Probes.Clone();
            for (var i = 0; i < probesCopy.Length; i++)
            {
                var probeState = probesCopy[i];
                var isVisualizationProbe = vizProbeNames.Contains(probeState.Name);

                probesCopy[i] = probeState with
                {
                    ProbeDisplayType =
                        isVisualizationProbe ? ProbeDisplayType.Transparent
                        : probeState.Name == action.payload ? ProbeDisplayType.Opaque
                        : ProbeDisplayType.Transparent,
                };
            }

            // Update the active probe UUID.
            return state with
            {
                Probes = probesCopy,
                ActiveProbeName = action.payload,
                ActiveManipulatorId = "",
            };
        }

        public static SceneState SetActiveManipulatorReducer(
            SceneState state,
            IAction<string> action
        )
        {
            // If not found, return the state unchanged.
            if (state.Manipulators.All(manipulator => manipulator.Id != action.payload))
                return state;

            // Precompute the active manipulator's visualization probe name for efficiency.
            var activeVisualizationProbeName = state.Manipulators.First(m => m.Id == action.payload)
                .VisualizationProbeName;

            // Build a set of visualization probe names for O(1) lookup
            var vizProbeNames = state.Manipulators
                .Select(m => m.VisualizationProbeName)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToHashSet();

            // Set opaque/transparent display for probes based on active manipulator.
            var probesCopy = (ProbeState[])state.Probes.Clone();
            for (var i = 0; i < probesCopy.Length; i++)
            {
                var probeState = probesCopy[i];
                var isVisualizationProbe = vizProbeNames.Contains(probeState.Name);

                probesCopy[i] = probeState with
                {
                    ProbeDisplayType = isVisualizationProbe
                        ? (activeVisualizationProbeName == probeState.Name
                            ? ProbeDisplayType.Opaque
                            : ProbeDisplayType.Transparent)
                        : ProbeDisplayType.Transparent,
                };
            }

            // Update the active manipulator ID.
            return state with
            {
                Probes = probesCopy,
                ActiveProbeName = "",
                ActiveManipulatorId = action.payload,
            };
        }

        #endregion

        #region Probe Reducers

        public static SceneState SetProbePositionReducer(
            SceneState state,
            IAction<(string Name, Vector3 APMLDV)> action
        )
        {
            // Find the index of the target probe.
            var index = Array.FindIndex(state.Probes, probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            var newProbes = (ProbeState[])state.Probes.Clone();
            newProbes[index] = state.Probes[index] with
            {
                APMLDV = action.payload.APMLDV,
            };

            return state with
            {
                Probes = newProbes,
            };
        }

        public static SceneState SetProbePositionByReducer(
            SceneState state,
            IAction<(string Name, Vector3 SurfaceAPMLDV, float Depth, Vector3 ForwardT)> action
        )
        {
            // Find the index of the target probe.
            var index = Array.FindIndex(state.Probes, probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            var newProbes = (ProbeState[])state.Probes.Clone();
            newProbes[index] = state.Probes[index] with
            {
                APMLDV =
                    action.payload.SurfaceAPMLDV + action.payload.ForwardT * action.payload.Depth,
            };

            return state with
            {
                Probes = newProbes,
            };
        }

        public static SceneState SetProbeAnglesReducer(
            SceneState state,
            IAction<(string Name, Vector3 Angles, Vector2 PitchRange)> action
        )
        {
            // Find the index of the target probe.
            var index = Array.FindIndex(state.Probes, probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            var pitchClampedAngles = action.payload.Angles;
            pitchClampedAngles.y = Mathf.Clamp(
                action.payload.Angles.y,
                action.payload.PitchRange.x,
                action.payload.PitchRange.y
            );

            var newProbes = (ProbeState[])state.Probes.Clone();
            newProbes[index] = state.Probes[index] with
            {
                Angles = pitchClampedAngles,
            };

            return state with
            {
                Probes = newProbes,
            };
        }

        public static SceneState SetProbePositionAndAnglesByReducer(
            SceneState state,
            IAction<(
                string Name,
                Vector3 SurfaceAPMLDV,
                float Depth,
                Vector3 ForwardT,
                Vector3 Angles,
                Vector2 PitchRange
            )> action
        )
        {
            // Find the index of the target probe.
            var index = Array.FindIndex(state.Probes, probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            var pitchClampedAngles = action.payload.Angles;
            pitchClampedAngles.y = Mathf.Clamp(
                action.payload.Angles.y,
                action.payload.PitchRange.x,
                action.payload.PitchRange.y
            );

            var newProbes = (ProbeState[])state.Probes.Clone();
            newProbes[index] = state.Probes[index] with
            {
                APMLDV =
                    action.payload.SurfaceAPMLDV + action.payload.ForwardT * action.payload.Depth,
                Angles = pitchClampedAngles,
            };

            return state with
            {
                Probes = newProbes,
            };
        }

        public static SceneState BulkSetProbePositionAndAnglesByReducer(
            SceneState state,
            IAction<
                List<(
                    string Name,
                    Vector3 SurfaceAPMLDV,
                    float Depth,
                    Vector3 ForwardT,
                    Vector3 Angles,
                    Vector2 PitchRange
                )>
            > action
        )
        {
            var probesCopy = (ProbeState[])state.Probes.Clone();

            foreach (var request in action.payload)
            {
                // Find the index of the target probe.
                var index = Array.FindIndex(state.Probes, probe => probe.Name == request.Name);

                // Exit if the probe is not found.
                if (index == -1)
                    return state;

                var pitchClampedAngles = request.Angles;
                pitchClampedAngles.y = Mathf.Clamp(
                    request.Angles.y,
                    request.PitchRange.x,
                    request.PitchRange.y
                );

                // Update the probe immutably using the `with` expression
                probesCopy[index] = state.Probes[index] with
                {
                    APMLDV = request.SurfaceAPMLDV + request.ForwardT * request.Depth,
                    Angles = pitchClampedAngles,
                };
            }

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState ChangeProbePositionByReducer(
            SceneState state,
            IAction<(string Name, Vector3 APMLDV, float Depth, Vector3 ForwardT)> action
        )
        {
            // Find the index of the target probe.
            var index = Array.FindIndex(state.Probes, probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            var newProbes = (ProbeState[])state.Probes.Clone();
            newProbes[index] = state.Probes[index] with
            {
                APMLDV =
                    state.Probes[index].APMLDV
                    + action.payload.APMLDV
                    + action.payload.ForwardT * action.payload.Depth,
            };

            return state with
            {
                Probes = newProbes,
            };
        }

        public static SceneState ChangeProbeAnglesByReducer(
            SceneState state,
            IAction<(string Name, Vector3 Angles, Vector2 PitchRange)> action
        )
        {
            // Find the index of the target probe.
            var index = Array.FindIndex(state.Probes, probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            var pitchClampedAngles = action.payload.Angles;
            pitchClampedAngles.y = Mathf.Clamp(
                action.payload.Angles.y,
                action.payload.PitchRange.x,
                action.payload.PitchRange.y
            );

            var newProbes = (ProbeState[])state.Probes.Clone();
            newProbes[index] = state.Probes[index] with
            {
                Angles = state.Probes[index].Angles + pitchClampedAngles,
            };

            return state with
            {
                Probes = newProbes,
            };
        }

        public static SceneState SetProbeColorReducer(
            SceneState state,
            IAction<(string Name, ProbeColor Color)> action
        )
        {
            // Find the index of the target probe.
            var index = Array.FindIndex(state.Probes, probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            var newProbes = (ProbeState[])state.Probes.Clone();
            newProbes[index] = state.Probes[index] with
            {
                Color = action.payload.Color,
            };

            return state with
            {
                Probes = newProbes,
            };
        }

        public static SceneState SetAllProbesToLineReducer(SceneState state, IAction<bool> action)
        {
            // Build a set of visualization probe names for O(1) lookup
            var vizProbeNames = state.Manipulators
                .Select(m => m.VisualizationProbeName)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToHashSet();

            var probesCopy = (ProbeState[])state.Probes.Clone();

            // Loop through every probe...
            for (var i = 0; i < probesCopy.Length; i++)
            {
                var probeState = probesCopy[i];

                // Ignore visualization probes.
                if (vizProbeNames.Contains(probeState.Name))
                    continue;

                // If setting to line...
                // Set ProbeDisplayType based on action.payload and active probe.
                probesCopy[i] = probeState with
                {
                    ProbeDisplayType = action.payload
                        ? ProbeDisplayType.Line
                        : (state.ActiveProbeName == probeState.Name
                            ? ProbeDisplayType.Opaque
                            : ProbeDisplayType.Transparent),
                };
            }

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetProbeLockedReducer(
            SceneState state,
            IAction<(string Name, bool Locked)> action
        )
        {
            // Find the index of the target probe.
            var index = Array.FindIndex(state.Probes, probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            var newProbes = (ProbeState[])state.Probes.Clone();
            newProbes[index] = state.Probes[index] with
            {
                Locked = action.payload.Locked,
            };

            return state with
            {
                Probes = newProbes,
            };
        }

        #endregion

        #region Manipulator Reducers

        public static SceneState SetManipulatorAnglesReducer(
            SceneState state,
            IAction<(string Id, Vector3 Angles)> action
        )
        {
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                Angles = action.payload.Angles,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState SetManipulatorHandednessReducer(
            SceneState state,
            IAction<(string Id, ManipulatorHandedness Handedness)> action
        )
        {
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                Handedness = action.payload.Handedness,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState SetManipulatorReferenceCoordinateOffsetReducer(
            SceneState state,
            IAction<(string Id, Vector4 ReferenceCoordinateOffset)> action
        )
        {
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                ReferenceCoordinateOffset = action.payload.ReferenceCoordinateOffset,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState SetDuraOffsetReducer(
            SceneState state,
            IAction<(
                string Id,
                float DuraDepth,
                Vector3 DuraCoordinate,
                float DuraOffsetDelta
            )> action
        )
        {
            // Get manipulator index.
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.Id);
            if (index == -1)
                return state;

            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                DuraDepth = action.payload.DuraDepth,
                DuraCoordinate = action.payload.DuraCoordinate,
                DuraOffset =
                    state.Manipulators[index].DuraOffset + action.payload.DuraOffsetDelta,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState ResetDuraOffsetReducer(SceneState state, IAction<string> action)
        {
            // Get manipulator index.
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload);
            if (index == -1)
                return state;

            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                DuraDepth = 0,
                DuraCoordinate = Vector4.zero,
                DuraOffset = 0,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState SetManipulatorManualControlEnabledReducer(
            SceneState state,
            IAction<(string Id, bool ManualControlEnabled)> action
        )
        {
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                ManualControlEnabled = action.payload.ManualControlEnabled,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState SetManipulatorDemoHomeCoordinateReducer(
            SceneState state,
            IAction<(string Id, Vector4 DemoHomeCoordinate)> action
        )
        {
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                DemoHomeCoordinate = action.payload.DemoHomeCoordinate,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState SetManipulatorDemoTargetCoordinateReducer(
            SceneState state,
            IAction<(string Id, Vector4 DemoTargetCoordinate)> action
        )
        {
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                DemoTargetCoordinate = action.payload.DemoTargetCoordinate,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState SetManipulatorDemoRunningReducer(
            SceneState state,
            IAction<(string Id, bool IsDemoRunning)> action
        )
        {
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                IsDemoRunning = action.payload.IsDemoRunning,
            };
            return state with { Manipulators = newManipulators };
        }

        #endregion

        #region Automation Reducers

        /// <summary>
        ///     Set the selected target insertion probe for the active manipulator.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="action">Chosen probe name in the payload.</param>
        /// <returns>State with chosen target updated on the active probe.</returns>
        public static SceneState SetTargetInsertionProbeNameReducer(
            SceneState state,
            IAction<(string Id, string targetName)> action
        )
        {
            // Get manipulator index.
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.Id);
            if (index == -1)
                return state;

            try
            {
                // Verify selected target exists and is targetable.
                if (
                    string.IsNullOrEmpty(action.payload.targetName)
                    || state.Probes.All(probeState => probeState.Name != action.payload.targetName) || state.Manipulators.All(manipulatorState => manipulatorState.VisualizationProbeName != action.payload.targetName))
                {
                    throw new ArgumentException("Selected target is not targetable.");
                }

                var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
                newManipulators[index] = state.Manipulators[index] with
                {
                    TargetInsertionProbeName = action.payload.targetName ?? string.Empty,
                };

                return state with
                {
                    Manipulators = newManipulators,
                };
            }
            catch (Exception)
            {
                // Return the state unchanged if the target is not found or not targetable.
                return state;
            }
        }

        public static SceneState SetAutomationProgressStateReducer(
            SceneState state,
            IAction<(string Id, AutomationProgressState state)> action
        )
        {
            // Get manipulator index.
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.Id);
            if (index == -1)
                return state;

            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                AutomationProgressState = action.payload.state,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState SetAutomationProgressStateToNextDrivingReducer(
            SceneState state,
            IAction<string> action
        )
        {
            // Get manipulator index.
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload);
            if (index == -1)
                return state;

            // Move to next driving state if possible. If not, return the state unchanged.
            var newProgressState = state.Manipulators[index].AutomationProgressState switch
            {
                AutomationProgressState.IsCalibrated =>
                    AutomationProgressState.DrivingToTargetEntryCoordinate,
                AutomationProgressState.AtDuraInsert => AutomationProgressState.DrivingToNearTarget,
                AutomationProgressState.AtNearTargetInsert =>
                    AutomationProgressState.DrivingToPastTarget,
                AutomationProgressState.AtPastTarget => AutomationProgressState.ReturningToTarget,
                AutomationProgressState.AtTarget => AutomationProgressState.DrivingToNearTarget,
                _ => state.Manipulators[index].AutomationProgressState,
            };

            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                AutomationProgressState = newProgressState,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState SetAutomationProgressStateToNextExitingReducer(
            SceneState state,
            IAction<string> action
        )
        {
            // Get manipulator index.
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload);
            if (index == -1)
                return state;

            // Move to next exiting state if possible. If not, return the state unchanged.
            var newProgressState = state.Manipulators[index].AutomationProgressState switch
            {
                AutomationProgressState.AtDuraInsert
                or AutomationProgressState.AtNearTargetInsert
                or AutomationProgressState.AtPastTarget
                or AutomationProgressState.AtTarget => AutomationProgressState.ExitingToDura,
                AutomationProgressState.AtDuraExit => AutomationProgressState.ExitingToMargin,
                AutomationProgressState.AtExitMargin =>
                    AutomationProgressState.ExitingToTargetEntryCoordinate,
                _ => state.Manipulators[index].AutomationProgressState,
            };

            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                AutomationProgressState = newProgressState,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState CompleteAutomationIntermediateProgressReducer(
            SceneState state,
            IAction<string> action
        )
        {
            // Get manipulator index.
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload);
            if (index == -1)
                return state;

            // Move to next landmark progress state if possible. If not, return the state unchanged.
            var newProgressState = state.Manipulators[index].AutomationProgressState switch
            {
                AutomationProgressState.DrivingToTargetEntryCoordinate =>
                    AutomationProgressState.AtTargetEntryCoordinate,
                AutomationProgressState.DrivingToNearTarget =>
                    AutomationProgressState.AtNearTargetInsert,
                AutomationProgressState.DrivingToPastTarget => AutomationProgressState.AtPastTarget,
                AutomationProgressState.ReturningToTarget => AutomationProgressState.AtTarget,
                AutomationProgressState.ExitingToDura => AutomationProgressState.AtDuraExit,
                AutomationProgressState.ExitingToMargin => AutomationProgressState.AtExitMargin,
                AutomationProgressState.ExitingToTargetEntryCoordinate =>
                    AutomationProgressState.AtTargetEntryCoordinate,
                _ => state.Manipulators[index].AutomationProgressState,
            };

            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                AutomationProgressState = newProgressState,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState CancelAutomationIntermediateProgressReducer(
            SceneState state,
            IAction<string> action
        )
        {
            // Get manipulator index.
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload);
            if (index == -1)
                return state;

            // Revert to the previous landmark progress state if possible. If not, return the state unchanged.
            var newProgressState = state.Manipulators[index].AutomationProgressState switch
            {
                AutomationProgressState.DrivingToTargetEntryCoordinate =>
                    AutomationProgressState.IsCalibrated,
                AutomationProgressState.DrivingToNearTarget => AutomationProgressState.AtDuraInsert,
                AutomationProgressState.DrivingToPastTarget =>
                    AutomationProgressState.AtNearTargetInsert,
                AutomationProgressState.ReturningToTarget => AutomationProgressState.AtPastTarget,
                AutomationProgressState.ExitingToDura => AutomationProgressState.AtTarget,
                AutomationProgressState.ExitingToMargin => AutomationProgressState.AtDuraExit,
                AutomationProgressState.ExitingToTargetEntryCoordinate =>
                    AutomationProgressState.AtExitMargin,
                _ => state.Manipulators[index].AutomationProgressState,
            };

            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                AutomationProgressState = newProgressState,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState SetInsertionSpeedReducer(
            SceneState state,
            IAction<(string Id, int speed)> action
        )
        {
            // Get manipulator index.
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.Id);
            if (index == -1)
                return state;

            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                InsertionSpeed = action.payload.speed,
            };
            return state with { Manipulators = newManipulators };
        }

        public static SceneState SetDrivePastDistanceReducer(
            SceneState state,
            IAction<(string Id, int distance)> action
        )
        {
            // Get manipulator index.
            var index = Array.FindIndex(state.Manipulators, m => m.Id == action.payload.Id);
            if (index == -1)
                return state;

            var newManipulators = (ManipulatorState[])state.Manipulators.Clone();
            newManipulators[index] = state.Manipulators[index] with
            {
                DrivePastDistance = action.payload.distance,
            };
            return state with { Manipulators = newManipulators };
        }

        #endregion

        #region Platform Info

        public static SceneState SetPlatformInfoReducer(
            SceneState state,
            IAction<(int ManipulatorAxesCount, Vector4 ManipulatorDimensions)> action
        )
        {
            return state with
            {
                NumberOfAxesOnManipulator = action.payload.ManipulatorAxesCount,
                ManipulatorDimensions = action.payload.ManipulatorDimensions,
                ManipulatorCoordinateSpace = new ManipulatorSpace(
                    action.payload.ManipulatorDimensions
                ),
            };
        }

        #endregion

        #region Brain Area

        public static SceneState InitializeAreaVisibilityReducer(
            SceneState state,
            IAction<Dictionary<int, AreaDisplayType>> action
        )
        {
            // Directly set the brain area visibility dictionary
            return state with
            {
                BrainAreaVisibility = new Dictionary<int, AreaDisplayType>(action.payload),
            };
        }

        public static SceneState RotateAreaVisibilityReducer(SceneState state, IAction<int> action)
        {
            var newBrainAreaVisibility = new Dictionary<int, AreaDisplayType>(
                state.BrainAreaVisibility
            );

            // Get the area ID from the action payload
            var areaID = action.payload;

            // If the area doesn't exist in the dictionary, add it with default value (Opaque)
            if (!newBrainAreaVisibility.TryAdd(areaID, AreaDisplayType.Opaque))
            {
                // Rotate the visibility state for this specific area
                var currentValue = (int)newBrainAreaVisibility[areaID];
                var rotatedValue =
                    (currentValue + 1) % Enum.GetValues(typeof(AreaDisplayType)).Length;
                newBrainAreaVisibility[areaID] = (AreaDisplayType)rotatedValue;
            }

            Debug.Log($"Updated area visibility for {areaID} to {newBrainAreaVisibility[areaID]}");

            return state with
            {
                BrainAreaVisibility = newBrainAreaVisibility,
            };
        }

        #endregion
    }

    public static class SceneActions
    {
        #region Probe List Actions

        public static readonly ActionCreator<ProbeType> ADD_PROBE =
            $"{SliceNames.SCENE_SLICE}/AddProbe";

        public static readonly ActionCreator<(
            string ManipulatorId,
            string ProbeName,
            ProbeType ProbeType
        )> ADD_VISUALIZATION_PROBE = $"{SliceNames.SCENE_SLICE}/AddVisualizationProbe";

        public static readonly ActionCreator<string> DUPLICATE_PROBE =
            $"{SliceNames.SCENE_SLICE}/DuplicateProbe";

        public static readonly ActionCreator<string> REMOVE_PROBE =
            $"{SliceNames.SCENE_SLICE}/RemoveProbe";

        public static readonly ActionCreator REMOVE_ALL_VISUALIZATION_PROBES =
            $"{SliceNames.SCENE_SLICE}/RemoveAllVisualizationProbes";

        #endregion

        #region Manipulator List Actions

        public static readonly ActionCreator<List<ManipulatorState>> SET_MANIPULATORS =
            $"{SliceNames.SCENE_SLICE}/SetManipulators";

        #endregion

        #region Active Item Actions

        public static readonly ActionCreator<string> SET_ACTIVE_PROBE =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbe";

        public static readonly ActionCreator<string> SET_ACTIVE_MANIPULATOR =
            $"{SliceNames.SCENE_SLICE}/SetActiveManipulator";

        #endregion

        #region Probe Actions

        public static readonly ActionCreator<(string Name, Vector3 APMLDV)> SET_PROBE_POSITION =
            $"{SliceNames.SCENE_SLICE}/SetProbePosition";

        public static readonly ActionCreator<(
            string Name,
            Vector3 SurfaceAPMLDV,
            float Depth,
            Vector3 ForwardT
        )> SET_PROBE_POSITION_BY = $"{SliceNames.SCENE_SLICE}/SetProbePositionBy";

        public static readonly ActionCreator<(
            string Name,
            Vector3 Angles,
            Vector2 PitchRange
        )> SET_PROBE_ANGLES = $"{SliceNames.SCENE_SLICE}/SetProbeAngles";

        public static readonly ActionCreator<(
            string Name,
            Vector3 SurfaceAPMLDV,
            float Depth,
            Vector3 ForwardT,
            Vector3 Angles,
            Vector2 PitchRange
        )> SET_PROBE_POSITION_AND_ANGLES_BY =
            $"{SliceNames.SCENE_SLICE}/SetProbePositionAndAnglesBy";

        public static readonly ActionCreator<
            List<(
                string Name,
                Vector3 SurfaceAPMLDV,
                float Depth,
                Vector3 ForwardT,
                Vector3 Angles,
                Vector2 PitchRange
            )>
        > BULK_SET_PROBE_POSITION_AND_ANGLES_BY =
            $"{SliceNames.SCENE_SLICE}/BulkSetProbePositionAndAnglesBy";

        public static readonly ActionCreator<(
            string Name,
            Vector3 APMLDV,
            float Depth,
            Vector3 ForwardT
        )> CHANGE_PROBE_POSITION_BY = $"{SliceNames.SCENE_SLICE}/ChangeProbePositionBy";

        public static readonly ActionCreator<(
            string Name,
            Vector3 Angles,
            Vector2 PitchRange
        )> CHANGE_PROBE_ANGLES_BY = $"{SliceNames.SCENE_SLICE}/ChangeProbeAnglesBy";

        public static readonly ActionCreator<(string, ProbeColor)> SET_PROBE_COLOR =
            $"{SliceNames.SCENE_SLICE}/SetProbeColor";

        public static readonly ActionCreator<bool> SET_ALL_PROBES_TO_LINE =
            $"{SliceNames.SCENE_SLICE}/SetAllProbesToLine";

        public static readonly ActionCreator<(string, bool)> SET_PROBE_LOCKED =
            $"{SliceNames.SCENE_SLICE}/SetProbeLocked";

        #endregion

        #region Manipulator Actions

        public static readonly ActionCreator<(string Id, Vector3 Angles)> SET_MANIPULATOR_ANGLES =
            $"{SliceNames.SCENE_SLICE}/SetManipulatorAngles";

        public static readonly ActionCreator<(
            string Id,
            ManipulatorHandedness Handedness
        )> SET_MANIPULATOR_HANDEDNESS = $"{SliceNames.SCENE_SLICE}/SetManipulatorHandedness";

        public static readonly ActionCreator<(
            string Id,
            Vector4 ReferenceCoordinateOffset
        )> SET_MANIPULATOR_REFERENCE_COORDINATE_OFFSET =
            $"{SliceNames.SCENE_SLICE}/SetManipulatorReferenceCoordinateOffset";

        public static readonly ActionCreator<(
            string Id,
            float DuraDepth,
            Vector3 DuraCoordinate,
            float DuraOffsetDelta
        )> SET_DURA_OFFSET = $"{SliceNames.SCENE_SLICE}/SetDuraOffset";

        public static readonly ActionCreator<string> RESET_DURA_OFFSET =
            $"{SliceNames.SCENE_SLICE}/ResetDuraOffset";

        public static readonly ActionCreator<(
            string Id,
            bool ManualControlEnabled
        )> SET_MANIPULATOR_MANUAL_CONTROL_ENABLED =
            $"{SliceNames.SCENE_SLICE}/SetManipulatorManualControlEnabled";

        public static readonly ActionCreator<(
            string Id,
            Vector4 DemoHomeCoordinate
        )> SET_MANIPULATOR_DEMO_HOME_COORDINATE =
            $"{SliceNames.SCENE_SLICE}/SetManipulatorDemoHomeCoordinate";

        public static readonly ActionCreator<(
            string Id,
            Vector4 DemoTargetCoordinate
        )> SET_MANIPULATOR_DEMO_TARGET_COORDINATE =
            $"{SliceNames.SCENE_SLICE}/SetManipulatorDemoTargetCoordinate";

        public static readonly ActionCreator<(
            string Id,
            bool IsDemoRunning
        )> SET_MANIPULATOR_DEMO_RUNNING =
            $"{SliceNames.SCENE_SLICE}/SetManipulatorDemoRunning";

        #endregion

        #region Automation Actions

        public static readonly ActionCreator<(
            string Id,
            string targetName
        )> SET_TARGET_INSERTION_PROBE_NAME =
            $"{SliceNames.SCENE_SLICE}/SetTargetInsertionProbeName";

        public static readonly ActionCreator<(
            string Id,
            AutomationProgressState state
        )> SET_AUTOMATION_PROGRESS_STATE = $"{SliceNames.SCENE_SLICE}/SetAutomationProgressState";

        public static readonly ActionCreator<string> SET_AUTOMATION_PROGRESS_STATE_TO_NEXT_DRIVING =
            $"{SliceNames.SCENE_SLICE}/SetAutomationProgressStateToNextDriving";

        public static readonly ActionCreator<string> SET_AUTOMATION_PROGRESS_STATE_TO_NEXT_EXITING =
            $"{SliceNames.SCENE_SLICE}/SetAutomationProgressStateToNextExiting";

        public static readonly ActionCreator<string> COMPLETE_AUTOMATION_INTERMEDIATE_PROGRESS =
            $"{SliceNames.SCENE_SLICE}/CompleteAutomationIntermediateProgress";

        public static readonly ActionCreator<string> CANCEL_AUTOMATION_INTERMEDIATE_PROGRESS =
            $"{SliceNames.SCENE_SLICE}/CancelAutomationIntermediateProgress";

        public static readonly ActionCreator<(string Id, int speed)> SET_INSERTION_SPEED =
            $"{SliceNames.SCENE_SLICE}/SetInsertionSpeed";

        public static readonly ActionCreator<(string Id, int distance)> SET_DRIVE_PAST_DISTANCE =
            $"{SliceNames.SCENE_SLICE}/SetDrivePastDistance";

        #endregion

        #region Platform Info Actions

        public static readonly ActionCreator<(
            int ManipulatorAxesCount,
            Vector4 ManipulatorDimensions
        )> SET_PLATFORM_INFO = $"{SliceNames.SCENE_SLICE}/SetPlatformInfo";

        #endregion

        #region Brain Atlas

        public static readonly ActionCreator<
            Dictionary<int, AreaDisplayType>
        > INITIALIZE_AREA_VISIBILITY = $"{SliceNames.SCENE_SLICE}/InitializeAreaVisibility";

        public static readonly ActionCreator<int> ROTATE_AREA_VISIBILITY =
            $"{SliceNames.SCENE_SLICE}/RotateAreaVisibility";

        #endregion
    }
}
