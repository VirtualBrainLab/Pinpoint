using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unisave;
using Unisave.Entities;
using Unisave.Facades;

public class AccountsManager : MonoBehaviour
{
    [SerializeField] private GameObject registerPanelGO;

    public void LoadPlayer()
    {
        OnFacet<PlayerDataFacet>
            .Call<PlayerEntity>(nameof(PlayerDataFacet.GetPlayerEntity))
            .Then(LoadPlayerCallback)
            .Done();
    }

    private void LoadPlayerCallback(PlayerEntity player)
    {

        Debug.Log("Player: " + player.email);
    }

    public void ShowRegisterPanel()
    {
        registerPanelGO.SetActive(true);
    }
}