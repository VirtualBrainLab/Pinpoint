using System;
using Unisave.Arango;
using Unisave.Bootstrapping;
using Unisave.Contracts;
using Unisave.Facades;

namespace Unisave.EmailAuthentication
{
    public abstract class EmailAuthBootstrapperBase : Bootstrapper
    {
        /// <summary>
        /// Name of the players database collection
        /// </summary>
        public abstract string PlayersCollection { get; }
        
        /// <summary>
        /// Name of the email document field
        /// </summary>
        public abstract string EmailField { get; }
        
        /// <summary>
        /// Name of the password document field
        /// </summary>
        public abstract string PasswordField { get; }
        
        public override void Main()
        {
            // make this bootstrapper instance be the one
            // used by the module backend code
            Services.RegisterInstance<EmailAuthBootstrapperBase>(this);
        }
        
        /// <summary>
        /// Creates a new player document for the given email and password.
        /// </summary>
        /// <param name="email">Normalized email address</param>
        /// <param name="password">Hashed password</param>
        /// <returns></returns>
        public abstract string RegisterNewPlayer(
            string email,
            string password
        );
        
        /// <summary>
        /// Called after successful registration
        /// </summary>
        /// <param name="documentId">The player ID that has been registered</param>
        public virtual void PlayerHasRegistered(string documentId)
        {
            // perform actions after registration, e.g. send validation email
        }
        
        /// <summary>
        /// Called after a successful login
        /// </summary>
        /// <param name="documentId">Document ID of the logged in player</param>
        public virtual void PlayerHasLoggedIn(string documentId)
        {
            // perform actions after login, e.g. update last login timestamp
        }
        
        /// <summary>
        /// Normalizes email address (trim + lowercase).
        /// Use this method when storing and finding email addresses
        /// in the database to make the process seems case-insensitive.
        /// </summary>
        public virtual string NormalizeEmail(string email)
        {
            return email?.Trim().ToLowerInvariant();
        }
        
        /// <summary>
        /// Checks that the given string is a valid email address.
        /// </summary>
        public virtual bool IsEmailValid(string email)
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
        
        /// <summary>
        /// Determines whether the given password is strong enough.
        /// </summary>
        public virtual bool IsPasswordStrong(string password)
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
        /// Makes sure database collection assertions are run only once
        /// </summary>
        protected bool collectionsAlreadyAsserted = false;
        
        /// <summary>
        /// Makes sure that necessary database collections are present.
        /// </summary>
        public virtual void AssertDatabaseCollections()
        {
            // run only once
            if (collectionsAlreadyAsserted) return;
            collectionsAlreadyAsserted = true;
            
            try
            {
                DB.Query(@"
                    FOR player IN @@collection
                        LIMIT 1
                        RETURN player
                ")
                    .Bind("@collection", PlayersCollection)
                    .Run();
            }
            catch (ArangoException e) when (e.ErrorNumber == 1203)
            {
                // [collection or view not found]
                CreatePlayersCollection();
            }
        }

        /// <summary>
        /// Creates the collection for players
        /// </summary>
        private void CreatePlayersCollection()
        {
            var arango = Services.Resolve<IArango>();
            
            arango.CreateCollection(PlayersCollection, CollectionType.Document);
            
            arango.CreateIndex(
                PlayersCollection,
                IndexType.Persistent,
                new string[] { EmailField }
            );
        }
    }
}