using System;
using Unisave.Examples.PlayerAuthentication.Backend;
using Unisave.Facades;
using UnityEngine;
using UnityEngine.UI;

namespace Unisave.Examples.PlayerAuthentication
{
    public class WhoIsController : MonoBehaviour
    {
        public Button whoIsButton;
        public Button callGuardedButton;
        
        private void Start()
        {
            if (whoIsButton == null)
                throw new ArgumentNullException(
                    nameof(whoIsButton),
                    nameof(whoIsButton) + " field has not been linked."
                );
            
            if (callGuardedButton == null)
                throw new ArgumentNullException(
                    nameof(callGuardedButton),
                    nameof(callGuardedButton) + " field has not been linked."
                );
            
            whoIsButton.onClick.AddListener(WhoIsButtonClicked);
            callGuardedButton.onClick.AddListener(CallGuardedButtonClicked);
        }

        private async void WhoIsButtonClicked()
        {
            var player = await OnFacet<WhoIsFacet>.CallAsync<PlayerEntity>(
                nameof(WhoIsFacet.WhoIsLoggedIn)
            );

            if (player == null)
                Debug.Log("There is no logged in player.");
            else
                Debug.Log("The logged in player is: " + player.email);
        }
        
        private async void CallGuardedButtonClicked()
        {
            Debug.Log(
                "Calling the guarded method...\n" +
                "This will fail if no player is authenticated."
            );
            
            await OnFacet<WhoIsFacet>.CallAsync(
                nameof(WhoIsFacet.GuardedMethod)
            );
        }
    }
}