namespace Unisave.Broadcasting
{
    public enum BroadcastingConnection
    {
        /// <summary>
        /// No connection exists, because no connection is needed
        /// </summary>
        Disconnected,
        
        /// <summary>
        /// Establishing new connection
        /// </summary>
        Connecting,
        
        /// <summary>
        /// Connected, listening for events
        /// </summary>
        Connected,
        
        /// <summary>
        /// The connection unexpectedly broke, we're trying to reconnect
        /// </summary>
        Reconnecting
    }
}