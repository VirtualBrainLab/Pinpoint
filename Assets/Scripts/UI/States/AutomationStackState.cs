using System;
using System.Collections.Generic;
using System.Linq;
using BrainAtlas;
using Core.Util;
using Unity.Properties;
using UnityEngine;

namespace UI.States
{
    [CreateAssetMenu]
    public class AutomationStackState : ResettingScriptableObject
    {
        #region Panel

        /// <summary>
        ///     Is the entire Automation stack enabled.
        /// </summary>
        /// <returns>True when the active probe manager is Ephys Link controlled.</returns>
        [CreateProperty]
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once MemberCanBeMadeStatic.Global
        public bool IsEnabled =>
            ProbeManager.ActiveProbeManager
            && ProbeManager.ActiveProbeManager.IsEphysLinkControlled;

        #endregion

        #region Target Insertion

        /// <summary>
        ///     Mapping from Ephys Link controlled manipulator probe's probe manager to the selected target insertion's probe
        ///     manager.
        /// </summary>
        private readonly Dictionary<
            ProbeManager,
            ProbeManager
        > _manipulatorProbeManagerToSelectedTargetInsertionProbeManager = new();

        /// <summary>
        ///     Selected target insertion option index.
        ///     Property accessor for the selected target insertion index.
        /// </summary>
        /// <remarks>Acts as a conversion layer between the mapping of probe managers to target insertion probe managers.</remarks>
        /// <exception cref="InvalidOperationException">Automation is not enabled for the active probe manager.</exception>
        [CreateProperty]
        public int SelectedTargetInsertionIndex
        {
            get
            {
                // Shortcut exit if panel is not enabled or if the probe hasn't selected a target.
                if (
                    !IsEnabled
                    || !_manipulatorProbeManagerToSelectedTargetInsertionProbeManager.TryGetValue(
                        ProbeManager.ActiveProbeManager,
                        out var selectedTargetInsertionProbeManager
                    )
                )
                    return 0;

                // Compute and return the index of the selected target insertion probe manager.
                return TargetInsertionOptions
                    .ToList()
                    .IndexOf(
                        ProbeManagerToTargetInsertionOption(selectedTargetInsertionProbeManager)
                    );
                ;
            }
            set
            {
                // Throw exception if invariant is violated.
                if (!IsEnabled)
                    throw new InvalidOperationException(
                        "Cannot set the selected target insertion index when automation is not enabled for probe "
                            + ProbeManager.ActiveProbeManager.name
                    );

                // Remove mapping if selected index <= 0 ("None").
                if (value <= 0)
                {
                    _manipulatorProbeManagerToSelectedTargetInsertionProbeManager.Remove(
                        ProbeManager.ActiveProbeManager
                    );
                    return;
                }

                // Get probe manager.
                var targetInsertionProbeManager =
                    SurfaceCoordinateStringToTargetInsertionOptionProbeManagers[
                        TargetInsertionOptions.ElementAt(value)
                    ];

                // Update the mapping
                _manipulatorProbeManagerToSelectedTargetInsertionProbeManager[
                    ProbeManager.ActiveProbeManager
                ] = targetInsertionProbeManager;
            }
        }

        /// <summary>
        ///     Option list for target insertion.
        /// </summary>
        /// <remarks>
        ///     Convert's the targetable probe manager's surface coordinate to a string and prepends "None".
        /// </remarks>
        /// <returns>Target insertion options as a string enumerable, or an empty enumerable if the panel is not enabled.</returns>
        [CreateProperty]
        // ReSharper disable once MemberCanBePrivate.Global
        public IEnumerable<string> TargetInsertionOptions =>
            IsEnabled
                ? SurfaceCoordinateStringToTargetInsertionOptionProbeManagers.Keys.Prepend("None")
                : Enumerable.Empty<string>();

        #region Option List helpers

        /// <summary>
        ///     Expose mapping from target insertion option probe manager to surface coordinate string.
        /// </summary>
        public Dictionary<
            string,
            ProbeManager
        > SurfaceCoordinateStringToTargetInsertionOptionProbeManagers =>
            TargetInsertionOptionsProbeManagers.ToDictionary(ProbeManagerToTargetInsertionOption);

        /// <summary>
        ///     Filter for probe managers this manipulator can target defined by:<br />
        ///     1. Are not ephys link controlled<br />
        ///     2. Are inside the brain (non-NaN entry coordinate).<br />
        ///     3. Not already selected<br />
        ///     4. Angles are coterminal<br />
        /// </summary>
        /// <returns>Filtered enumerable of probe managers, or an empty one if the panel is not enabled.</returns>
        private IEnumerable<ProbeManager> TargetInsertionOptionsProbeManagers =>
            IsEnabled
                ? ProbeManager
                    .Instances
                    // 1. Are not EphysLink controlled.
                    .Where(manager => !manager.IsEphysLinkControlled)
                    // 2. Are inside the brain (non-NaN entry coordinate).
                    .Where(manager =>
                        !float.IsNaN(
                            manager
                                .FindEntryIdxCoordinate(
                                    BrainAtlasManager.ActiveReferenceAtlas.World2AtlasIdx(
                                        manager.ProbeController.Insertion.PositionWorldU()
                                    ),
                                    BrainAtlasManager.ActiveReferenceAtlas.World2Atlas_Vector(
                                        manager.ProbeController.GetTipWorldU().tipUpWorldU
                                    )
                                )
                                .x
                        )
                    )
                    // 3. Not already selected (except for your own).
                    .Where(manager =>
                        !_manipulatorProbeManagerToSelectedTargetInsertionProbeManager
                            .Where(pair => pair.Key != ProbeManager.ActiveProbeManager)
                            .Select(pair => pair.Value)
                            .Contains(manager)
                    )
                    // 4. Angles are coterminal.
                    .Where(manager =>
                        IsCoterminal(
                            manager.ProbeController.Insertion.Angles,
                            ProbeManager.ActiveProbeManager.ProbeController.Insertion.Angles
                        )
                    )
                : Enumerable.Empty<ProbeManager>();

        /// <summary>
        ///     Compute if two sets of 3D angles are coterminal.
        /// </summary>
        /// <param name="first">First set of angles.</param>
        /// <param name="second">Second set of angles.</param>
        /// <returns>True if the 3D angles are coterminous, false otherwise.</returns>
        private static bool IsCoterminal(Vector3 first, Vector3 second)
        {
            return Mathf.Abs(first.x - second.x) % 360 < 0.01f
                && Mathf.Abs(first.y - second.y) % 360 < 0.01f
                && Mathf.Abs(first.z - second.z) % 360 < 0.01f;
        }

        /// <summary>
        ///     Create a target insertion option string from a probe manager.
        /// </summary>
        /// <param name="manager">Probe manager to extract info from</param>
        /// <returns>Target insertion option string from a probe manager.</returns>
        private string ProbeManagerToTargetInsertionOption(ProbeManager manager)
        {
            return (manager.OverrideName ?? manager.name)
                + ": "
                + SurfaceCoordinateToString(manager.GetSurfaceCoordinateT());
        }

        /// <summary>
        ///     Convert a surface coordinate to a string.
        /// </summary>
        /// <param name="surfaceCoordinate">Brain surface coordinate to convert.</param>
        /// <returns>Brain surface coordinate encoded as a string.</returns>
        private static string SurfaceCoordinateToString(
            (Vector3 surfaceCoordinateT, float depthT) surfaceCoordinate
        )
        {
            var apMicrometers = Math.Truncate(surfaceCoordinate.surfaceCoordinateT.x * 1000);
            var mlMicrometers = Math.Truncate(surfaceCoordinate.surfaceCoordinateT.y * 1000);
            var dvMicrometers = Math.Truncate(surfaceCoordinate.surfaceCoordinateT.z * 1000);
            var depthMicrometers = Math.Truncate(surfaceCoordinate.depthT * 1000);
            return "AP: "
                + (Settings.DisplayUM ? apMicrometers : apMicrometers / 1000f)
                + ", ML: "
                + (Settings.DisplayUM ? mlMicrometers : mlMicrometers / 1000f)
                + ", DV: "
                + (Settings.DisplayUM ? dvMicrometers : dvMicrometers / 1000f)
                + ", Depth: "
                + (Settings.DisplayUM ? depthMicrometers : depthMicrometers / 1000f);
        }

        #endregion

        /// <summary>
        ///     Is the drive to selected target entry coordinate button enabled.<br />
        /// </summary>
        /// <returns>
        ///     Returns true if the active probe manager is Ephys Link controlled, calibrated to Bregma, and has a selected target.
        /// </returns>
        [CreateProperty]
        public bool IsDriveToTargetEntryCoordinateButtonEnabled =>
            IsEnabled
            && ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.ProbeAutomationStateManager.IsCalibrated()
            && _manipulatorProbeManagerToSelectedTargetInsertionProbeManager.ContainsKey(
                ProbeManager.ActiveProbeManager
            );

        /// <summary>
        ///     Record of probes that have acknowledged their target insertion is out of their bounds.
        /// </summary>
        public readonly HashSet<ProbeManager> AcknowledgedTargetInsertionIsOutOfBoundsProbes =
            new();

        /// <summary>
        ///     Text for the drive to target entry coordinate button.
        /// </summary>
        /// <returns>
        ///     Says "Stop" when the probe is in motion, and "Drive to Target Entry Coordinate" otherwise.
        /// </returns>
        [CreateProperty]
        public string DriveToTargetEntryCoordinateButtonText =>
            IsEnabled
            && ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.ProbeAutomationStateManager.IsDrivingToEntryCoordinate()
                ? "Stop"
                : "Drive to Target Entry Coordinate";

        #endregion

        #region Insertion

        /// <summary>
        ///     Is the drive to target insertion button enabled.
        /// </summary>
        /// <returns>
        ///     Returns true if the active probe manager is Ephys Link controlled and has its Dura offset calibrated.
        /// </returns>
        [CreateProperty]
        public bool IsDriveToTargetInsertionButtonEnabled =>
            IsEnabled
            && ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.ProbeAutomationStateManager.IsAtDura();

        #endregion
    }
}
