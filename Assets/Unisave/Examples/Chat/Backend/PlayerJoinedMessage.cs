using Unisave.Broadcasting;

namespace Unisave.Examples.Chat.Backend
{
    public class PlayerJoinedMessage : BroadcastingMessage
    {
        public string userName;
    }
}