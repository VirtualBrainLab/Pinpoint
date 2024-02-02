using Unisave.Broadcasting;

namespace Unisave.Examples.Chat.Backend
{
    /// <summary>
    /// Defines all the broadcasting channels for chat rooms.
    /// Channels are used to send messages to their subscribed clients.
    /// </summary>
    public class ChatRoomChannel : BroadcastingChannel
    {
        /// <summary>
        /// This will get or create the one broadcasting channel
        /// for the given chat room ID
        /// </summary>
        /// <param name="roomId">The chat room ID</param>
        /// <returns>A one specific broadcasting channel</returns>
        public SpecificChannel WithParameters(string roomId)
        {
            return SpecificChannel.From<ChatRoomChannel>(roomId);
        }
    }
}