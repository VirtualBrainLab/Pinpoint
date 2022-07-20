// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global

namespace SensapexLink
{
#pragma warning disable CS0649

    #region Input Data Format

    /// <summary>
    /// Enable/Disable write access to the server event argument format
    /// </summary>
    public struct CanWriteInputDataFormat
    {
        public int manipulator_id;
        public bool can_write;
        public float hours;

        public CanWriteInputDataFormat(int manipulatorID, bool canWrite, float hours)
        {
            manipulator_id = manipulatorID;
            can_write = canWrite;
            this.hours = hours;
        }
    }

    #endregion

    #region Callback Data Format

    /// <summary>
    /// Returned callback data format containing available manipulator IDs and error message
    /// </summary>
    public struct GetManipulatorsCallbackParameters
    {
        public int[] manipulators;
        public string error;
    }

    /// <summary>
    /// Returned callback data format containing ID and error message
    /// </summary>
    public struct IdCallbackParameters
    {
        public int manipulator_id;
        public string error;
    }

    /// <summary>
    /// Returned callback data format from positional data
    /// </summary>
    public struct PositionalCallbackParameters
    {
        public int manipulator_id;
        public float[] position;
        public string error;
    }

    #endregion

#pragma warning restore CS0649
}