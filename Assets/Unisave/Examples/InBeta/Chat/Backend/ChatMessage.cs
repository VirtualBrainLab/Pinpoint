using Unisave.Broadcasting;

namespace Unisave.Examples.InBeta.Chat.Backend
{
    public class ChatMessage : BroadcastingMessage
    {
        public string userName;
        public string message;
    }
}