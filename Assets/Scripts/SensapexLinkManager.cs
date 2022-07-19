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

    // Components
    private SocketManager _connectionManager;
    private TP_TrajectoryPlannerManager _trajectoryPlannerManager;
    private NeedlesTransform _neTransform;

    // Manipulator things
    private Vector3 _zeroPosition;

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
        _connectionManager.Socket.Emit("bypass_calibration", 1);
        _connectionManager.Socket.ExpectAcknowledgement<GetPositionCallbackParameters>(_SetZeroPosition)
            .Emit("get_pos", 1);
    }

    /// <summary>
    /// Record needle coordinates for bregma
    /// </summary>
    /// <param name="data">Formatted callback parameters for getting position</param>
    private void _SetZeroPosition(GetPositionCallbackParameters data)
    {
        if (data.error == "")
        {
            _zeroPosition = new Vector3(data.position[0], data.position[1], data.position[2]);
        }
        else
        {
            Debug.LogError(data.error);
        }

        _connectionManager.Socket.ExpectAcknowledgement<GetPositionCallbackParameters>(_GetPosCallbackHandler)
            .Emit("get_pos", 1);
    }

    /// <summary>
    /// get_pos event callback handler
    /// </summary>
    /// <para>
    /// Reads the returned data for errors and then prints it back out. Calls another get_pos event at the end.
    /// </para>
    /// <param name="data">Formatted callback parameters for getting position</param>
    private void _GetPosCallbackHandler(GetPositionCallbackParameters data)
    {
        if (data.error == "")
        {
            // Convert to CCF
            var positionVector = new Vector3(data.position[0], data.position[1], data.position[2]);
            var ccf = _neTransform.ToCCF(positionVector - _zeroPosition);
            try
            {
                // Get current coordinates
                var curCoordinates = _trajectoryPlannerManager.GetActiveProbeController().GetCoordinates();

                _trajectoryPlannerManager.GetActiveProbeController().ManualCoordinateEntry(ccf.x, ccf.y, ccf.z,
                    curCoordinates.Item4, curCoordinates.Item5, curCoordinates.Item6, curCoordinates.Item7);
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

        _connectionManager.Socket.ExpectAcknowledgement<GetPositionCallbackParameters>(_GetPosCallbackHandler)
            .Emit("get_pos", 1);
    }

    /// <summary>
    /// Returned callback data format from get_pos
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private struct GetPositionCallbackParameters
    {
#pragma warning disable CS0649
        public int manipulator_id;
        public float[] position;
        public string error;
#pragma warning restore CS0649
    }
}