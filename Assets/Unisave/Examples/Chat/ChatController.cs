using Unisave.Broadcasting;
using Unisave.Examples.Chat.Backend;
using Unisave.Facades;
using UnityEngine.UI;

namespace Unisave.Examples.Chat
{
    public class ChatController : UnisaveBroadcastingClient
    {
        public string userName;
        public string roomName;

        public InputField chatLogField;
        public InputField messageField;
        public Button sendButton;

        private void Start()
        {
            sendButton.onClick.AddListener(OnSendClicked);
        }
        
        private async void OnEnable()
        {
            var subscription = await OnFacet<ChatFacet>
                .CallAsync<ChannelSubscription>(
                    nameof(ChatFacet.JoinRoom),
                    roomName,
                    userName
                );
            
            FromSubscription(subscription)
                .Forward<ChatMessage>(ChatMessageReceived)
                .Forward<PlayerJoinedMessage>(PlayerJoined)
                .ElseLogWarning();
        }

        private async void OnSendClicked()
        {
            await OnFacet<ChatFacet>.CallAsync(
                nameof(ChatFacet.SendMessage),
                roomName,
                userName,
                messageField.text
            );
        }

        void ChatMessageReceived(ChatMessage msg)
        {
            chatLogField.text += $"[{msg.userName}]: {msg.message}\n";
        }

        void PlayerJoined(PlayerJoinedMessage msg)
        {
            chatLogField.text += $"{msg.userName} joined the room\n";
        }

        protected override void OnConnectionLost()
        {
            chatLogField.text += "Connection lost, reconnecting...\n";
        }

        protected override void OnConnectionRegained()
        {
            chatLogField.text += "Connection established.\n";
        }
    }
}