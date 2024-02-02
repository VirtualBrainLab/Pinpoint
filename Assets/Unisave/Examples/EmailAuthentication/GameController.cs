using System;
using TMPro;
using Unisave.EmailAuthentication;
using Unisave.Examples.EmailAuthentication.Backend;
using Unisave.Facets;
using Unisave.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Unisave.Examples.EmailAuthentication
{
    public class GameController : MonoBehaviour
    {
        // references to UI components
        public EmailAuthPanel authPanel;
        public GameObject gameUI;
        public TMP_Text starsText;
        public TMP_Text playerEntityText;
        public Button logoutButton;
        public Button starButton;

        // state
        public int collectedStars = 0;

        private void Start()
        {
            CheckRequiredDependencies();
            
            // start the game when a user logs in or registers
            authPanel.onLoginSuccess.AddListener(
                loginResponse => ShowGame()
            );
            authPanel.onRegistrationSuccess.AddListener(
                registerResponse => ShowGame()
            );
            
            // handle logout
            logoutButton.onClick.AddListener(OnLogoutClicked);
            
            // handle star click
            starButton.onClick.AddListener(OnStarClicked);
        }

        private void CheckRequiredDependencies()
        {
            if (authPanel == null)
                throw new ArgumentException(
                    $"Link the '{nameof(authPanel)}' in the inspector."
                );
            
            if (gameUI == null)
                throw new ArgumentException(
                    $"Link the '{nameof(gameUI)}' in the inspector."
                );
            
            if (starsText == null)
                throw new ArgumentException(
                    $"Link the '{nameof(starsText)}' in the inspector."
                );
            
            if (playerEntityText == null)
                throw new ArgumentException(
                    $"Link the '{nameof(playerEntityText)}' in the inspector."
                );
            
            if (logoutButton == null)
                throw new ArgumentException(
                    $"Link the '{nameof(logoutButton)}' in the inspector."
                );
            
            if (starButton == null)
                throw new ArgumentException(
                    $"Link the '{nameof(starButton)}' in the inspector."
                );
        }

        public async void ShowGame()
        {
            // prepare the game
            gameUI.SetActive(true);
            authPanel.gameObject.SetActive(false);
            RandomizeStarPosition();
            
            // download player entity
            PlayerEntity entity = await this.CallFacet(
                (PlayerDataFacet f) => f.DownloadLoggedInPlayer()
            );
            collectedStars = entity.collectedStars;
            DisplayPlayerEntity(entity);
            RefreshStarCount();
        }

        public void ShowAuthPanel()
        {
            gameUI.SetActive(false);
            authPanel.gameObject.SetActive(true);
            authPanel.ShowLoginForm();
        }

        private void RandomizeStarPosition()
        {
            RectTransform t = starButton.GetComponent<RectTransform>();
            
            t.anchoredPosition = new Vector2(
                Random.Range(-100f, 100f),
                Random.Range(-100f, 100f)
            );
        }

        public void OnLogoutClicked()
        {
            // logout in unisave
            this.CallFacet((EmailAuthFacet f) => f.Logout());
            
            ShowAuthPanel();
        }

        public async void OnStarClicked()
        {
            // update UI immediately
            collectedStars += 1;
            RefreshStarCount();
            
            // change star position
            RandomizeStarPosition();
            
            // call unisave to update the player entity
            PlayerEntity entity = await this.CallFacet(
                (PlayerDataFacet f) => f.CollectStar()
            );
            DisplayPlayerEntity(entity);
        }

        private void DisplayPlayerEntity(PlayerEntity entity)
        {
            playerEntityText.text = Serializer
                .ToJson(entity)
                .ToString(pretty: true);
        }

        private void RefreshStarCount()
        {
            starsText.text = $"Stars: {collectedStars}";
        }
    }
}