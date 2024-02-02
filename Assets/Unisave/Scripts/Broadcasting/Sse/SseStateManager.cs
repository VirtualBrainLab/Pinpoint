using System;

namespace Unisave.Broadcasting.Sse
{
    /// <summary>
    /// Manages the state of an SSE Socket
    /// </summary>
    public class SseStateManager
    {
        // socket event invocation callbacks
        private readonly Action invokeConnectionLost;
        private readonly Action invokeConnectionRegained;
        
        /// <summary>
        /// Backing field for the current state of the connection
        /// </summary>
        private BroadcastingConnection connectionState = BroadcastingConnection.Disconnected;
        
        /// <summary>
        /// Id of the last received SSE event
        /// (updated with each received event,
        /// used during connection establishment)
        /// </summary>
        public int LastReceivedEventId { get; set; } = SseEvent.NullEventId;
        
        /// <summary>
        /// How long to wait for in between during connection retrying
        /// </summary>
        public int RetryMilliseconds { get; set; } = 5_000;
        
        public SseStateManager(
            Action invokeConnectionLost,
            Action invokeConnectionRegained
        )
        {
            this.invokeConnectionLost = invokeConnectionLost;
            this.invokeConnectionRegained = invokeConnectionRegained;
        }

        /// <summary>
        /// Returns the current connection state
        /// </summary>
        public BroadcastingConnection GetConnectionState()
        {
            return connectionState;
        }

        /// <summary>
        /// Sets the connection state and invokes events accordingly. Is idempotent.
        /// </summary>
        /// <param name="value">The new connection state</param>
        public void SetConnectionState(BroadcastingConnection value)
        {
            if (connectionState == value)
                return;
            
            var before = connectionState;
            
            connectionState = value;
            
#if UNISAVE_BROADCASTING_DEBUG
            UnityEngine.Debug.Log($"[UnisaveBroadcasting] ConnectionState = {connectionState}");
#endif
            
            if (value == BroadcastingConnection.Reconnecting)
                invokeConnectionLost.Invoke();
            
            if (value == BroadcastingConnection.Connected
                && before == BroadcastingConnection.Reconnecting)
                invokeConnectionRegained.Invoke();
        }

        /// <summary>
        /// Called by the SSE socket to update the state according to a received SSE event
        /// </summary>
        /// <param name="event"></param>
        public void ObserveReceivedEvent(SseEvent @event)
        {
            if (@event.id != SseEvent.NullEventId)
                LastReceivedEventId = @event.id;

            if (@event.retry != null)
                RetryMilliseconds = (int) @event.retry;
            
            // handle start of connection
            if (@event.@event == "welcome")
            {
                SetConnectionState(BroadcastingConnection.Connected);
            }

            // handle end of connection
            if (@event.@event == "end-connection")
            {
                LastReceivedEventId = SseEvent.NullEventId;
            }
        }
    }
}