using System;
using Unisave.Examples.PlayerAuthentication.Backend.EmailAuthentication;
using Unisave.Facades;
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

namespace Unisave.Examples.PlayerAuthentication
{
    public class EmailRegisterForm : MonoBehaviour
    {
        public InputField emailField;
        public InputField passwordField;
        public InputField confirmPasswordField;
        public Button registerButton;
        public Text statusText;
    
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
        }

        public async void OnRegisterClicked()
        {
            statusText.enabled = true;
            statusText.text = "Registering...";

            if (passwordField.text != confirmPasswordField.text)
            {
                statusText.text = "Password confirmation does not match";
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
                    statusText.text = "Registration succeeded";
                    break;
            
                case EmailRegisterResponse.EmailTaken:
                    statusText.text = "This email has already been registered";
                    break;
            
                case EmailRegisterResponse.InvalidEmail:
                    statusText.text = "This is not a valid email address";
                    break;
            
                case EmailRegisterResponse.WeakPassword:
                    statusText.text = "Password needs to be at least 8 " +
                                      "characters long";
                    break;
            
                default:
                    statusText.text = "Unknown response: " + response;
                    break;
            }
        }
    }
}

