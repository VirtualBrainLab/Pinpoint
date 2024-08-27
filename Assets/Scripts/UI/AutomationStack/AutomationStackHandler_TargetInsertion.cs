using System;
using System.Collections.Generic;
using System.Linq;
using BrainAtlas;
using UnityEngine;

namespace UI.AutomationStack
{
    /// <summary>
    ///     Handles picking and driving to a target insertion in the Automation Stack.<br />
    /// </summary>
    public partial class AutomationStackHandler : MonoBehaviour
    {
        /// <summary>
        ///     Filter for probe managers this manipulator can target defined by:<br />
        ///     1. Are not ephys link controlled<br />
        ///     2. Are inside the brain (non-NaN entry coordinate).<br />
        ///     3. Not already selected<br />
        ///     4. Angles are coterminal<br />
        /// </summary>
        public static List<(Color, string)> GetTargetInsertionOptions()
        {
            print("GetTargetInsertionOptions");
            // Compute targetable probe managers.
            var targetableProbeManagers = ProbeManager
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
                // 3. Not already selected.
                // .Where(manager =>
                //     !_manipulatorIDToSelectedTargetProbeManager
                //         .Where(pair =>
                //             pair.Key
                //             != ProbeManager
                //                 .ActiveProbeManager
                //                 .ManipulatorBehaviorController
                //                 .ManipulatorID
                //         )
                //         .Select(pair => pair.Value)
                //         .Contains(manager)
                // )
                // 4. Angles are coterminal.
                .Where(manager =>
                    IsCoterminal(
                        manager.ProbeController.Insertion.Angles,
                        ProbeManager.ActiveProbeManager.ProbeController.Insertion.Angles
                    )
                );
            
            // Generate options.
            var options = new List<(Color, string)> { (Color.clear, "None") };
            options.AddRange(
                targetableProbeManagers.Select(targetableProbeManager =>
                    (
                        targetableProbeManager.Color,
                        (targetableProbeManager.OverrideName ?? targetableProbeManager.name)
                            + ": "
                            + SurfaceCoordinateToString(
                                targetableProbeManager.GetSurfaceCoordinateT()
                            )
                    )
                )
            );
            return options;
        }

        #region Helper functions

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
                + " ML: "
                + (Settings.DisplayUM ? mlMicrometers : mlMicrometers / 1000f)
                + " DV: "
                + (Settings.DisplayUM ? dvMicrometers : dvMicrometers / 1000f)
                + " Depth: "
                + (Settings.DisplayUM ? depthMicrometers : depthMicrometers / 1000f);
        }

        #endregion
    }
}
