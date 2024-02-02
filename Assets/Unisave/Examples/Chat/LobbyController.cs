using Unisave.Examples.Chat.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Unisave.Examples.Chat
{
    public class LobbyController : MonoBehaviour
    {
        // set up via the inspector
        public InputField playerNameField;
        public InputField roomIdField;
        public Button joinButton;
        public ChatController chatController;
        public ScreenController screenController;
        
        private void Start()
        {
            // register the button click
            joinButton.onClick.AddListener(JoinTheRoom);
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) && gameObject.activeInHierarchy)
                JoinTheRoom();
        }

        private void JoinTheRoom()
        {
            chatController.SetPlayerAndRoom(
                player: playerNameField.text,
                room: roomIdField.text
            );
            
            screenController.ShowChatRoomScreen();
        }
    }
}
