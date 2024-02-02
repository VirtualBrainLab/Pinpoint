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
    public (bool success, string token) Login(string email, string password)
    {
        var player = EmailAuthUtils.FindPlayer(email);

        if (player == null)
            return (false, null);

        if (!Hash.Check(password, player.password))
            return (false, null);

        Auth.Login(player);
        
        PlayerHasLoggedIn(player, true);
        
        return (true, player.token);
    }

    public bool LoginViaToken(string email, string token)
    {
        var player = EmailAuthUtils.FindPlayer(email);

        if (player == null)
            return false;

        if (!token.Equals(player.token) || DateTime.UtcNow > player.tokenExpiration)
            return false;

        Auth.Login(player);

        PlayerHasLoggedIn(player);

        return true;
    }

    /// <summary>
    /// Called after successful login
    /// </summary>
    /// <param name="player">The player that has logged in</param>
    private void PlayerHasLoggedIn(PlayerEntity player, bool updateToken = false)
    {
        player.lastLoginAt = DateTime.UtcNow;
        if (updateToken)
        {
            player.token = Guid.NewGuid().ToString();
            player.tokenExpiration = DateTime.UtcNow.AddDays(7);
        }
        player.Save();
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

