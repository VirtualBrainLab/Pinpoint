using System;
using System.Threading.Tasks;
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
 * This script controls the register form and makes registration requests.
 *
 * Reference required UI elements and specify what scene to load
 * after registration.
 */

public class EmailRegisterForm : MonoBehaviour
{
    public TMP_InputField EmailField;
    public TMP_InputField PasswordField;
    public TMP_InputField ConfirmPasswordField;
    public Button RegisterButton;
    public TMP_Text StatusText;
    
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
        
        if (ConfirmPasswordField == null)
            throw new ArgumentException(
                $"Link the '{nameof(ConfirmPasswordField)}' in the inspector."
            );
        
        if (RegisterButton == null)
            throw new ArgumentException(
                $"Link the '{nameof(RegisterButton)}' in the inspector."
            );
        
        if (StatusText == null)
            throw new ArgumentException(
                $"Link the '{nameof(StatusText)}' in the inspector."
            );
        
        RegisterButton.onClick.AddListener(OnRegisterClicked);

        StatusText.enabled = false;
        RegisterButton.enabled = true;
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
        StatusText.enabled = true;
        StatusText.text = "Registering...";
        RegisterButton.enabled = false;
        StatusText.color = Color.yellow;

        if (PasswordField.text != ConfirmPasswordField.text)
        {
            StatusText.text = "Password confirmation does not match";
            StatusText.color = Color.red;
            return;
        }
        
        var response = await OnFacet<EmailRegisterFacet>
            .CallAsync<EmailRegisterResponse>(
                nameof(EmailRegisterFacet.Register),
                EmailField.text,
                PasswordField.text
            );

        switch (response)
        {
            case EmailRegisterResponse.Ok:
                StatusText.text = "Registration success";
                StatusText.color = Color.green;

                await Task.Delay(1000);

                gameObject.SetActive(false);

                break;
            
            case EmailRegisterResponse.EmailTaken:
                StatusText.text = "This email has already been registered";
                StatusText.color = Color.red;
                RegisterButton.enabled = true;
                break;
            
            case EmailRegisterResponse.InvalidEmail:
                StatusText.text = "This is not a valid email address";
                StatusText.color = Color.red;
                RegisterButton.enabled = true;
                break;
            
            case EmailRegisterResponse.WeakPassword:
                StatusText.text = "Password needs to be at least 8 " +
                                  "characters long";
                StatusText.color = Color.red;
                RegisterButton.enabled = true;
                break;
            
            default:
                StatusText.text = "Unknown response: " + response;
                StatusText.color = Color.yellow;
                RegisterButton.enabled = true;
                break;
        }
    }
}

