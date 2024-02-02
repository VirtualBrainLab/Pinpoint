/*
 * EmailAuthentication template - v0.9.1
 * -------------------------------------
 */

/// <summary>
/// Possible results of player registration via email and password
/// </summary>
public enum EmailRegisterResponse
{
    Ok = 0,
    InvalidEmail = 1,
    WeakPassword = 2,
    EmailTaken = 3
}

