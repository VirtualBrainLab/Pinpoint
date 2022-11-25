using System;
using TMPro;
using Unisave.Facades;
using Unisave.Facets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
 * EmailAuthentication template - v0.9.1
 * -------------------------------------
 *
 * This script controls the login form and makes login requests.
 *
 * Reference required UI elements and specify what scene to load after login.
 */

public class EmailLoginForm : MonoBehaviour
{
    [SerializeField] private AccountsManager _accountsManager;

    public TMP_InputField EmailField;
    public TMP_InputField PasswordField;
    public Button LoginButton;
    public TMP_Text StatusText;

    private bool loggedIn;

    void Start()
    {
        if (EmailField == null)
            throw new ArgumentException(
                $"Link the '{nameof(EmailField)}' in the inspector."
            );
        
        if (PasswordField == null)
            throw new ArgumentException(
                $"Link the '{nameof(PasswordField)}' in the inspector."
            );
        
        if (LoginButton == null)
            throw new ArgumentException(
                $"Link the '{nameof(LoginButton)}' in the inspector."
            );
        
        if (StatusText == null)
            throw new ArgumentException(
                $"Link the '{nameof(StatusText)}' in the inspector."
            );
        
        LoginButton.onClick.AddListener(OnLoginClicked);

        StatusText.enabled = false;
    }

    public async void OnLoginClicked()
    {
        if (loggedIn)
        {
            StatusText.enabled = false;
            loggedIn = false;
            LoginButton.GetComponentInChildren<TMP_Text>().text = "Login";

            var logoutResponse = await OnFacet<EmailLoginFacet>.CallAsync<bool>(nameof(EmailLoginFacet.Logout));

            return;
        }


        StatusText.enabled = true;
        StatusText.text = "Logging in...";
        StatusText.color = Color.yellow;
        loggedIn = false;

        var loginResponse = await OnFacet<EmailLoginFacet>.CallAsync<bool>(
            nameof(EmailLoginFacet.Login),
            EmailField.text,
            PasswordField.text
        );

        if (loginResponse)
        {
            StatusText.text = string.Format("Logged into: {0}",EmailField.text);
            StatusText.color = Color.green;

            // setup logout logic
            loggedIn = true;
            LoginButton.GetComponentInChildren<TMP_Text>().text = "Logout";

            _accountsManager.LoadPlayer();
        }
        else
        {
            StatusText.text = "This account does not exist,\nor the password is not valid";
            StatusText.color = Color.red;
        }
    }
}

