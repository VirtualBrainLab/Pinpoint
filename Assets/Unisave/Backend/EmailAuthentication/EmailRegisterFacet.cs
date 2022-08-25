using System;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.Utils;

/*
 * EmailAuthentication template - v0.9.1
 * -------------------------------------
 *
 * This facet handles player registration via email and password.
 *
 * Modify CreateNewPlayer(...) and IsPasswordStrong(...) methods
 * to suit your needs.
 *
 * Modify PlayerHasRegistered(...) to perform actions after successful
 * registration (e.g. send validation email)
 */

public class EmailRegisterFacet : Facet
{
    /// <summary>
    /// Call this from your registration form
    /// </summary>
    /// <param name="email">Player's email</param>
    /// <param name="password">Player's password</param>
    public EmailRegisterResponse Register(string email, string password)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email));
    
        if (password == null)
            throw new ArgumentNullException(nameof(password));
    
        string normalizedEmail = EmailAuthUtils.NormalizeEmail(email);

        if (!EmailAuthUtils.IsEmailValid(normalizedEmail))
            return EmailRegisterResponse.InvalidEmail;
    
        if (!IsPasswordStrong(password))
            return EmailRegisterResponse.WeakPassword;
    
        if (EmailAuthUtils.FindPlayer(email) != null)
            return EmailRegisterResponse.EmailTaken;
    
        var player = CreateNewPlayer(normalizedEmail, password);
        player.Save();
    
        Auth.Login(player);
        
        PlayerHasRegistered(player);
    
        return EmailRegisterResponse.Ok;
    }

    /// <summary>
    /// Creates the new player during successful registration
    /// </summary>
    /// <param name="email">Player's email</param>
    /// <param name="password">Player's password (not hashed yet)</param>
    /// <returns>PlayerEntity representing the new player</returns>
    public static PlayerEntity CreateNewPlayer(string email, string password)
    {
        var player = new PlayerEntity {
            email = email,
            password = Hash.Make(password),
        };
        
        // Add your own logic here,
        // e.g. give gold to players on testing server:
        //
        //    if (Env.GetString("ENV_TYPE") == "testing")
        //        gold += 1_000_000;
        //

        return player;
    }
    
    /// <summary>
    /// Determines whether the given password is strong enough.
    /// </summary>
    public static bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        // You can add additional constraints, like:
        //
        //    if (password.Length < 8)
        //        return false;
        //
    
        return true;
    }

    /// <summary>
    /// Called after successful registration
    /// </summary>
    /// <param name="player">The player that has been registered</param>
    private void PlayerHasRegistered(PlayerEntity player)
    {
        // perform actions after registration, e.g. send validation email
    }
}

