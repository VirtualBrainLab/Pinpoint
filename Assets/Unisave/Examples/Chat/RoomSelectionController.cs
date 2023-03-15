using UnityEngine;
using UnityEngine.UI;

namespace Unisave.Examples.Chat
{
    public class RoomSelectionController : MonoBehaviour
    {
        public InputField userNameField;
        public InputField roomNameField;
        public Button joinRoomButton;

        public ChatController chatController;

        public GameObject roomSelectionScreen;
        public GameObject roomScreen;
        
        // Start is called before the first frame update
        void Start()
        {
            joinRoomButton.onClick.AddListener(JoinTheRoom);
        }

        private void JoinTheRoom()
        {
            chatController.userName = userNameField.text;
            chatController.roomName = roomNameField.text;
            chatController.gameObject.SetActive(true);
            
            roomSelectionScreen.SetActive(false);
            roomScreen.SetActive(true);
        }
    }
}
