namespace Unisave.EmailAuthentication
{
    /// <summary>
    /// Possible results of player registration via email and password
    /// </summary>
    public enum EmailRegisterStatusCode
    {
        /// <summary>
        /// The player was registered
        /// </summary>
        Success = 0,
        
        /// <summary>
        /// The provided email address is not a valid email address
        /// </summary>
        InvalidEmail = 1,
        
        /// <summary>
        /// The provided password is too weak
        /// </summary>
        WeakPassword = 2,
        
        /// <summary>
        /// The provided email is already registered
        /// </summary>
        EmailTaken = 3,
        
        /// <summary>
        /// The player did not agree to the legal terms of the registration
        /// </summary>
        LegalConsentRequired = 4,
    }
}