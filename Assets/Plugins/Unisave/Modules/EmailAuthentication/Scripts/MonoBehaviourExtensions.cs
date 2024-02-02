using Unisave.Facets;
using UnityEngine;

namespace Unisave.EmailAuthentication
{
    /// <summary>
    /// Allows you to interact with the email auth system from mono behaviours
    /// </summary>
    public static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Logs the player in based on the given email address and password.
        /// </summary>
        /// <param name="caller">The MonoBehaviour triggering this Unisave operation</param>
        /// <param name="email">Player's email address</param>
        /// <param name="password">Player-entered password</param>
        /// <returns>Result of the login attempt - success or failure</returns>
        public static UnisaveOperation<EmailLoginResponse> LoginViaEmail(
            this MonoBehaviour caller,
            string email,
            string password
        )
        {
            return caller.CallFacet(
                (EmailAuthFacet f) => f.Login(email, password)
            );
        }

        /// <summary>
        /// Registers a new player account for the given email address
        /// with the provided password.
        /// </summary>
        /// <param name="caller">The MonoBehaviour triggering this Unisave operation</param>
        /// <param name="email">Email address to register the account for</param>
        /// <param name="password">Password to set for the account</param>
        /// <param name="playerAcceptsLegalTerms">
        /// Whether or not the player accepted legal terms of the account registration
        /// </param>
        /// <returns></returns>
        public static UnisaveOperation<EmailRegisterResponse> RegisterViaEmail(
            this MonoBehaviour caller,
            string email,
            string password,
            bool playerAcceptsLegalTerms
        )
        {
            return caller.CallFacet(
                (EmailAuthFacet f) => f.Register(
                    email, password, playerAcceptsLegalTerms
                )
            );
        }
    }
}