using System;
using Unisave.EmailAuthentication;
using Unisave.Facades;

namespace Unisave.Examples.EmailAuthentication.Backend
{
    public class EmailAuthBootstrapper : EmailAuthBootstrapperBase
    {
        // REMOVE THIS LINE IF YOU WANT TO USE THIS CODE IN YOUR OWN BACKEND
        // (this line makes sure the example scene overrides your backend)
        public override int StageNumber => base.StageNumber + 1;

        public override string PlayersCollection => "players";
        public override string EmailField => "email";
        public override string PasswordField => "password";
        
        public override string RegisterNewPlayer(
            string email,
            string password
        )
        {
            // create the player entity
            var player = new PlayerEntity {
                email = email,
                password = password
                
                // ... you can do your own initialization here ...
            };
            
            // insert it to the database to obtain the document ID
            player.Save();

            // return the document ID
            return player.EntityId;
        }
        
        public override void PlayerHasLoggedIn(string documentId)
        {
            var player = DB.Find<PlayerEntity>(documentId);
            
            // update login timestamp
            player.lastLoginAt = DateTime.UtcNow;
            
            player.Save();
        }
        
        public override bool IsPasswordStrong(string password)
        {
            // apply base constraints (non-empty, non-null)
            if (!base.IsPasswordStrong(password))
                return false;
        
            // an example additional constraint
            if (password.Length < 8)
                return false;
        
            return true;
        }
    }
}