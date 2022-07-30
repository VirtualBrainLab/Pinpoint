using Unisave.Broadcasting;

namespace Unisave.Examples.InBeta.Chat.Backend
{
    public class PlayerJoinedMessage : BroadcastingMessage
    {
        public string userName;
    }
}