// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global

using UnityEngine;

namespace EphysLink
{
#pragma warning disable CS0649

    #region Input Data Format

    /// <summary>
    ///     Enable/Disable write access to the server event argument format.
    /// </summary>
    public struct CanWriteInputDataFormat
    {
        public string manipulator_id;
        public bool can_write;
        public float hours;

        /// <summary>
        ///     Construct a new can_write event argument.
        /// </summary>
        /// <param name="manipulatorID">ID of the manipulator to set the state on</param>
        /// <param name="canWrite">Write state to set</param>
        /// <param name="hours">Write lease duration</param>
        public CanWriteInputDataFormat(string manipulatorID, bool canWrite, float hours)
        {
            manipulator_id = manipulatorID;
            can_write = canWrite;
            this.hours = hours;
        }
    }

    /// <summary>
    ///     Movement argument format.
    /// </summary>
    public struct GotoPositionInputDataFormat
    {
        public string manipulator_id;
        public float[] pos;
        public float speed;

        /// <summary>
        ///     Construct a new goto_pos event argument.
        /// </summary>
        /// <param name="manipulatorID">ID of the manipulator to move</param>
        /// <param name="pos">Position in μm of the manipulator (in needle coordinates)</param>
        /// <param name="speed">How fast to move the manipulator (in μm/s)</param>
        public GotoPositionInputDataFormat(string manipulatorID, Vector4 pos, float speed)
        {
            manipulator_id = manipulatorID;
            this.pos = new[] { pos.x, pos.y, pos.z, pos.w };
            this.speed = speed;
        }
    }

    /// <summary>
    ///     Depth driving argument format.
    /// </summary>
    public struct DriveToDepthInputDataFormat
    {
        public string manipulator_id;
        public float depth;
        public float speed;

        /// <summary>
        ///     Construct a new drive_to_depth event argument.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to move</param>
        /// <param name="depth">Depth in μm of the manipulator (in needle coordinates)</param>
        /// <param name="speed">How fast to drive the manipulator (in μm/s)</param>
        public DriveToDepthInputDataFormat(string manipulatorId, float depth, float speed)
        {
            manipulator_id = manipulatorId;
            this.depth = depth;
            this.speed = speed;
        }
    }

    /// <summary>
    ///     Inside brain state argument format.
    /// </summary>
    public struct InsideBrainInputDataFormat
    {
        public string manipulator_id;
        public bool inside;

        /// <summary>
        ///     Construct a new inside_brain event argument.
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to set the state of</param>
        /// <param name="inside">State to set to</param>
        public InsideBrainInputDataFormat(string manipulatorId, bool inside)
        {
            manipulator_id = manipulatorId;
            this.inside = inside;
        }
    }

    #endregion

    #region Callback Parameters Data Format (Output)

    // FIXME: Dependent on Manipulator Type. Should be standardized by Ephys Link.
    /// <summary>
    ///     Returned callback data format containing available manipulator IDs and error message.
    /// </summary>
    public struct GetManipulatorsCallbackParameters
    {
        public string[] manipulators;
        public int num_axes;
        public float[] dimensions;
        public string error;
    }

    /// <summary>
    ///     Returned callback data format from positional data.
    /// </summary>
    public struct PositionalCallbackParameters
    {
        public float[] position;
        public string error;
    }

    /// <summary>
    ///     Returned callback data format from angular data.
    /// </summary>
    public struct AngularCallbackParameters
    {
        public float[] angles;
        public string error;
    }

    /// <summary>
    ///     Returned callback data format from driving to depth.
    /// </summary>
    public struct DriveToDepthCallbackParameters
    {
        public float depth;
        public string error;
    }

    /// <summary>
    ///     Returned callback data for a state-based event.
    /// </summary>
    public struct StateCallbackParameters
    {
        public bool state;
        public string error;
    }

    #endregion

#pragma warning restore CS0649
}