using System;
using System.Threading.Tasks;
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
 * This script controls the register form and makes registration requests.
 *
 * Reference required UI elements and specify what scene to load
 * after registration.
 */

public class EmailRegisterForm : MonoBehaviour
{
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TMP_InputField confirmPasswordField;
    public Button registerButton;
    public TMP_Text statusText;
    
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
        
        if (confirmPasswordField == null)
            throw new ArgumentException(
                $"Link the '{nameof(confirmPasswordField)}' in the inspector."
            );
        
        if (registerButton == null)
            throw new ArgumentException(
                $"Link the '{nameof(registerButton)}' in the inspector."
            );
        
        if (statusText == null)
            throw new ArgumentException(
                $"Link the '{nameof(statusText)}' in the inspector."
            );
        
        registerButton.onClick.AddListener(OnRegisterClicked);

        statusText.enabled = false;
        registerButton.enabled = true;
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public async void OnRegisterClicked()
    {
        statusText.enabled = true;
        statusText.text = "Registering...";
        registerButton.enabled = false;
        statusText.color = Color.yellow;

        if (passwordField.text != confirmPasswordField.text)
        {
            statusText.text = "Password confirmation does not match";
            statusText.color = Color.red;
            return;
        }
        
        var response = await OnFacet<EmailRegisterFacet>
            .CallAsync<EmailRegisterResponse>(
                nameof(EmailRegisterFacet.Register),
                emailField.text,
                passwordField.text
            );

        switch (response)
        {
            case EmailRegisterResponse.Ok:
                statusText.text = "Registration success";
                statusText.color = Color.green;

                await Task.Delay(1000);

                gameObject.SetActive(false);

                break;
            
            case EmailRegisterResponse.EmailTaken:
                statusText.text = "This email has already been registered";
                statusText.color = Color.red;
                registerButton.enabled = true;
                break;
            
            case EmailRegisterResponse.InvalidEmail:
                statusText.text = "This is not a valid email address";
                statusText.color = Color.red;
                registerButton.enabled = true;
                break;
            
            case EmailRegisterResponse.WeakPassword:
                statusText.text = "Password needs to be at least 8 " +
                                  "characters long";
                statusText.color = Color.red;
                registerButton.enabled = true;
                break;
            
            default:
                statusText.text = "Unknown response: " + response;
                statusText.color = Color.yellow;
                registerButton.enabled = true;
                break;
        }
    }
}

