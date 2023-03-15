using Unisave.Authentication;
using Unisave.Broadcasting;
using Unisave.Facades;
using Unisave.Facets;

namespace Unisave.Examples.Chat.Backend
{
    public class ChatFacet : Facet
    {
        public ChannelSubscription JoinRoom(string roomName, string userName)
        {
            // verify the player can access the channel
            if (userName == "Some Banned Dude")
                throw new AuthException();

            // subscribe the client into the channel
            var subscription = Broadcast.Channel<ChatRoomChannel>()
                .WithParameters(roomName)
                .CreateSubscription();
            
            // new subscriber broadcast
            Broadcast.Channel<ChatRoomChannel>()
                .WithParameters(roomName)
                .Send(new PlayerJoinedMessage {
                    userName = userName
                });

            return subscription;
        }
        
        public void SendMessage(
            string roomName,
            string userName,
            string message
        )
        {
            Broadcast.Channel<ChatRoomChannel>()
                .WithParameters(roomName)
                .Send(new ChatMessage {
                    userName = userName,
                    message = message
                });
        }
    }
}