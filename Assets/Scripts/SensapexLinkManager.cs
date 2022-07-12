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

    private void Awake()
    {
        // Get access to everything else
        GameObject main = GameObject.Find("Main");
    }
    // Start is called before the first frame update
    void Start()
    {
        // Create connection to server
        var connectionManager = new SocketManager(new System.Uri("http://" + _serverIp + ":" + _serverPort));
        connectionManager.Socket.On("connect", () => Debug.Log(connectionManager.Handshake.Sid));

    }

    // Update is called once per frame
    void Update()
    {

    }
}
