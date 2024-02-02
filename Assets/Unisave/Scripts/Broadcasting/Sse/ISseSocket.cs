using System;
using Unisave.Foundation;

namespace Unisave.Broadcasting.Sse
{
    /// <summary>
    /// Represents an SSE Socket that can be used by the BroadcastingTunnel
    /// </summary>
    public interface ISseSocket
    {
        /// <summary>
        /// Called when a new event arrives
        /// </summary>
        event Action<SseEvent> OnEventReceived;
        
        /// <summary>
        /// Event called when the connection unexpectedly breaks
        /// </summary>
        event Action OnConnectionLost;
        
        /// <summary>
        /// Event called when the connection is established again
        /// </summary>
        event Action OnConnectionRegained;
        
        /// <summary>
        /// Status of the connection to the Unisave broadcasting server
        /// </summary>
        BroadcastingConnection ConnectionState { get; }

        /// <summary>
        /// Call this right after this component is created
        /// </summary>
        void Initialize(ClientApplication app);
    }
}