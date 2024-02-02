using Unisave.Authentication.Middleware;
using Unisave.Facades;
using Unisave.Facets;

namespace Unisave.Examples.EmailAuthentication.Backend
{
    /// <summary>
    /// An example facet for handling player data
    /// </summary>
    public class PlayerDataFacet : Facet
    {
        /// <summary>
        /// Returns the logged-in player's entity
        /// </summary>
        public PlayerEntity DownloadLoggedInPlayer()
        {
            return Auth.GetPlayer<PlayerEntity>();
        }

        /// <summary>
        /// Increments the star count for the player
        /// and returns the updated player entity
        /// </summary>
        public PlayerEntity CollectStar()
        {
            var player = Auth.GetPlayer<PlayerEntity>();

            player.collectedStars += 1;
            player.Save();

            return player;
        }
    }
}
