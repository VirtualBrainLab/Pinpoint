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
    [SerializeField] private UnisaveAccountsManager _accountsManager;

    public TMP_InputField _emailField;
    public TMP_InputField _passwordField;
    public Button _loginButton;
    public TMP_Text _statusText;

    private bool loggedIn;

    void Start()
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

        _statusText.enabled = false;
    }

    public async void OnLoginClicked()
    {
        if (loggedIn)
        {
            _statusText.enabled = false;
            loggedIn = false;
            _loginButton.GetComponentInChildren<TMP_Text>().text = "Login";

            var logoutResponse = await OnFacet<EmailLoginFacet>.CallAsync<bool>(nameof(EmailLoginFacet.Logout));

            return;
        }


        _statusText.enabled = true;
        _statusText.text = "Logging in...";
        _statusText.color = Color.yellow;
        loggedIn = false;

        var loginResponse = await OnFacet<EmailLoginFacet>.CallAsync<bool>(
            nameof(EmailLoginFacet.Login),
            _emailField.text,
            _passwordField.text
        );

        if (loginResponse)
        {
            _statusText.text = string.Format("Logged into: {0}",_emailField.text);
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

