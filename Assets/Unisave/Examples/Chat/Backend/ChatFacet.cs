using Unisave.Authentication;
using Unisave.Broadcasting;
using Unisave.Facades;
using Unisave.Facets;

namespace Unisave.Examples.Chat.Backend
{
    /// <summary>
    /// Facet is a class whose public methods can be called
    /// over the internet from game clients. The code here
    /// runs on the server (in the cloud).
    /// </summary>
    public class ChatFacet : Facet
    {
        /// <summary>
        /// Called by the client when it wants to join a chat room
        /// </summary>
        /// <param name="roomId">ID of the chat room</param>
        /// <param name="playerName">Name of the player</param>
        /// <returns>New subscription to the broadcasting channel
        /// that represents the chat room</returns>
        public ChannelSubscription JoinRoom(string roomId, string playerName)
        {
            // We can deny the subscription if we want,
            // simply don't create and don't return the subscription
            if (playerName.ToLowerInvariant() == "banned")
                throw new AuthException("You are banned!");
            
            // Get or create the broadcasting channel for the chat room.
            SpecificChannel channel = Broadcast.Channel<ChatRoomChannel>()
                .WithParameters(roomId);

            // Subscribe the client to the channel.
            ChannelSubscription subscription = channel.CreateSubscription();
            
            // Send a message to everyone listening to the channel
            // (including the newly joined player) that notifies
            // everyone that a new player has joined.
            channel.Send(new PlayerJoinedMessage {
                playerName = playerName
            });

            // Send the new subscription back to the client so that
            // it can start consuming the messages.
            return subscription;
        }
        
        /// <summary>
        /// Called by the client when it wants to send a message into the room
        /// </summary>
        /// <param name="roomId">ID of the chat room to send the message to</param>
        /// <param name="playerName">Name of the sender</param>
        /// <param name="message">Body of the sent message</param>
        public void SendMessage(
            string roomId,
            string playerName,
            string message
        )
        {
            // Get or create the broadcasting channel for the chat room.
            SpecificChannel channel = Broadcast.Channel<ChatRoomChannel>()
                .WithParameters(roomId);
            
            // Send a message into the channel.
            channel.Send(new ChatMessage {
                playerName = playerName,
                message = message
            });
        }
    }
}