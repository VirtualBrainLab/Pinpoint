using System;
using Unisave.Entities;

namespace Unisave.Examples.PlayerAuthentication.Backend
{
    public class PlayerEntity : Entity
    {
        /// <summary>
        /// Email of the player
        /// </summary>
        public string email;
        
        /// <summary>
        /// Hashed password of the player
        /// </summary>
        public string password;
        
        /// <summary>
        /// When was the last time the player has logged in
        /// </summary>
        public DateTime lastLoginAt = DateTime.UtcNow;
    }
}
