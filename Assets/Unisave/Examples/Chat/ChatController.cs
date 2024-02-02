using Unisave.Broadcasting;
using Unisave.Examples.Chat.Backend;
using Unisave.Facets;
using UnityEngine;
using UnityEngine.UI;

namespace Unisave.Examples.Chat
{
    public class ChatController : UnisaveBroadcastingClient
    {
        private const string Purple = "#de41b0";
        private const string Blue = "#22cbc5";
        
        private string playerName;
        private string roomId;
        
        // set up via the inspector
        public InputField messageField;
        public Text chatLogText;
        public Button sendButton;
        public GameObject connectingSpinner;

        private void Start()
        {
            // register the button click
            sendButton.onClick.AddListener(OnSendClicked);
        }

        /// <summary>
        /// Called by the ChatController.cs just before the chat room
        /// screen is displayed. It just sets up the connection parameters.
        /// </summary>
        public void SetPlayerAndRoom(string player, string room)
        {
            playerName = player;
            roomId = room;
        }
        
        /// <summary>
        /// This method is called when the game object becomes active
        /// (when the chat room screen is shown). It requests the server
        /// to join the broadcasting channel for the chat room,
        /// receives the channel subscription, and starts consuming messages
        /// coming in through the channel.
        /// </summary>
        private async void OnEnable()
        {
            ShowSpinner();
            
            // clear the chat log
            chatLogText.text = "";

            // request the server to join the broadcasting channel
            // for the chat room
            ChannelSubscription subscription = await this.CallFacet(
                (ChatFacet f) => f.JoinRoom(roomId, playerName)
            );
            
            // forward messages from the subscription into corresponding methods
            FromSubscription(subscription)
                .Forward<PlayerJoinedMessage>(PlayerJoined)
                .Forward<ChatMessage>(ChatMessageReceived)
                .ElseLogWarning(); // for unknown message type
            
            HideSpinner();
        }

        void PlayerJoined(PlayerJoinedMessage msg)
        {
            chatLogText.text += $"<color={Purple}>[server]: {msg.playerName} " +
                                $"has joined the room.</color>\n\n";
        }

        void ChatMessageReceived(ChatMessage msg)
        {
            chatLogText.text += $"<color={Blue}>[{msg.playerName}]:</color> " +
                                $"{msg.message}\n\n";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) && gameObject.activeInHierarchy)
                OnSendClicked();
        }

        private void OnSendClicked()
        {
            // do nothing if there's nothing to send
            if (string.IsNullOrWhiteSpace(messageField.text))
                return;
            
            // clear the message field and remember the message
            string message = messageField.text;
            messageField.text = "";
            
            // call the server that a message needs to be sent
            this.CallFacet((ChatFacet f) =>
                f.SendMessage(roomId, playerName, message)
            );
        }

        protected override void OnConnectionLost()
        {
            chatLogText.text += $"<color={Purple}>[server]: " +
                                $"Connection lost, reconnecting...</color>\n\n";
            ShowSpinner();
        }

        protected override void OnConnectionRegained()
        {
            chatLogText.text += $"<color={Purple}>[server]: " +
                                $"Connection established.</color>\n\n";
            HideSpinner();
        }

        private void ShowSpinner()
        {
            connectingSpinner.SetActive(true);
            chatLogText.gameObject.SetActive(false);
            messageField.DeactivateInputField();
            sendButton.interactable = false;
        }

        private void HideSpinner()
        {
            connectingSpinner.SetActive(false);
            chatLogText.gameObject.SetActive(true);
            messageField.ActivateInputField();
            sendButton.interactable = true;
        }
    }
}