namespace Unisave.EmailAuthentication
{
    /// <summary>
    /// Outcome of an email login attempt
    /// </summary>
    public class EmailLoginResponse
    {
        /// <summary>
        /// True if the login was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// ID of the logged-in unisave player
        /// (null if login fails)
        /// </summary>
        public string PlayerId { get; set; }
        
        /// <summary>
        /// Returned by the backend on login failure
        /// </summary>
        public static EmailLoginResponse Failure => new EmailLoginResponse {
            Success = false,
            PlayerId = null
        };
    }
}