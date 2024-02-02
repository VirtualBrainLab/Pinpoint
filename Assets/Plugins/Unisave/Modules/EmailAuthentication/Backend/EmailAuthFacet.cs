using System;
using LightJson;
using Unisave.Arango;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.Utils;

namespace Unisave.EmailAuthentication
{
    public class EmailAuthFacet : Facet
    {
        private readonly EmailAuthBootstrapperBase bootstrapper;

        public EmailAuthFacet(EmailAuthBootstrapperBase bootstrapper)
        {
            this.bootstrapper = bootstrapper;
            
            bootstrapper.AssertDatabaseCollections();
        }

        /// <summary>
        /// Call this from your login form
        /// </summary>
        /// <param name="email">Player's email</param>
        /// <param name="password">Player's password</param>
        public EmailLoginResponse Login(string email, string password)
        {
            JsonObject player = FindPlayer(email);

            // player with this email does not exist, fail login
            if (player == null)
                return EmailLoginResponse.Failure;

            // player does exist, but lacks the password field,
            // fail the login and log warning
            if (!player.ContainsKey(bootstrapper.PasswordField))
            {
                Log.Warning(
                    "The player document is missing the password field, " +
                    "and so it cannot be logged in.",
                    new JsonObject {
                        ["playerId"] = player["_id"].AsString
                    }
                );
                return EmailLoginResponse.Failure;
            }

            // if the password does not match, fail login
            string passwordHash = player[bootstrapper.PasswordField].AsString;
            if (!Hash.Check(password, passwordHash))
                return EmailLoginResponse.Failure;

            // === login is successful ===
            
            // perform the login
            Auth.Login(player["_id"].AsString);

            // run the after-login hook
            bootstrapper.PlayerHasLoggedIn(player["_id"].AsString);

            // signal success to the client
            return new EmailLoginResponse {
                Success = true,
                PlayerId = player["_id"].AsString
            };
        }

        /// <summary>
        /// Call this from your registration form
        /// </summary>
        /// <param name="email">Player's email</param>
        /// <param name="password">Player's password</param>
        /// <param name="playerAcceptsLegalTerms">
        /// Whether or not the player accepted legal terms of the registration
        /// </param>
        public EmailRegisterResponse Register(
            string email,
            string password,
            bool playerAcceptsLegalTerms
        )
        {
            if (email == null)
                throw new ArgumentNullException(nameof(email));
        
            if (password == null)
                throw new ArgumentNullException(nameof(password));
 
            // check email format validity
            string normalizedEmail = bootstrapper.NormalizeEmail(email);
            if (!bootstrapper.IsEmailValid(normalizedEmail))
                return new EmailRegisterResponse {
                    StatusCode = EmailRegisterStatusCode.InvalidEmail
                };
        
            // check password strength
            if (!bootstrapper.IsPasswordStrong(password))
                return new EmailRegisterResponse {
                    StatusCode = EmailRegisterStatusCode.WeakPassword
                };
        
            // check if email still available
            if (FindPlayer(email) != null)
                return new EmailRegisterResponse {
                    StatusCode = EmailRegisterStatusCode.EmailTaken
                };
            
            // check legal consent
            if (!playerAcceptsLegalTerms)
                return new EmailRegisterResponse {
                    StatusCode = EmailRegisterStatusCode.LegalConsentRequired
                };
        
            // register the player
            string hashedPassword = Hash.Make(password);
            string playerId = bootstrapper.RegisterNewPlayer(
                normalizedEmail,
                hashedPassword
            );
            
            // make sure the email and password fields are set properly
            string playerKey = DocumentId.Parse(playerId).Key;
            try
            {
                DB.Query(@"
                    UPDATE @key WITH {
                        @emailField: @email,
                        @passwordField: @password
                    } IN @@collection
                ")
                    .Bind("@collection", bootstrapper.PlayersCollection)
                    .Bind("emailField", bootstrapper.EmailField)
                    .Bind("passwordField", bootstrapper.PasswordField)
                    .Bind("email", normalizedEmail)
                    .Bind("password", hashedPassword)
                    .Bind("key", playerKey)
                    .Run();
            }
            catch (ArangoException e) when (e.ErrorNumber == 1202)
            {
                // [document not found]
                string methodName = nameof(
                    EmailAuthBootstrapperBase.RegisterNewPlayer
                );
                throw new Exception(
                    $"{methodName} must create the player document " +
                    $"in the database."
                );
            }
            catch (ArangoException e) when (e.ErrorNumber == 1200)
            {
                // [write-write conflict]
                // do nothing, most likely everything is fine
            }

            // log in
            Auth.Login(playerId);
            
            bootstrapper.PlayerHasRegistered(playerId);
        
            return new EmailRegisterResponse {
                StatusCode = EmailRegisterStatusCode.Success,
                PlayerId = playerId
            };
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
        
        /// <summary>
        /// Finds a player by the given email address in the same way
        /// login method finds the player. Returns null if no player was found.
        /// </summary>
        private JsonObject FindPlayer(string email)
        {
            // First, find by normalized email address.
            JsonObject player = DB.Query(@"
                FOR player IN @@collection
                    FILTER player[@emailField] == @email
                    RETURN player
            ")
                .Bind("@collection", bootstrapper.PlayersCollection)
                .Bind("emailField", bootstrapper.EmailField)
                .Bind("email", bootstrapper.NormalizeEmail(email))
                .FirstAs<JsonObject>();

            // If we found one, we're done.
            if (player != null)
                return player;
        
            // If not, try finding without normalizing, since the player
            // may have been inserted by hand.
            return DB.Query(@"
                FOR player IN @@collection
                    FILTER player[@emailField] == @email
                    RETURN player
            ")
                .Bind("@collection", bootstrapper.PlayersCollection)
                .Bind("emailField", bootstrapper.EmailField)
                .Bind("email", email)
                .FirstAs<JsonObject>();
        }
    }
}