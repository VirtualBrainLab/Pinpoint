using Unisave.Facades;
using Unisave.Facets;

public class PlayerDataFacet : Facet
{
    /// <summary>
    /// Returns information about the logged-in player
    /// </summary>
    public PlayerEntity LoadPlayerEntity()
    {
        // obtain authenticated player ID from the session
        // and load player data from the database
        return Auth.GetPlayer<PlayerEntity>();
    }

    public void SavePlayerEntity(PlayerEntity givenPlayer)
    {
        var player = Auth.GetPlayer<PlayerEntity>();

        player.FillWith(givenPlayer);

        player.Save();
    }
}