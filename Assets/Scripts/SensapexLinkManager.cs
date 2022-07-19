using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using BestHTTP.SocketIO3;

/// <summary>
/// WebSocket connection manager between the Trajectory Planner and a running Sensapex Link server
/// </summary>
public class SensapexLinkManager : MonoBehaviour
{
    // Connection details
    [SerializeField] private string serverIp = "10.18.251.95";
    [SerializeField] private ushort serverPort = 8080;
    [SerializeField] private bool calibrateOnConnect = false;

    // Components
    private SocketManager _connectionManager;
    private TP_TrajectoryPlannerManager _trajectoryPlannerManager;
    private NeedlesTransform _neTransform;

    // Manipulator things
    private float[] _zeroPosition;

    private void Awake()
    {
        // Get access to everything else
        GameObject main = GameObject.Find("main");
        _trajectoryPlannerManager = main.GetComponent<TP_TrajectoryPlannerManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Create connection to server
        _connectionManager = new SocketManager(new System.Uri("http://" + serverIp + ":" + serverPort));
        _connectionManager.Socket.On("connect", () => Debug.Log(_connectionManager.Handshake.Sid));

        // Instantiate components
        _neTransform = new NeedlesTransform();

        // Register manipulators
        _connectionManager.Socket.Emit("register_manipulator", 1);
        _connectionManager.Socket.Emit("set_can_write", new CanWriteInputDataFormat(1, true, 1));
        _connectionManager.Socket.ExpectAcknowledgement<IdCallbackParameters>(_HandleCalibration)
            .Emit(calibrateOnConnect ? "calibrate" : "bypass_calibration", 1);
    }

    /// <summary>
    /// Handle post-calibration state
    /// </summary>
    /// <param name="data">Formatted callback parameter from calibration</param>
    private void _HandleCalibration(IdCallbackParameters data)
    {
        if (data.error == "")
        {
            _connectionManager.Socket.ExpectAcknowledgement<PositionalCallbackParameters>(_SetZeroPosition)
                .Emit("get_pos", 1);
        }
        else
        {
            Debug.LogError(data.error);
        }
    }

    /// <summary>
    /// Record needle coordinates for bregma
    /// </summary>
    /// <param name="data">Formatted callback parameters from getting position</param>
    private void _SetZeroPosition(PositionalCallbackParameters data)
    {
        if (data.error == "")
        {
            _zeroPosition = data.position;
        }
        else
        {
            Debug.LogError(data.error);
        }

        _connectionManager.Socket.ExpectAcknowledgement<PositionalCallbackParameters>(_GetPosCallbackHandler)
            .Emit("get_pos", 1);
    }

    /// <summary>
    /// get_pos event callback handler
    /// </summary>
    /// <para>
    /// Reads the returned data for errors and then prints it back out. Calls another get_pos event at the end.
    /// </para>
    /// <param name="data">Formatted callback parameters for getting position</param>
    private void _GetPosCallbackHandler(PositionalCallbackParameters data)
    {
        if (data.error == "")
        {
            // Convert to CCF
            Debug.Log(data.position[0] + "\t" + data.position[1] + "\t" + data.position[2] + "\t" + data.position[3]);

            var ccf = _neTransform.ToCCF(new Vector3(data.position[0] - _zeroPosition[0],
                data.position[1] - _zeroPosition[1],
                data.position[2] - _zeroPosition[2]));

            try
            {
                // Get current coordinates
                var curCoordinates = _trajectoryPlannerManager.GetActiveProbeController().GetCoordinates();

                // Manually set probe coordinates
                _trajectoryPlannerManager.GetActiveProbeController().ManualCoordinateEntry(ccf.x, ccf.y, ccf.z,
                    data.position[3] - _zeroPosition[3], curCoordinates.Item5, curCoordinates.Item6,
                    curCoordinates.Item7);
            }
            catch
            {
                Debug.Log("No active probe yet");
            }
        }
        else
        {
            Debug.LogError(data.error);
        }

        _connectionManager.Socket.ExpectAcknowledgement<PositionalCallbackParameters>(_GetPosCallbackHandler)
            .Emit("get_pos", 1);
    }

    /// <summary>
    /// Enable/Disable write access to the server event argument format
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    private struct CanWriteInputDataFormat
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

    /// <summary>
    /// Returned callback data format containing ID and error message
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private struct IdCallbackParameters
    {
#pragma warning disable CS0649
        public int manipulator_id;
        public string error;
#pragma warning restore CS0649
    }

    /// <summary>
    /// Returned callback data format from positional data
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private struct PositionalCallbackParameters
    {
#pragma warning disable CS0649
        public int manipulator_id;
        public float[] position;
        public string error;
#pragma warning restore CS0649
    }
}