using System;
using Unisave;
using Unisave.Facades;
using Unisave.Facets;

public class PlayerDataFacet : Facet
{
    /// <summary>
    /// Returns information about the logged-in player
    /// </summary>
    public PlayerEntity GetPlayerEntity()
    {
        // obtain authenticated player ID from the session
        // and load player data from the database
        PlayerEntity player = Auth.GetPlayer<PlayerEntity>();

        // send the data back to the game client
        return player;
    }

    public void SavePlayerEntity()
    {
        
    }

    public string Greeting()
    {
        return "Hello world";
    }
}