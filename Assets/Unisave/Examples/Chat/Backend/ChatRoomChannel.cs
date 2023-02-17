using Unisave.Broadcasting;

namespace Unisave.Examples.Chat.Backend
{
    public class ChatRoomChannel : BroadcastingChannel
    {
        public SpecificChannel WithParameters(string roomName)
        {
            return SpecificChannel.From<ChatRoomChannel>(roomName);
        }
    }
}