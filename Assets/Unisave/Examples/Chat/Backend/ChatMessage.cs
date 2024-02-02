using Unisave.Broadcasting;

namespace Unisave.Examples.Chat.Backend
{
    /// <summary>
    /// A broadcasting message representing an actual chat message
    /// sent by someone
    /// </summary>
    public class ChatMessage : BroadcastingMessage
    {
        /// <summary>
        /// Name of the player who send the message
        /// </summary>
        public string playerName;
        
        /// <summary>
        /// Content of the message
        /// </summary>
        public string message;
    }
}