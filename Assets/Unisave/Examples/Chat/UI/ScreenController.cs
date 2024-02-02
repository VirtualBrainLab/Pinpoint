using UnityEngine;

namespace Unisave.Examples.Chat.UI
{
    public class ScreenController : MonoBehaviour
    {
        // set up via the inspector
        public GameObject lobbyScreen;
        public GameObject chatRoomScreen;

        public void ShowChatRoomScreen()
        {
            chatRoomScreen.SetActive(true);
            lobbyScreen.SetActive(false);
        }
        
        public void ShowLobbyScreen()
        {
            lobbyScreen.SetActive(true);
            chatRoomScreen.SetActive(false);
        }
    }
}