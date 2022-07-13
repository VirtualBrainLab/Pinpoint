using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO3;

/// <summary>
/// WebSocket connection manager between the Trajectory Planner and a running Sensapex Link server
/// </summary>
public class SensapexLinkManager : MonoBehaviour
{
    // Connection details
    [SerializeField] private string _serverIp = "10.18.251.95";
    [SerializeField] private ushort _serverPort = 8080;

    // Components
    private SocketManager connectionManager;
    private TP_TrajectoryPlannerManager tpmanager;
    private NeedlesTransform neTransform;

    private void Awake()
    {
        // Get access to everything else
        GameObject main = GameObject.Find("main");
        tpmanager = main.GetComponent<TP_TrajectoryPlannerManager>();
    }
    // Start is called before the first frame update
    void Start()
    {
        // Create connection to server
        connectionManager = new SocketManager(new System.Uri("http://" + _serverIp + ":" + _serverPort));
        connectionManager.Socket.On("connect", () => Debug.Log(connectionManager.Handshake.Sid));

        // Instantiate components
        neTransform = new NeedlesTransform();

        // Register manipulators
        connectionManager.Socket.Emit("register_manipulator", 1);
        connectionManager.Socket.Emit("bypass_calibration", 1);
        connectionManager.Socket.ExpectAcknowledgement<GetPositionCallbackParameters>(_GetPosCallbackHandler).Emit("get_pos", 1);
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
            var ccf = neTransform.ToCCF(positionVector);
            Debug.Log(ccf);
            // tpmanager.GetActiveProbeController().SetProbePosition(new ProbeInsertion(ccf.x, ccf.y, ccf.z, data.position[3], 0, 0, 0));
        }
        else
        {
            Debug.LogError(data.error);
        }

        connectionManager.Socket.ExpectAcknowledgement<GetPositionCallbackParameters>(_GetPosCallbackHandler).Emit("get_pos", 1);
    }

    /// <summary>
    /// Returned callback data format from get_pos
    /// </summary>
    private struct GetPositionCallbackParameters
    {
        public int manipulator_id;
        public float[] position;
        public string error;
    }
}