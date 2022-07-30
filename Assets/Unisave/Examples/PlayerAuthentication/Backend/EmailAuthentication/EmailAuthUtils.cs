using System;
using Unisave.Facades;

/*
 * EmailAuthentication template - v0.9.1
 * -------------------------------------
 *
 * Set of utility functions used during player authentication via email
 * and password. You don't need to modify this code.
 */

namespace Unisave.Examples.PlayerAuthentication.Backend.EmailAuthentication
{
    public static class EmailAuthUtils
    {
        /// <summary>
        /// Finds a player by the email address in the same way
        /// login method finds the player. Returns null if no player was found.
        /// </summary>
        public static PlayerEntity FindPlayer(string email)
        {
            // find by email as-is
            // (allows already registered players with non-normalized email
            // addresses to login)
            var player = DB.TakeAll<PlayerEntity>()
                .Filter(entity => entity.email == email)
                .First();

            if (player != null)
                return player;
    
            // find with normalized email address
            // (the default login method)
            player = DB.TakeAll<PlayerEntity>()
                .Filter(entity => entity.email == NormalizeEmail(email))
                .First();

            return player;
        }
    
        /// <summary>
        /// Normalizes email address (trim + lowercase).
        /// Use this method when storing and finding email addresses
        /// in the database to make the process seem case-insensitive.
        /// </summary>
        public static string NormalizeEmail(string email)
        {
            return email?.Trim().ToLowerInvariant();
        }
    
        /// <summary>
        /// Checks that the given string is a valid email address.
        /// </summary>
        public static bool IsEmailValid(string email)
        {
            try
            {
                var parsed = new System.Net.Mail.MailAddress(email);
                return parsed.Address == email;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}

