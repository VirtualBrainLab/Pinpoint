using System;
using LightJson;
using Unisave.Broadcasting.Sse;
using Unisave.Foundation;
using UnityEngine;
using Application = UnityEngine.Application;

namespace Unisave.Broadcasting
{
    /// <summary>
    /// The tunnel that transports events from the server to the client
    /// (all the channels combined with all the metadata)
    /// </summary>
    public class BroadcastingTunnel : IDisposable
    {
        /// <summary>
        /// Called when a message event arrives through the SSE tunnel
        /// </summary>
        public event Action<JsonObject> OnMessageEvent;
        
        /// <summary>
        /// Called when a subscription event arrives through the SSE tunnel
        /// </summary>
        public event Action<JsonObject> OnSubscriptionEvent;
        
        /// <summary>
        /// Called when the server connection is lost
        /// </summary>
        public event Action OnConnectionLost;
        
        /// <summary>
        /// Called when the server connection is regained
        /// </summary>
        public event Action OnConnectionRegained;

        private readonly ClientApplication app;

        /// <summary>
        /// The underlying SSE socket. Can be null when not needed.
        /// </summary>
        public ISseSocket Socket { get; private set; }

        /// <summary>
        /// The game object that owns the socket
        /// (if the socket is a MonoBehaviour, otherwise null)
        /// </summary>
        public GameObject SocketGameObject { get; private set; }

        /// <summary>
        /// Status of the connection to the Unisave broadcasting server
        /// </summary>
        public BroadcastingConnection ConnectionState
            => Socket == null
                ? BroadcastingConnection.Disconnected
                : Socket.ConnectionState;

        public BroadcastingTunnel(ClientApplication app)
        {
            this.app = app;
        }

        /// <summary>
        /// Called just before the tunnel becomes needed
        /// (idempotent)
        /// </summary>
        public void IsNeeded()
        {
            if (Socket == null)
                CreateSocket();
        }

        /// <summary>
        /// Called right after the tunnel stops being needed
        /// (idempotent)
        /// </summary>
        public void IsNotNeeded()
        {
            DisposeSocket();
        }

        private void CreateSocket()
        {
            if (Socket != null)
                throw new InvalidOperationException("Socket already created");

            SocketGameObject = new GameObject("UnisaveBroadcastingSseSocket");
            UnityEngine.Object.DontDestroyOnLoad(SocketGameObject);

            if (!app.InEditMode)
                SocketGameObject.transform.parent = app.GameObject.transform;
            
#if UNITY_WEBGL && !UNITY_EDITOR
            Socket = SocketGameObject.AddComponent<WebGlSseSocket>();
#else
            Socket = SocketGameObject.AddComponent<DefaultSseSocket>();
#endif

            Socket.Initialize(app);
            
            Socket.OnEventReceived += OnEventReceived;
            Socket.OnConnectionLost += ConnectionLostDelegator;
            Socket.OnConnectionRegained += ConnectionRegainedDelegator;
        }

        private void DisposeSocket()
        {
            if (Socket == null)
                return;
            
            Socket.OnEventReceived -= OnEventReceived;
            Socket.OnConnectionLost -= ConnectionLostDelegator;
            Socket.OnConnectionRegained -= ConnectionRegainedDelegator;
            
            if (SocketGameObject != null)
                UnityEngine.Object.Destroy(SocketGameObject);
            
            Socket = null;
            SocketGameObject = null;
        }

        private void ConnectionLostDelegator()
            => OnConnectionLost?.Invoke();
        
        private void ConnectionRegainedDelegator()
            => OnConnectionRegained?.Invoke();

        private void OnEventReceived(SseEvent @event)
        {
            switch (@event.@event)
            {
                case "message":
                    OnMessageEvent?.Invoke(@event.jsonData);
                    break;
                
                case "subscription":
                    OnSubscriptionEvent?.Invoke(@event.jsonData);
                    break;
                
                case "welcome":
                    // do nothing
                    // (used by the SseSocket to detect
                    // connection establishment)
                    break;
                
                case "heartbeat":
                    // do nothing
                    break;
                
                case "end-connection":
                    // do nothing
                    // (used by the SseSocket to detect
                    // server-requested disconnection)
                    break;
                
                default:
                    Debug.LogWarning(
                        "[Unisave] Unknown broadcasting event received: " +
                        @event.@event
                    );
                    break;
            }
        }

        public void Dispose()
        {
            DisposeSocket();
        }
    }
}