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

        // Register manipulators
        connectionManager.Socket.Emit("register_manipulator", 1);
        connectionManager.Socket.Emit("bypass_calibration", 1);
        connectionManager.Socket.ExpectAcknowledgement<GetPositionReturnData>(_GetPosCallbackHandler).Emit("get_pos", 1);
    }

    /// <summary>
    /// get_pos event callback handler
    /// </summary>
    /// <para>
    /// Reads the returned data for errors and then prints it back out. Calls another get_pos event at the end.
    /// </para>
    /// <param name="data">Formatted callback parameters for getting position</param>
    private void _GetPosCallbackHandler(GetPositionReturnData data)
    {
        if (data.error == "")
        {
            Debug.Log("Manipulator " + data.manipulator_id + ": (" + string.Join(", ", data.position) + ")");
        }
        else
        {
            Debug.LogError(data.error);
        }

        connectionManager.Socket.ExpectAcknowledgement<GetPositionReturnData>(_GetPosCallbackHandler).Emit("get_pos", 1);
    }

    /// <summary>
    /// Returned callback data format from get_pos
    /// </summary>
    private struct GetPositionReturnData
    {
        public int manipulator_id;
        public float[] position;
        public string error;
    }
}