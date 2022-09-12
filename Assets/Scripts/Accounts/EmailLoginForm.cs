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
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public Button loginButton;
    public TMP_Text statusText;

    private bool loggedIn;

    void Start()
    {
        if (emailField == null)
            throw new ArgumentException(
                $"Link the '{nameof(emailField)}' in the inspector."
            );
        
        if (passwordField == null)
            throw new ArgumentException(
                $"Link the '{nameof(passwordField)}' in the inspector."
            );
        
        if (loginButton == null)
            throw new ArgumentException(
                $"Link the '{nameof(loginButton)}' in the inspector."
            );
        
        if (statusText == null)
            throw new ArgumentException(
                $"Link the '{nameof(statusText)}' in the inspector."
            );
        
        loginButton.onClick.AddListener(OnLoginClicked);

        statusText.enabled = false;
    }

    public async void OnLoginClicked()
    {
        if (loggedIn)
        {
            statusText.enabled = false;
            loggedIn = false;
            loginButton.GetComponentInChildren<TMP_Text>().text = "Login";

            var logoutResponse = await OnFacet<EmailLoginFacet>.CallAsync<bool>(nameof(EmailLoginFacet.Logout));

            return;
        }


        statusText.enabled = true;
        statusText.text = "Logging in...";
        statusText.color = Color.yellow;
        loggedIn = false;

        var loginResponse = await OnFacet<EmailLoginFacet>.CallAsync<bool>(
            nameof(EmailLoginFacet.Login),
            emailField.text,
            passwordField.text
        );

        if (loginResponse)
        {
            statusText.text = string.Format("Logged into: {0}",emailField.text);
            statusText.color = Color.green;

            // setup logout logic
            loggedIn = true;
            loginButton.GetComponentInChildren<TMP_Text>().text = "Logout";
        }
        else
        {
            statusText.text = "This account does not exist,\nor the password is not valid";
            statusText.color = Color.red;
        }
    }
}

