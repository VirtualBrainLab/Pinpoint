using Unisave.Broadcasting;

namespace Unisave.Examples.Chat.Backend
{
    public class ChatMessage : BroadcastingMessage
    {
        public string userName;
        public string message;
    }
}