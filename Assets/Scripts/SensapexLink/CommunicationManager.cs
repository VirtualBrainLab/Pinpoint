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

        // Components
        private SocketManager _connectionManager;

        #endregion

        #region Setup

        void Start()
        {
            // Create connection to server
            _connectionManager = new SocketManager(new Uri("http://" + serverIp + ":" + serverPort));
            _connectionManager.Socket.On("connect", () => Debug.Log(_connectionManager.Handshake.Sid));
        }

        #endregion

        #region Event Handlers

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
        /// Request a manipulator be moved to a specific position
        /// </summary>
        /// <remarks>Position is defined by a Vector4</remarks>
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
        /// <remarks>Position is defined by an array of 4 floats</remarks>
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

        /// <summary>
        /// Set the inside brain state of a manipulator
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to set the state of</param>
        /// <param name="inside">State to set to</param>
        /// <param name="callback">Callback function to handle a successful state set</param>
        /// <param name="error">Callback function to handle an unsuccessful state set</param>
        public void SetInsideBrain(int manipulatorId, bool inside, Action<bool> callback, Action<string> error = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<StateCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    callback(data.state);
                }
                else
                {
                    error?.Invoke(data.error);
                    Debug.LogError(data.error);
                }
            }).Emit("set_inside_brain", new InsideBrainInputDataFormat(manipulatorId, inside));
        }

        /// <summary>
        /// Request a manipulator to be calibrated
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to be calibrated</param>
        /// <param name="callback">Callback function to handle a successful calibration</param>
        /// <param name="error">Callback function to handle an unsuccessful calibration</param>
        public void Calibrate(int manipulatorId, Action callback, Action<string> error = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<IdCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    callback();
                }
                else
                {
                    error?.Invoke(data.error);
                    Debug.LogError(data.error);
                }
            }).Emit("calibrate", manipulatorId);
        }

        /// <summary>
        /// Bypass calibration requirement of a manipulator
        /// </summary>
        /// <remarks>This method should only be used for testing and NEVER in production</remarks>
        /// <param name="manipulatorId">ID of the manipulator to bypass calibration</param>
        /// <param name="callback">Callback function to handle a successful calibration bypass</param>
        /// <param name="error">Callback function to handle an unsuccessful calibration bypass</param>
        public void BypassCalibration(int manipulatorId, Action callback, Action<string> error = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<IdCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    callback();
                }
                else
                {
                    error?.Invoke(data.error);
                    Debug.LogError(data.error);
                }
            }).Emit("bypass_calibration", manipulatorId);
        }

        /// <summary>
        /// Request a write lease for a manipulator
        /// </summary>
        /// <param name="manipulatorId">ID of the manipulator to allow writing</param>
        /// <param name="canWrite">Write state to set the manipulator to</param>
        /// <param name="hours">How many hours a manipulator may have a write lease</param>
        /// <param name="callback">Callback function to handle a successful write lease request</param>
        /// <param name="error">Callback function to handle an unsuccessful write lease request</param>
        public void SetCanWrite(int manipulatorId, bool canWrite, float hours, Action<bool> callback,
            Action<string> error = null)
        {
            _connectionManager.Socket.ExpectAcknowledgement<StateCallbackParameters>(data =>
            {
                if (data.error == "")
                {
                    callback(data.state);
                }
                else
                {
                    error?.Invoke(data.error);
                    Debug.LogError(data.error);
                }
            }).Emit("set_can_write", new CanWriteInputDataFormat(manipulatorId, canWrite, hours));
        }

        /// <summary>
        /// Request all movement to stop
        /// </summary>
        /// <param name="callback">Callback function to handle stop result</param>
        public void Stop(Action<bool> callback)
        {
            _connectionManager.Socket.ExpectAcknowledgement(callback).Emit("stop");
        }

        #endregion
    }
}