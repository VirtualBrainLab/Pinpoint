using Unisave.Broadcasting;

namespace Unisave.Examples.Chat.Backend
{
    /// <summary>
    /// A broadcasting message sent when someone joins the chat room
    /// </summary>
    public class PlayerJoinedMessage : BroadcastingMessage
    {
        /// <summary>
        /// Name of the player who joined the chat room
        /// </summary>
        public string playerName;
    }
}