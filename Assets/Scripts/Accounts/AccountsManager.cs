using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unisave;
using Unisave.Entities;
using Unisave.Facades;

public class AccountsManager : MonoBehaviour
{
    [SerializeField] private GameObject registerPanelGO;

    #region current player data
    private PlayerEntity player;

    #endregion

    public void LoadPlayer()
    {
        OnFacet<PlayerDataFacet>
            .Call<PlayerEntity>(nameof(PlayerDataFacet.GetPlayerEntity))
            .Then(LoadPlayerCallback)
            .Done();
    }

    private void LoadPlayerCallback(PlayerEntity player)
    {
        this.player = player;
        Debug.Log("Loaded player data: " + player.email);
    }

    public void AddExperiment()
    {

        player.Save();
    }

    public void AddProbe()
    {

        player.Save();
    }

    public void ChangeProbeExperiment()
    {

        player.Save();
    }

    public void ShowRegisterPanel()
    {
        registerPanelGO.SetActive(true);
    }
}