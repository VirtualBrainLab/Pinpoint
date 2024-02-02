namespace Unisave.EmailAuthentication
{
    /// <summary>
    /// Outcome of an email registration attempt
    /// </summary>
    public class EmailRegisterResponse
    {
        /// <summary>
        /// Specific status code for the result of the registration
        /// </summary>
        public EmailRegisterStatusCode StatusCode { get; set; }

        /// <summary>
        /// True if the registration was successful
        /// </summary>
        public bool Success => StatusCode == EmailRegisterStatusCode.Success;
        
        /// <summary>
        /// ID of the registered (and now logged-in) unisave player
        /// (null if registration fails)
        /// </summary>
        public string PlayerId { get; set; }
    }
}