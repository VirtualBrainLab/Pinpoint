using System;
using BestHTTP.SocketIO3;
using Models.Automation;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;
using UnityEngine;
using Action = System.Action;

namespace Services
{
    public class EphysLinkService
    {
        #region Constants

        private const string UNKOWN_EVENT_RESPONSE = "{\"error\": \"Unknown event.\"}";

        #endregion

        #region Services

        [Service]
        private StoreService _storeService;

        #endregion

        #region Components

        private SocketManager _socketManager;
        private Socket _socket;

        #endregion

        #region Connection Handling


        /// <summary>
        ///     Create a connection to the server.
        /// </summary>
        /// <param name="ip">IP address of the server</param>
        /// <param name="port">Port of the server</param>
        /// <param name="onConnected">Callback function to handle a successful connection</param>
        /// <param name="onError"></param>
        public void ConnectToServer(
            string ip,
            int port,
            Action onConnected = null,
            System.Action<string> onError = null
        )
        {
            // Disconnect the old connection if needed
            if (_socketManager != null && _socketManager.Socket.IsOpen)
                _socketManager.Close();

            // Create new connection
            var options = new SocketOptions { Timeout = new TimeSpan(0, 0, 2) };

            // Try to open a connection
            try
            {
                // Create a new socket
                _socketManager = new SocketManager(new Uri($"http://{ip}:{port}"), options);
                _socket = _socketManager.Socket;

                // On successful connection
                _socket.Once(
                    "connect",
                    () =>
                    {
                        _storeService.Store.Dispatch(EphysLinkActions.SET_IS_CONNECTED, true);
                        onConnected?.Invoke();
                    }
                );

                // On error
                _socket.Once("error", () => HandleError(GetErrorConnectingToServerMessage()));

                // On timeout
                _socket.Once("connect_timeout", () => HandleError(GetConnectionTimeoutMessage()));
            }
            catch (Exception e)
            {
                HandleError($"{GetErrorConnectingToServerMessage()} Caused exception: {e}");
            }

            return;

            string GetErrorConnectingToServerMessage() =>
                $"Error connecting to server at {ip}:{port}. Check server for details.";
            string GetConnectionTimeoutMessage() =>
                $"Connection to server at {ip}:{port} timed out.";

            void HandleDisconnect()
            {
                _storeService.Store.Dispatch(EphysLinkActions.SET_IS_CONNECTED, false);
                _socketManager?.Close();
                _socketManager = null;
                _socket = null;
            }

            void HandleError(string message)
            {
                HandleDisconnect();
                onError?.Invoke(message);
            }
        }

        #endregion
    }
}
