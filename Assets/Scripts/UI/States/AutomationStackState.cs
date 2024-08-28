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
        [CreateProperty]
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool IsEnabled =>
            ProbeManager.ActiveProbeManager
            && ProbeManager.ActiveProbeManager.IsEphysLinkControlled;

        /// <summary>
        ///     Is the drive to selected target entry coordinate button enabled.<br />
        ///     Requires an active probe manager that is Ephys Link controlled, is calibrated to Bregma, TODO and has a selected
        ///     target.
        /// </summary>
        [CreateProperty]
        public bool IsDriveToTargetEntryCoordinateButtonEnabled =>
            IsEnabled
            && ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.HasCalibratedToBregma;

        /// <summary>
        ///     Is the drive to target insertion button enabled.<br />
        ///     Requires an active probe manager that is Ephys Link controlled and has its Dura offset calibrated.
        /// </summary>
        [CreateProperty]
        public bool IsDriveToTargetInsertionButtonEnabled =>
            IsEnabled && ProbeManager.ActiveProbeManager.ManipulatorBehaviorController.HasResetDura;

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
        ///     Property accessor for the selected target insertion index.<br />
        ///     Converts the selected target insertion probe manager to an index in the target insertion options list.<br />
        ///     Converts the index to the selected target insertion probe manager and updates the mapping.<br />
        /// </summary>
        /// <remarks>Set invariant: the panel is enabled, meaning there are options to pick from.</remarks>
        [CreateProperty]
        public int SelectedTargetInsertionIndex
        {
            get
            {
                // Shortcut exit if panel is not enabled.
                if (!IsEnabled)
                    return -1;

                // Get the current selected target insertion probe manager.
                var selectedTargetInsertionProbeManager =
                    _manipulatorProbeManagerToSelectedTargetInsertionProbeManager.GetValueOrDefault(
                        ProbeManager.ActiveProbeManager,
                        null
                    );

                // Shortcut exit if no target insertion is selected.
                if (selectedTargetInsertionProbeManager == null)
                    return 0;

                // Compute and return the index of the selected target insertion probe manager.
                return GetTargetInsertionOptionsProbeManagers()
                        .ToList()
                        .IndexOf(selectedTargetInsertionProbeManager) + 1;
            }
            set
            {
                // Remove mapping if selected index is 0 ("None").
                if (value == 0)
                {
                    _manipulatorProbeManagerToSelectedTargetInsertionProbeManager.Remove(
                        ProbeManager.ActiveProbeManager
                    );
                    return;
                }

                // Get probe manager from index.
                var targetInsertionProbeManager = GetTargetInsertionOptionsProbeManagers()
                    .ElementAt(value - 1);

                // Update the mapping
                _manipulatorProbeManagerToSelectedTargetInsertionProbeManager[
                    ProbeManager.ActiveProbeManager
                ] = targetInsertionProbeManager;
            }
        }

        #region Options

        /// <summary>
        ///     Option list for target insertion.<br />
        ///     Convert's the targetable probe manager's surface coordinate to a string and prepends "None".<br />
        /// </summary>
        /// <returns>Target insertion options as a string enumerable, or an empty enumerable if the panel is not enabled.</returns>
        [CreateProperty]
        public IEnumerable<string> TargetInsertionOptions =>
            IsEnabled
                ? GetTargetInsertionOptionsProbeManagers()
                    .Select(targetableProbeManager =>
                        (targetableProbeManager.OverrideName ?? targetableProbeManager.name)
                        + ": "
                        + SurfaceCoordinateToString(targetableProbeManager.GetSurfaceCoordinateT())
                    )
                    .Prepend("None")
                : Enumerable.Empty<string>();

        /// <summary>
        ///     Filter for probe managers this manipulator can target defined by:<br />
        ///     1. Are not ephys link controlled<br />
        ///     2. Are inside the brain (non-NaN entry coordinate).<br />
        ///     3. Not already selected<br />
        ///     4. Angles are coterminal<br />
        /// </summary>
        /// <returns>Filtered enumerable of probe managers, or an empty one if the panel is not enabled.</returns>
        private IEnumerable<ProbeManager> GetTargetInsertionOptionsProbeManagers()
        {
            // Shortcut exit if panel is not enabled.
            if (!IsEnabled)
                return Enumerable.Empty<ProbeManager>();
            return ProbeManager
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
                );
        }

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

        #endregion
    }
}
