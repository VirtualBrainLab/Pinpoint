using System;
using BestHTTP.SocketIO3;
using UnityEngine;

namespace SensapexLink
{
    /// <summary>
    /// WebSocket connection manager between the Trajectory Planner and a running Sensapex Link server
    /// </summary>
    public class CommunicationManager : MonoBehaviour
    {
        #region Variables

        // Connection details
        [SerializeField] private string serverIp = "10.18.251.95";
        [SerializeField] private ushort serverPort = 8080;
        [SerializeField] private bool calibrateOnConnect;

        // Components
        private SocketManager _connectionManager;
        private TP_TrajectoryPlannerManager _trajectoryPlannerManager;
        private NeedlesTransform _neTransform;

        // Manipulator things
        private float[] _zeroPosition;

        #endregion

        #region Setup

        private void Awake()
        {
            // Get access to everything else
            var main = GameObject.Find("main");
            _trajectoryPlannerManager = main.GetComponent<TP_TrajectoryPlannerManager>();
        }

        void Start()
        {
            // Create connection to server
            _connectionManager = new SocketManager(new Uri("http://" + serverIp + ":" + serverPort));
            _connectionManager.Socket.On("connect", () => Debug.Log(_connectionManager.Handshake.Sid));

            // Instantiate components
            _neTransform = new NeedlesTransform();

            RegisterManipulator(1, () => GetPos(1, pos => Debug.Log(pos)));

            // Register manipulators
            // _connectionManager.Socket.Emit("register_manipulator", 1);
            // _connectionManager.Socket.Emit("set_can_write", new CanWriteInputDataFormat(1, true, 1));
            // _connectionManager.Socket.ExpectAcknowledgement<IdCallbackParameters>(_HandleCalibration)
            //     .Emit(calibrateOnConnect ? "calibrate" : "bypass_calibration", 1);
        }

        #endregion

        #region Event senders

        /// <summary>
        /// Get manipulators event sender
        /// </summary>
        /// <param name="callback">Callback function to handle incoming manipulator ID's</param>
        /// <param name="error">Callback function to handle errors</param>
        public void GetManipulators(Action<int[]> callback, Action<string> error = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<GetManipulatorsCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    callback(data.manipulators);
                }
                else
                {
                    error?.Invoke(data.error);
                    Debug.LogError(data.error);
                }
            }).Emit("get_manipulators");
        }

        /// <summary>
        /// Register a manipulator with the server
        /// </summary>
        /// <param name="manipulatorId">The ID of the manipulator to register</param>
        /// <param name="callback">Callback function to handle a successful registration</param>
        /// <param name="error">Callback function to handle errors</param>
        public void RegisterManipulator(int manipulatorId, Action callback = null, Action<string> error = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<IdCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    callback?.Invoke();
                }
                else
                {
                    error?.Invoke(data.error);
                    Debug.LogError(data.error);
                }
            }).Emit("register_manipulator", manipulatorId);
        }

        /// <summary>
        /// Request the current position of a manipulator
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to get the position of</param>
        /// <param name="callback">Callback function to handle receiving the position</param>
        /// <param name="error">Callback function to handle error getting the position</param>
        public void GetPos(int manipulatorId, Action<Vector4> callback, Action<string> error = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<PositionalCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    callback(new Vector4(data.position[0], data.position[1], data.position[2], data.position[3]));
                }
                else
                {
                    error?.Invoke(data.error);
                    Debug.LogError(data.error);
                }
            }).Emit("get_pos", manipulatorId);
        }

        /// <summary>
        /// Request a manipulator be moved to a specific position defined by a vector4
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to be moved</param>
        /// <param name="pos">Position in μm of the manipulator (in needle coordinates)</param>
        /// <param name="speed">How fast to move the manipulator (in μm/s)</param>
        /// <param name="callback">Callback function to handle a successful move</param>
        /// <param name="error">Callback function to handle an unsuccessful move</param>
        public void GotoPos(int manipulatorId, Vector4 pos, int speed, Action<Vector4> callback,
            Action<string> error = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<PositionalCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    callback(new Vector4(data.position[0], data.position[1], data.position[2], data.position[3]));
                }
                else
                {
                    error?.Invoke(data.error);
                    Debug.LogError(data.error);
                }
            }).Emit("goto_pos", new GotoPositionInputDataFormat(manipulatorId, pos, speed));
        }

        /// <summary>
        /// Request a manipulator be moved to a specific position defined by an array of 4 floats
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to be moved</param>
        /// <param name="pos">Position in μm of the manipulator (in needle coordinates)</param>
        /// <param name="speed">How fast to move the manipulator (in μm/s)</param>
        /// <param name="callback">Callback function to handle a successful move</param>
        /// <param name="error">Callback function to handle an unsuccessful move</param>
        /// <exception cref="ArgumentException">If the given position is not in an array of 4 floats</exception>
        public void GotoPos(int manipulatorId, float[] pos, int speed, Action<Vector4> callback,
            Action<string> error = null)
        {
            if (pos.Length != 4)
            {
                throw new ArgumentException("Position array must be of length 4");
            }

            GotoPos(manipulatorId, new Vector4(pos[0], pos[1], pos[2], pos[3]), speed, callback, error);
        }

        /// <summary>
        /// Request a manipulator drive down to a specific depth
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to move</param>
        /// <param name="depth">Depth in μm of the manipulator (in needle coordinates)</param>
        /// <param name="speed">How fast to drive the manipulator (in μm/s)</param>
        /// <param name="callback">Callback function to handle a successful move</param>
        /// <param name="error">Callback function to handle an unsuccessful move</param>
        public void DriveToDepth(int manipulatorId, float depth, int speed, Action<float> callback,
            Action<string> error)
        {
            _connectionManager.Socket.ExpectAcknowledgement<DriveToDepthCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    callback(data.depth);
                }
                else
                {
                    error?.Invoke(data.error);
                    Debug.LogError(data.error);
                }
            }).Emit("drive_to_depth", new DriveToDepthInputDataFormat(manipulatorId, depth, speed));
        }

        #endregion

        #region Event Handlers

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
                Debug.Log(
                    data.position[0] + "\t" + data.position[1] + "\t" + data.position[2] + "\t" + data.position[3]);

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

        #endregion
    }
}