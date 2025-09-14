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
            var newProbesList = state.Probes.ToList();
            newProbesList.Add(new ProbeState { ProbeType = action.payload });
            return state with { Probes = newProbesList };
        }

        public static SceneState AddVisualizationProbeReducer(
            SceneState state,
            IAction<(string ManipulatorId, string ProbeName, ProbeType ProbeType)> action
        )
        {
            var index = state.Manipulators.FindIndex(m => m.Id == action.payload.ManipulatorId);
            if (index == -1)
                return state;
            var newManipulatorsList = state.Manipulators.ToList();
            newManipulatorsList[index] = newManipulatorsList[index] with
            {
                VisualizationProbeName = action.payload.ProbeName,
            };

            var newProbesList = state.Probes.ToList();
            newProbesList.Add(
                new ProbeState
                {
                    Name = action.payload.ProbeName,
                    ProbeType = action.payload.ProbeType,
                }
            );

            return state with
            {
                Probes = newProbesList,
                Manipulators = newManipulatorsList,
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

            // Create a copy of the probes list
            var newProbesList = state.Probes.ToList();

            // Create a new probe with a new UUID but same properties as the original.
            var duplicatedProbe = probeToDuplicate with
            {
                Name = Guid.NewGuid().ToString(),
            };

            // Add the duplicated probe to the list.
            newProbesList.Add(duplicatedProbe);

            return state with
            {
                Probes = newProbesList,
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
            var newProbesList = state.Probes.ToList();

            // If no probes were removed, return the state unchanged.
            if (newProbesList.RemoveAll(probeState => probeState.Name == action.payload) == 0)
                return state;

            // Erase the visualization probe reference in manipulators if it points to the removed probe.
            var newManipulatorsList = state.Manipulators.ToList();
            for (var i = 0; i < newManipulatorsList.Count; i++)
            {
                if (newManipulatorsList[i].VisualizationProbeName == action.payload)
                {
                    newManipulatorsList[i] = newManipulatorsList[i] with
                    {
                        VisualizationProbeName = string.Empty,
                    };
                }
            }

            // Update the state with the new probes list, and update the active probe name if it was removed.
            return state with
            {
                Probes = newProbesList,
                ActiveProbeName =
                    state.ActiveProbeName == action.payload ? string.Empty : state.ActiveProbeName,
            };
        }

        public static SceneState RemoveAllVisualizationProbesReducer(
            SceneState state,
            IAction action
        )
        {
            var newProbesList = state.Probes.ToList();
            var nonVisualizationProbeList = newProbesList.Where(probeState =>
                !state
                    .Manipulators.Select(manipulatorState =>
                        manipulatorState.VisualizationProbeName
                    )
                    .Contains(probeState.Name)
            );

            var newManipulatorsList = state.Manipulators.ToList();
            for (var i = 0; i < newManipulatorsList.Count; i++)
            {
                newManipulatorsList[i] = newManipulatorsList[i] with
                {
                    VisualizationProbeName = string.Empty,
                };
            }

            return state with
            {
                Probes = nonVisualizationProbeList.ToList(),
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
            return state with { Manipulators = action.payload, ActiveManipulatorId = "" };
        }

        #endregion

        #region Active Item Reducers

        public static SceneState SetActiveProbeReducer(SceneState state, IAction<string> action)
        {
            // If not found, return the state unchanged.
            if (!state.Probes.Exists(probe => probe.Name == action.payload))
                return state;

            // Update the active probe UUID.
            return state with
            {
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
            if (!state.Manipulators.Exists(manipulator => manipulator.Id == action.payload))
                return state;

            // Update the active manipulator ID.
            return state with
            {
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
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                APMLDV = action.payload.APMLDV,
            };

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetProbePositionByReducer(
            SceneState state,
            IAction<(string Name, Vector3 SurfaceAPMLDV, float Depth, Vector3 ForwardT)> action
        )
        {
            // Find the index of the target probe.
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                APMLDV =
                    action.payload.SurfaceAPMLDV + action.payload.ForwardT * action.payload.Depth,
            };

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetProbeAnglesReducer(
            SceneState state,
            IAction<(string Name, Vector3 Angles, Vector2 PitchRange)> action
        )
        {
            // Find the index of the target probe.
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            var pitchClampedAngles = action.payload.Angles;
            pitchClampedAngles.y = Mathf.Clamp(
                action.payload.Angles.y,
                action.payload.PitchRange.x,
                action.payload.PitchRange.y
            );

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                Angles = pitchClampedAngles,
            };

            return state with
            {
                Probes = probesCopy,
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
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            var pitchClampedAngles = action.payload.Angles;
            pitchClampedAngles.y = Mathf.Clamp(
                action.payload.Angles.y,
                action.payload.PitchRange.x,
                action.payload.PitchRange.y
            );

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                APMLDV =
                    action.payload.SurfaceAPMLDV + action.payload.ForwardT * action.payload.Depth,
                Angles = pitchClampedAngles,
            };

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
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                APMLDV =
                    probesCopy[index].APMLDV
                    + action.payload.APMLDV
                    + action.payload.ForwardT * action.payload.Depth,
            };

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState ChangeProbeAnglesByReducer(
            SceneState state,
            IAction<(string Name, Vector3 Angles, Vector2 PitchRange)> action
        )
        {
            // Find the index of the target probe.
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            var pitchClampedAngles = action.payload.Angles;
            pitchClampedAngles.y = Mathf.Clamp(
                action.payload.Angles.y,
                action.payload.PitchRange.x,
                action.payload.PitchRange.y
            );

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                Angles = probesCopy[index].Angles + pitchClampedAngles,
            };

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetProbeColorReducer(
            SceneState state,
            IAction<(string Name, ProbeColor Color)> action
        )
        {
            // Find the index of the target probe.
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                Color = action.payload.Color,
            };

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
            var index = state.Probes.FindIndex(probe => probe.Name == action.payload.Name);

            // Exit if the probe is not found.
            if (index == -1)
                return state;

            // Create a copy of the probes list
            var probesCopy = state.Probes.ToList();

            // Update the probe immutably using the `with` expression
            probesCopy[index] = probesCopy[index] with
            {
                Locked = action.payload.Locked,
            };

            return state with
            {
                Probes = probesCopy,
            };
        }

        #endregion

        #region Manipulator Reducers

        public static SceneState SetManipulatorAnglesReducer(
            SceneState state,
            IAction<(string Id, Vector3 Angles)> action
        )
        {
            var index = state.Manipulators.FindIndex(m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var manipulatorsCopy = state.Manipulators.ToList();
            manipulatorsCopy[index] = manipulatorsCopy[index] with
            {
                Angles = action.payload.Angles,
            };
            return state with { Manipulators = manipulatorsCopy };
        }

        public static SceneState SetManipulatorHandednessReducer(
            SceneState state,
            IAction<(string Id, ManipulatorHandedness Handedness)> action
        )
        {
            var index = state.Manipulators.FindIndex(m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var manipulatorsCopy = state.Manipulators.ToList();
            manipulatorsCopy[index] = manipulatorsCopy[index] with
            {
                Handedness = action.payload.Handedness,
            };
            return state with { Manipulators = manipulatorsCopy };
        }

        public static SceneState SetManipulatorReferenceCoordinateOffsetReducer(
            SceneState state,
            IAction<(string Id, Vector4 ReferenceCoordinateOffset)> action
        )
        {
            var index = state.Manipulators.FindIndex(m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var manipulatorsCopy = state.Manipulators.ToList();
            manipulatorsCopy[index] = manipulatorsCopy[index] with
            {
                ReferenceCoordinateOffset = action.payload.ReferenceCoordinateOffset,
            };
            return state with { Manipulators = manipulatorsCopy };
        }

        public static SceneState SetManipulatorDuraOffsetReducer(
            SceneState state,
            IAction<(string Id, float DuraOffset)> action
        )
        {
            var index = state.Manipulators.FindIndex(m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var manipulatorsCopy = state.Manipulators.ToList();
            manipulatorsCopy[index] = manipulatorsCopy[index] with
            {
                DuraOffset = action.payload.DuraOffset,
            };
            return state with { Manipulators = manipulatorsCopy };
        }

        public static SceneState ChangeManipulatorDuraOffsetByReducer(
            SceneState state,
            IAction<(string Id, float DuraOffsetDelta)> action
        )
        {
            var index = state.Manipulators.FindIndex(m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var manipulatorsCopy = state.Manipulators.ToList();
            manipulatorsCopy[index] = manipulatorsCopy[index] with
            {
                DuraOffset = manipulatorsCopy[index].DuraOffset + action.payload.DuraOffsetDelta,
            };
            return state with { Manipulators = manipulatorsCopy };
        }

        public static SceneState SetManipulatorManualControlEnabledReducer(
            SceneState state,
            IAction<(string Id, bool ManualControlEnabled)> action
        )
        {
            var index = state.Manipulators.FindIndex(m => m.Id == action.payload.Id);
            if (index == -1)
                return state;
            var manipulatorsCopy = state.Manipulators.ToList();
            manipulatorsCopy[index] = manipulatorsCopy[index] with
            {
                ManualControlEnabled = action.payload.ManualControlEnabled,
            };
            return state with { Manipulators = manipulatorsCopy };
        }

        #endregion

        #region Automation Reducers

        /// <summary>
        ///     Set the selected target insertion probe for the active probe.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="action">Chosen probe name in the payload.</param>
        /// <returns>State with chosen target updated on the active probe.</returns>
        public static SceneState SetSelectedTargetInsertionProbeNameReducer(
            SceneState state,
            IAction<string> action
        )
        {
            try
            {
                // Verify selected target exists and is targetable.
                var selectedTarget = state.Probes.First(probeState =>
                    probeState.Name == action.payload
                );
                if (selectedTarget.IsEphysLinkControlled)
                    throw new ArgumentException("Selected target is not targetable.");

                // Update the selected target insertion probe for the active probe.
                var probesCopy = state.Probes.ToList();
                probesCopy[state.ActiveProbeIndex].SelectedTargetInsertionProbeName =
                    action.payload;

                return state with
                {
                    Probes = probesCopy,
                };
            }
            catch (Exception)
            {
                // Return the state unchanged if the target is not found or not targetable.
                return state;
            }
        }

        public static SceneState SetActiveProbeAutomationProgressStateReducer(
            SceneState state,
            IAction<AutomationProgressState> action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Set the active probe's automation progress state.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].AutomationProgressState = action.payload;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetActiveProbeAutomationProgressStateToNextDrivingReducer(
            SceneState state,
            IAction action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Move to next driving state if possible. If not, return the state unchanged.
            var newProgressState = state.ActiveProbeState.AutomationProgressState switch
            {
                AutomationProgressState.IsCalibrated =>
                    AutomationProgressState.DrivingToTargetEntryCoordinate,
                AutomationProgressState.AtDuraInsert => AutomationProgressState.DrivingToNearTarget,
                AutomationProgressState.AtNearTargetInsert =>
                    AutomationProgressState.DrivingToPastTarget,
                AutomationProgressState.AtPastTarget => AutomationProgressState.ReturningToTarget,
                AutomationProgressState.AtTarget => AutomationProgressState.DrivingToNearTarget,
                _ => state.ActiveProbeState.AutomationProgressState,
            };

            // Set the active probe's automation progress state to driving to target entry coordinate.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].AutomationProgressState = newProgressState;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetActiveProbeAutomationProgressStateToNextExitingReducer(
            SceneState state,
            IAction action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Move to next exiting state if possible. If not, return the state unchanged.
            var newProgressState = state.ActiveProbeState.AutomationProgressState switch
            {
                AutomationProgressState.AtDuraInsert
                or AutomationProgressState.AtNearTargetInsert
                or AutomationProgressState.AtPastTarget
                or AutomationProgressState.AtTarget => AutomationProgressState.ExitingToDura,
                AutomationProgressState.AtDuraExit => AutomationProgressState.ExitingToMargin,
                AutomationProgressState.AtExitMargin =>
                    AutomationProgressState.ExitingToTargetEntryCoordinate,
                _ => state.ActiveProbeState.AutomationProgressState,
            };

            // Set the active probe's automation progress state to next exiting state.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].AutomationProgressState = newProgressState;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState CompleteActiveProbeAutomationIntermediateProgressReducer(
            SceneState state,
            IAction action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Move to next landmark progress state if possible. If not, return the state unchanged.
            var newProgressState = state.ActiveProbeState.AutomationProgressState switch
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
                _ => state.ActiveProbeState.AutomationProgressState,
            };

            // Complete the intermediate progress for the active probe.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].AutomationProgressState = newProgressState;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState CancelActiveProbeAutomationIntermediateProgressReducer(
            SceneState state,
            IAction action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Revert to the previous landmark progress state if possible. If not, return the state unchanged.
            var newProgressState = state.ActiveProbeState.AutomationProgressState switch
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
                _ => state.ActiveProbeState.AutomationProgressState,
            };

            // Cancel the intermediate progress for the active probe.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].AutomationProgressState = newProgressState;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetActiveProbeReferenceCoordinateReducer(
            SceneState state,
            IAction<Vector4> action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Set the active probe's reference coordinate.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].ReferenceCoordinateOffset = action.payload;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetActiveProbeDuraOffsetReducer(
            SceneState state,
            IAction<float> action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Set the active probe's Dura offset.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].DuraDepth = action.payload;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetActiveProbeTargetInsertionBaseSpeedReducer(
            SceneState state,
            IAction<int> action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Set the active probe's target insertion speed.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].InsertionBaseSpeed = action.payload;

            return state with
            {
                Probes = probesCopy,
            };
        }

        public static SceneState SetActiveProbeDrivePastDistanceReducer(
            SceneState state,
            IAction<int> action
        )
        {
            // If no active probe, return the state unchanged.
            if (state.ActiveProbeState == null)
                return state;

            // Set the active probe's drive past distance.
            var probesCopy = state.Probes.ToList();
            probesCopy[state.ActiveProbeIndex].DrivePastDistance = action.payload;

            return state with
            {
                Probes = probesCopy,
            };
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
            float DuraOffset
        )> SET_MANIPULATOR_DURA_OFFSET = $"{SliceNames.SCENE_SLICE}/SetManipulatorDuraOffset";

        public static readonly ActionCreator<(
            string Id,
            float DuraOffsetDelta
        )> CHANGE_MANIPULATOR_DURA_OFFSET_BY =
            $"{SliceNames.SCENE_SLICE}/ChangeManipulatorDuraOffsetBy";

        public static readonly ActionCreator<(
            string Id,
            bool ManualControlEnabled
        )> SET_MANIPULATOR_MANUAL_CONTROL_ENABLED =
            $"{SliceNames.SCENE_SLICE}/SetManipulatorManualControlEnabled";

        #endregion

        #region Automation Actions

        public static readonly ActionCreator<string> SET_SELECTED_TARGET_INSERTION_PROBE_NAME =
            $"{SliceNames.SCENE_SLICE}/SetSelectedTargetInsertionProbeName";

        public static readonly ActionCreator<AutomationProgressState> SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeAutomationProgressState";

        public static readonly ActionCreator SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE_TO_NEXT_DRIVING =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeAutomationProgressStateToNextDriving";

        public static readonly ActionCreator SET_ACTIVE_PROBE_AUTOMATION_PROGRESS_STATE_TO_NEXT_EXITING =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeAutomationProgressStateToNextExiting";

        public static readonly ActionCreator COMPLETE_ACTIVE_PROBE_AUTOMATION_INTERMEDIATE_PROGRESS =
            $"{SliceNames.SCENE_SLICE}/CompleteActiveProbeAutomationIntermediateProgress";

        public static readonly ActionCreator CANCEL_ACTIVE_PROBE_AUTOMATION_INTERMEDIATE_PROGRESS =
            $"{SliceNames.SCENE_SLICE}/CancelActiveProbeAutomationIntermediateProgress";

        public static readonly ActionCreator<Vector4> SET_ACTIVE_PROBE_REFERENCE_COORDINATE =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeReferenceCoordinate";

        public static readonly ActionCreator<float> SET_ACTIVE_PROBE_DURA_OFFSET =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeDuraOffset";

        public static readonly ActionCreator<int> SET_ACTIVE_PROBE_INSERTION_BASE_SPEED =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeInsertionBaseSpeed";

        public static readonly ActionCreator<int> SET_ACTIVE_PROBE_DRIVE_PAST_DISTANCE =
            $"{SliceNames.SCENE_SLICE}/SetActiveProbeDrivePastDistance";

        #endregion

        #region Platform Info Actions

        public static readonly ActionCreator<(
            int ManipulatorAxesCount,
            Vector4 ManipulatorDimensions
        )> SET_PLATFORM_INFO = $"{SliceNames.SCENE_SLICE}/SetPlatformInfo";

        #endregion

        #region Brain Atlas

        public static readonly ActionCreator<int> ROTATE_AREA_VISIBILITY =
            $"{SliceNames.SCENE_SLICE}/RotateAreaVisibility";

        #endregion
    }
}
