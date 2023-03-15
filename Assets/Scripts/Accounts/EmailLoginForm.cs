using System;
using TMPro;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.Utils;
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
    public const string UNISAVE_EMAIL_STR = "unisave-email";
    public const string UNISAVE_TOKEN_STR = "unisave-token";

    [SerializeField] private UnisaveAccountsManager _accountsManager;
    [SerializeField] private Toggle _stayLoggedInToggle;

    public TMP_InputField _emailField;
    public TMP_InputField _passwordField;
    public Button _loginButton;
    public TMP_Text _statusText;

    private bool loggedIn;

    private void Awake()
    {
        if (_emailField == null)
            throw new ArgumentException(
                $"Link the '{nameof(_emailField)}' in the inspector."
            );

        if (_passwordField == null)
            throw new ArgumentException(
                $"Link the '{nameof(_passwordField)}' in the inspector."
            );

        if (_loginButton == null)
            throw new ArgumentException(
                $"Link the '{nameof(_loginButton)}' in the inspector."
            );

        if (_statusText == null)
            throw new ArgumentException(
                $"Link the '{nameof(_statusText)}' in the inspector."
            );

        _loginButton.onClick.AddListener(OnLoginClicked);

        //_statusText.enabled = false;
    }

    public async void OnLoginClicked()
    {

        // Save the email
        PlayerPrefs.SetString(UNISAVE_EMAIL_STR, _emailField.text);

        if (loggedIn)
        {
            _statusText.enabled = false;
            loggedIn = false;
            _loginButton.GetComponentInChildren<TMP_Text>().text = "Login";

            _accountsManager.SavePlayer();

            var logoutResponse = await OnFacet<EmailLoginFacet>.CallAsync<bool>(nameof(EmailLoginFacet.Logout));

            _accountsManager.LogoutCleanup();
        }
        else
        {
            if (_emailField.text.Length == 0 || _passwordField.text.Length == 0)
            {
                _statusText.enabled = true;
                _statusText.text = "Please enter a username and password";
                _statusText.color = Color.red;
                return;
            }

            _statusText.enabled = true;
            _statusText.text = "Logging in...";
            _statusText.color = Color.yellow;
            loggedIn = false;

            // Note that we send a hashed password to avoid ever sending a plain text password
            var loginResponse = await OnFacet<EmailLoginFacet>.CallAsync<(bool success, string token)>(
                nameof(EmailLoginFacet.Login),
                _emailField.text,
                _passwordField.text
            );

            if (loginResponse.success)
            {
                if (_stayLoggedInToggle)
                    PlayerPrefs.SetString(UNISAVE_TOKEN_STR, loginResponse.token);
                Debug.Log($"Received token {loginResponse.token}");

                _statusText.text = string.Format("Logged into: {0}", _emailField.text);
                _statusText.color = Color.green;

                // setup logout logic
                loggedIn = true;
                _loginButton.GetComponentInChildren<TMP_Text>().text = "Logout";

                _accountsManager.Login();
            }
            else
            {
                _statusText.text = "This account does not exist,\nor the password is not valid";
                _statusText.color = Color.red;
            }
        }

    }

    public async void AttemptLoginViaTokenAsync()
    {
        if (PlayerPrefs.HasKey(UNISAVE_TOKEN_STR) && PlayerPrefs.HasKey(UNISAVE_EMAIL_STR))
        {
            string email = PlayerPrefs.GetString(UNISAVE_EMAIL_STR);
            string token = PlayerPrefs.GetString(UNISAVE_TOKEN_STR);

            Debug.Log($"Attempting login using {email} and {token}");

            var loginResponse = await OnFacet<EmailLoginFacet>.CallAsync<bool>(
                nameof(EmailLoginFacet.LoginViaToken),
                email,
                token
            );

            _statusText.enabled = true;
            _statusText.text = "Attempting login via token...";
            _statusText.color = Color.yellow;
            loggedIn = false;

            Debug.Log("Auto-login response received");
            if (loginResponse)
            {
                _statusText.enabled = true;
                _statusText.text = string.Format("Logged into: {0}", email);
                _statusText.color = Color.green;

                // setup logout logic
                loggedIn = true;
                _loginButton.GetComponentInChildren<TMP_Text>().text = "Logout";

                _accountsManager.Login();
            }
            else
            {
                _statusText.text = "Autologin failed\nThis account does not exist,\nor the token was invalid";
                _statusText.color = Color.red;

                // Also clear the token
                ClearToken();
            }
        }
    }

    public void DisableLoginToken(bool state)
    {
        Settings.StayLoggedIn = state;
        if (!state)
        {
            ClearToken();
        }
    }

    public void ClearToken()
    {
        PlayerPrefs.DeleteKey(UNISAVE_EMAIL_STR);
        PlayerPrefs.DeleteKey(UNISAVE_TOKEN_STR);
        PlayerPrefs.Save();
    }

}

