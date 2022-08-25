using System;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.Utils;

/*
 * EmailAuthentication template - v0.9.1
 * -------------------------------------
 *
 * This facet handles player login via email and password and player logout.
 *
 * You can extend the PlayerHasLoggedIn(...) method to perform logic
 * after a successful login attempt.
 */

public class EmailLoginFacet : Facet
{
    /// <summary>
    /// Call this from your login form
    /// </summary>
    /// <param name="email">Player's email</param>
    /// <param name="password">Player's password</param>
    /// <returns>True when the login succeeds</returns>
    public bool Login(string email, string password)
    {
        var player = EmailAuthUtils.FindPlayer(email);

        if (player == null)
            return false;

        if (!Hash.Check(password, player.password))
            return false;

        Auth.Login(player);
        
        PlayerHasLoggedIn(player);
        
        return true;
    }

    /// <summary>
    /// Called after successful login
    /// </summary>
    /// <param name="player">The player that has logged in</param>
    private void PlayerHasLoggedIn(PlayerEntity player)
    {
        player.lastLoginAt = DateTime.UtcNow;
        player.Save();
        
        // You can perform any additional actions here
    }

    /// <summary>
    /// Call this from your "logout" button
    /// </summary>
    /// <returns>
    /// False if the player wasn't logged in to begin with.
    /// </returns>
    public bool Logout()
    {
        bool wasLoggedIn = Auth.Check();
        
        Auth.Logout();
        
        return wasLoggedIn;
    }
}

