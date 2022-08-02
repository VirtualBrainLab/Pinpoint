using Unisave.Authentication.Middleware;
using Unisave.Facades;
using Unisave.Facets;

namespace Unisave.Examples.PlayerAuthentication.Backend
{
    public class WhoIsFacet : Facet
    {
        /// <summary>
        /// Returns the logged-in player
        /// </summary>
        public PlayerEntity WhoIsLoggedIn()
        {
            // WARNING: Be extremely careful about returning player entities
            // to clients in your game. You might accidentally leak someone
            // else's email address or password hash!
            
            return Auth.GetPlayer<PlayerEntity>();
        }

        [Middleware(typeof(Authenticate))]
        public void GuardedMethod()
        {
            // This method cannot be called unless a player is authenticated.
            // (thanks to the authentication middleware)
            // see https://unisave.cloud/docs/authentication
            
            Log.Info(
                "The guarded method has been called! The player is: "
                + Auth.GetPlayer<PlayerEntity>().email
            );
        }
    }
}
